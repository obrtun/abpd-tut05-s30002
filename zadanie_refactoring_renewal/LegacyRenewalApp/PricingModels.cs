namespace LegacyRenewalApp
{
    public sealed class PricingBreakdown
    {
        public PricingBreakdown(decimal baseAmount, decimal discountAmount, decimal subtotalAfterDiscount, string notes)
        {
            BaseAmount = baseAmount;
            DiscountAmount = discountAmount;
            SubtotalAfterDiscount = subtotalAfterDiscount;
            Notes = notes;
        }

        public decimal BaseAmount { get; }
        public decimal DiscountAmount { get; }
        public decimal SubtotalAfterDiscount { get; }
        public string Notes { get; }
    }

    public sealed class PricingComponent
    {
        public PricingComponent(decimal amount, string notes)
        {
            Amount = amount;
            Notes = notes;
        }

        public decimal Amount { get; }
        public string Notes { get; }
    }
}
