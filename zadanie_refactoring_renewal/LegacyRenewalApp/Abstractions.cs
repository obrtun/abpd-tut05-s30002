using System;

namespace LegacyRenewalApp
{
    public interface ICustomerRepository
    {
        Customer GetById(int customerId);
    }

    public interface ISubscriptionPlanRepository
    {
        SubscriptionPlan GetByCode(string code);
    }

    public interface IBillingGateway
    {
        void SaveInvoice(RenewalInvoice invoice);
        void SendEmail(string email, string subject, string body);
    }

    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public interface IInvoiceNumberGenerator
    {
        string Generate(int customerId, string normalizedPlanCode, DateTime generatedAtUtc);
    }

    public interface IDiscountCalculator
    {
        PricingBreakdown ApplyDiscounts(Customer customer, SubscriptionPlan plan, int seatCount, bool useLoyaltyPoints);
    }

    public interface ISupportFeeCalculator
    {
        PricingComponent Calculate(string normalizedPlanCode, bool includePremiumSupport);
    }

    public interface IPaymentFeeCalculator
    {
        PricingComponent Calculate(string normalizedPaymentMethod, decimal subtotalAfterDiscount, decimal supportFee);
    }

    public interface ITaxCalculator
    {
        decimal GetTaxRate(string country);
    }

    public interface IRenewalInvoiceFactory
    {
        RenewalInvoice Create(
            Customer customer,
            string normalizedPlanCode,
            string normalizedPaymentMethod,
            int seatCount,
            decimal baseAmount,
            decimal discountAmount,
            decimal supportFee,
            decimal paymentFee,
            decimal taxAmount,
            decimal finalAmount,
            string notes,
            DateTime generatedAtUtc);
    }

    public interface IInvoicePersistenceService
    {
        void Save(RenewalInvoice invoice);
    }

    public interface IInvoiceNotificationService
    {
        void SendRenewalInvoice(Customer customer, string normalizedPlanCode, RenewalInvoice invoice);
    }
}
