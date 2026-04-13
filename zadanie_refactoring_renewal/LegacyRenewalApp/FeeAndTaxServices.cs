using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public sealed class SupportFeeCalculator : ISupportFeeCalculator
    {
        private static readonly IReadOnlyDictionary<string, decimal> FeesByPlanCode =
            new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                ["START"] = 250m,
                ["PRO"] = 400m,
                ["ENTERPRISE"] = 700m
            };

        public PricingComponent Calculate(string normalizedPlanCode, bool includePremiumSupport)
        {
            if (!includePremiumSupport)
            {
                return new PricingComponent(0m, string.Empty);
            }

            FeesByPlanCode.TryGetValue(normalizedPlanCode, out decimal fee);
            return new PricingComponent(fee, "premium support included; ");
        }
    }

    public sealed class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        private static readonly IReadOnlyDictionary<string, (decimal rate, string note)> PaymentMethods =
            new Dictionary<string, (decimal rate, string note)>(StringComparer.Ordinal)
            {
                ["CARD"] = (0.02m, "card payment fee; "),
                ["BANK_TRANSFER"] = (0.01m, "bank transfer fee; "),
                ["PAYPAL"] = (0.035m, "paypal fee; "),
                ["INVOICE"] = (0m, "invoice payment; ")
            };

        public PricingComponent Calculate(string normalizedPaymentMethod, decimal subtotalAfterDiscount, decimal supportFee)
        {
            if (!PaymentMethods.TryGetValue(normalizedPaymentMethod, out var paymentMethod))
            {
                throw new ArgumentException("Unsupported payment method");
            }

            decimal baseForFee = subtotalAfterDiscount + supportFee;
            return new PricingComponent(baseForFee * paymentMethod.rate, paymentMethod.note);
        }
    }

    public sealed class TaxCalculator : ITaxCalculator
    {
        private static readonly IReadOnlyDictionary<string, decimal> TaxRatesByCountry =
            new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                ["Poland"] = 0.23m,
                ["Germany"] = 0.19m,
                ["Czech Republic"] = 0.21m,
                ["Norway"] = 0.25m
            };

        public decimal GetTaxRate(string country)
        {
            return TaxRatesByCountry.TryGetValue(country, out decimal taxRate)
                ? taxRate
                : 0.20m;
        }
    }
}
