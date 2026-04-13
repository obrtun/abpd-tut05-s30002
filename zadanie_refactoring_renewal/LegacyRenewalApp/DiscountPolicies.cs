using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LegacyRenewalApp
{
    public interface IDiscountPolicy
    {
        DiscountResult Evaluate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints);
    }

    public sealed class DiscountResult
    {
        public DiscountResult(decimal amount, string note)
        {
            Amount = amount;
            Note = note;
        }

        public decimal Amount { get; }
        public string Note { get; }
    }

    public sealed class SegmentDiscountPolicy : IDiscountPolicy
    {
        private static readonly IReadOnlyDictionary<string, (decimal rate, string note)> SegmentDiscounts =
            new Dictionary<string, (decimal rate, string note)>(StringComparer.Ordinal)
            {
                ["Silver"] = (0.05m, "silver discount; "),
                ["Gold"] = (0.10m, "gold discount; "),
                ["Platinum"] = (0.15m, "platinum discount; ")
            };

        public DiscountResult Evaluate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (customer.Segment == "Education" && plan.IsEducationEligible)
            {
                return new DiscountResult(baseAmount * 0.20m, "education discount; ");
            }

            return SegmentDiscounts.TryGetValue(customer.Segment, out var discount)
                ? new DiscountResult(baseAmount * discount.rate, discount.note)
                : new DiscountResult(0m, string.Empty);
        }
    }

    public sealed class TenureDiscountPolicy : IDiscountPolicy
    {
        public DiscountResult Evaluate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (customer.YearsWithCompany >= 5)
            {
                return new DiscountResult(baseAmount * 0.07m, "long-term loyalty discount; ");
            }

            if (customer.YearsWithCompany >= 2)
            {
                return new DiscountResult(baseAmount * 0.03m, "basic loyalty discount; ");
            }

            return new DiscountResult(0m, string.Empty);
        }
    }

    public sealed class SeatCountDiscountPolicy : IDiscountPolicy
    {
        public DiscountResult Evaluate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (seatCount >= 50)
            {
                return new DiscountResult(baseAmount * 0.12m, "large team discount; ");
            }

            if (seatCount >= 20)
            {
                return new DiscountResult(baseAmount * 0.08m, "medium team discount; ");
            }

            if (seatCount >= 10)
            {
                return new DiscountResult(baseAmount * 0.04m, "small team discount; ");
            }

            return new DiscountResult(0m, string.Empty);
        }
    }

    public sealed class LoyaltyPointsDiscountPolicy : IDiscountPolicy
    {
        public DiscountResult Evaluate(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
        {
            if (!useLoyaltyPoints || customer.LoyaltyPoints <= 0)
            {
                return new DiscountResult(0m, string.Empty);
            }

            int pointsToUse = Math.Min(customer.LoyaltyPoints, 200);
            return new DiscountResult(pointsToUse, $"loyalty points used: {pointsToUse}; ");
        }
    }

    public sealed class DiscountCalculator : IDiscountCalculator
    {
        private readonly IReadOnlyCollection<IDiscountPolicy> _discountPolicies;

        public DiscountCalculator(IEnumerable<IDiscountPolicy> discountPolicies)
        {
            _discountPolicies = discountPolicies.ToArray();
        }

        public PricingBreakdown ApplyDiscounts(Customer customer, SubscriptionPlan plan, int seatCount, bool useLoyaltyPoints)
        {
            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            decimal discountAmount = 0m;
            var notes = new StringBuilder();

            foreach (var policy in _discountPolicies)
            {
                var result = policy.Evaluate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);
                discountAmount += result.Amount;
                notes.Append(result.Note);
            }

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes.Append("minimum discounted subtotal applied; ");
            }

            return new PricingBreakdown(baseAmount, discountAmount, subtotalAfterDiscount, notes.ToString());
        }
    }
}
