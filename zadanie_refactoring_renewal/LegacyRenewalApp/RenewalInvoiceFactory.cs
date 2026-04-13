using System;

namespace LegacyRenewalApp
{
    public sealed class RenewalInvoiceFactory : IRenewalInvoiceFactory
    {
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;

        public RenewalInvoiceFactory(IInvoiceNumberGenerator invoiceNumberGenerator)
        {
            _invoiceNumberGenerator = invoiceNumberGenerator;
        }

        public RenewalInvoice Create(
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
            DateTime generatedAtUtc)
        {
            return new RenewalInvoice
            {
                InvoiceNumber = _invoiceNumberGenerator.Generate(customer.Id, normalizedPlanCode, generatedAtUtc),
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Round(baseAmount),
                DiscountAmount = Round(discountAmount),
                SupportFee = Round(supportFee),
                PaymentFee = Round(paymentFee),
                TaxAmount = Round(taxAmount),
                FinalAmount = Round(finalAmount),
                Notes = notes.Trim(),
                GeneratedAt = generatedAtUtc
            };
        }

        private static decimal Round(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
