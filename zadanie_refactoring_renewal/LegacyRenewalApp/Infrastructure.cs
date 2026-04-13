using System;

namespace LegacyRenewalApp
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public sealed class RenewalInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        public string Generate(int customerId, string normalizedPlanCode, DateTime generatedAtUtc)
        {
            return $"INV-{generatedAtUtc:yyyyMMdd}-{customerId}-{normalizedPlanCode}";
        }
    }

    public sealed class LegacyBillingGatewayAdapter : IBillingGateway
    {
        public void SaveInvoice(RenewalInvoice invoice)
        {
            LegacyBillingGateway.SaveInvoice(invoice);
        }

        public void SendEmail(string email, string subject, string body)
        {
            LegacyBillingGateway.SendEmail(email, subject, body);
        }
    }

    public sealed class InvoicePersistenceService : IInvoicePersistenceService
    {
        private readonly IBillingGateway _billingGateway;

        public InvoicePersistenceService(IBillingGateway billingGateway)
        {
            _billingGateway = billingGateway;
        }

        public void Save(RenewalInvoice invoice)
        {
            _billingGateway.SaveInvoice(invoice);
        }
    }

    public sealed class InvoiceNotificationService : IInvoiceNotificationService
    {
        private readonly IBillingGateway _billingGateway;

        public InvoiceNotificationService(IBillingGateway billingGateway)
        {
            _billingGateway = billingGateway;
        }

        public void SendRenewalInvoice(Customer customer, string normalizedPlanCode, RenewalInvoice invoice)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                return;
            }

            const string subject = "Subscription renewal invoice";
            string body =
                $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

            _billingGateway.SendEmail(customer.Email, subject, body);
        }
    }
}
