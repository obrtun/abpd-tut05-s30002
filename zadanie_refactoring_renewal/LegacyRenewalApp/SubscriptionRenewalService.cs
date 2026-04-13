using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IRenewalInvoiceFactory _invoiceFactory;
        private readonly IInvoicePersistenceService _invoicePersistenceService;
        private readonly IInvoiceNotificationService _invoiceNotificationService;
        private readonly IClock _clock;

        public SubscriptionRenewalService()
            : this(CreateDefaultDependencies())
        {
        }

        public SubscriptionRenewalService(RenewalServiceDependencies dependencies)
        {
            _customerRepository = dependencies.CustomerRepository;
            _planRepository = dependencies.PlanRepository;
            _discountCalculator = dependencies.DiscountCalculator;
            _supportFeeCalculator = dependencies.SupportFeeCalculator;
            _paymentFeeCalculator = dependencies.PaymentFeeCalculator;
            _taxCalculator = dependencies.TaxCalculator;
            _invoiceFactory = dependencies.InvoiceFactory;
            _invoicePersistenceService = dependencies.InvoicePersistenceService;
            _invoiceNotificationService = dependencies.InvoiceNotificationService;
            _clock = dependencies.Clock;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            Customer customer = _customerRepository.GetById(customerId);
            SubscriptionPlan plan = _planRepository.GetByCode(normalizedPlanCode);

            EnsureCustomerCanRenew(customer);

            PricingBreakdown pricingBreakdown = _discountCalculator.ApplyDiscounts(customer, plan, seatCount, useLoyaltyPoints);
            PricingComponent supportFee = _supportFeeCalculator.Calculate(normalizedPlanCode, includePremiumSupport);
            PricingComponent paymentFee = _paymentFeeCalculator.Calculate(normalizedPaymentMethod, pricingBreakdown.SubtotalAfterDiscount, supportFee.Amount);

            decimal taxRate = _taxCalculator.GetTaxRate(customer.Country);
            decimal taxBase = pricingBreakdown.SubtotalAfterDiscount + supportFee.Amount + paymentFee.Amount;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            string notes = pricingBreakdown.Notes + supportFee.Notes + paymentFee.Notes;
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            DateTime generatedAtUtc = _clock.UtcNow;
            RenewalInvoice invoice = _invoiceFactory.Create(
                customer,
                normalizedPlanCode,
                normalizedPaymentMethod,
                seatCount,
                pricingBreakdown.BaseAmount,
                pricingBreakdown.DiscountAmount,
                supportFee.Amount,
                paymentFee.Amount,
                taxAmount,
                finalAmount,
                notes,
                generatedAtUtc);

            _invoicePersistenceService.Save(invoice);
            _invoiceNotificationService.SendRenewalInvoice(customer, normalizedPlanCode, invoice);

            return invoice;
        }

        private static void ValidateInputs(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }
        }

        private static void EnsureCustomerCanRenew(Customer customer)
        {
            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }
        }

        private static RenewalServiceDependencies CreateDefaultDependencies()
        {
            var billingGateway = new LegacyBillingGatewayAdapter();
            var discountPolicies = new IDiscountPolicy[]
            {
                new SegmentDiscountPolicy(),
                new TenureDiscountPolicy(),
                new SeatCountDiscountPolicy(),
                new LoyaltyPointsDiscountPolicy()
            };

            return new RenewalServiceDependencies(
                new CustomerRepository(),
                new SubscriptionPlanRepository(),
                new DiscountCalculator(discountPolicies),
                new SupportFeeCalculator(),
                new PaymentFeeCalculator(),
                new TaxCalculator(),
                new RenewalInvoiceFactory(new RenewalInvoiceNumberGenerator()),
                new InvoicePersistenceService(billingGateway),
                new InvoiceNotificationService(billingGateway),
                new SystemClock());
        }
    }

    public sealed class RenewalServiceDependencies
    {
        public RenewalServiceDependencies(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator,
            IRenewalInvoiceFactory invoiceFactory,
            IInvoicePersistenceService invoicePersistenceService,
            IInvoiceNotificationService invoiceNotificationService,
            IClock clock)
        {
            CustomerRepository = customerRepository;
            PlanRepository = planRepository;
            DiscountCalculator = discountCalculator;
            SupportFeeCalculator = supportFeeCalculator;
            PaymentFeeCalculator = paymentFeeCalculator;
            TaxCalculator = taxCalculator;
            InvoiceFactory = invoiceFactory;
            InvoicePersistenceService = invoicePersistenceService;
            InvoiceNotificationService = invoiceNotificationService;
            Clock = clock;
        }

        public ICustomerRepository CustomerRepository { get; }
        public ISubscriptionPlanRepository PlanRepository { get; }
        public IDiscountCalculator DiscountCalculator { get; }
        public ISupportFeeCalculator SupportFeeCalculator { get; }
        public IPaymentFeeCalculator PaymentFeeCalculator { get; }
        public ITaxCalculator TaxCalculator { get; }
        public IRenewalInvoiceFactory InvoiceFactory { get; }
        public IInvoicePersistenceService InvoicePersistenceService { get; }
        public IInvoiceNotificationService InvoiceNotificationService { get; }
        public IClock Clock { get; }
    }
}
