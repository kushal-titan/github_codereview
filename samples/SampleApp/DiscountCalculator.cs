using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class CustomerProfile
    {
        public string CustomerId { get; set; } = string.Empty;
        public string MembershipLevel { get; set; } = "Bronze";
        public int LoyaltyPoints { get; set; }
        public bool IsEmployee { get; set; }
        public bool IsFirstTimeBuyer { get; set; }
    }

    public class DiscountCalculator
    {
        // Deliberate violations for PR test:
        // 1. Method Length > 50 lines (CQ001)
        // 2. Cyclomatic Complexity > 10 (CQ002)
        // 3. Parameter Count = 6 > 4 (CQ003)
        // 4. Nesting Depth = 4 > 3 (CQ004)
        public decimal CalculateDiscountsAndPromotions(
            CustomerProfile customer,
            decimal cartTotal,
            string promoCode,
            string regionCode,
            bool isHolidaySeason,
            bool hasGiftCard)
        {
            decimal finalDiscount = 0m;

            if (customer != null)
            {
                if (cartTotal > 0)
                {
                    if (customer.IsEmployee)
                    {
                        finalDiscount += cartTotal * 0.25m;
                    }
                    else
                    {
                        if (customer.MembershipLevel == "Platinum")
                        {
                            if (cartTotal > 500)
                            {
                                finalDiscount += cartTotal * 0.20m;
                            }
                            else
                            {
                                finalDiscount += cartTotal * 0.15m;
                            }
                        }
                        else if (customer.MembershipLevel == "Gold")
                        {
                            finalDiscount += cartTotal * 0.10m;
                        }
                        else if (customer.MembershipLevel == "Silver")
                        {
                            finalDiscount += cartTotal * 0.05m;
                        }

                        if (customer.IsFirstTimeBuyer)
                        {
                            finalDiscount += 15.00m;
                        }
                    }

                    if (!string.IsNullOrEmpty(promoCode))
                    {
                        if (promoCode == "FLAT50" && cartTotal > 200)
                        {
                            finalDiscount += 50.00m;
                        }
                        else if (promoCode == "SAVE10")
                        {
                            finalDiscount += cartTotal * 0.10m;
                        }
                        else if (promoCode == "HOLIDAY25" && isHolidaySeason)
                        {
                            finalDiscount += cartTotal * 0.25m;
                        }
                    }

                    if (customer.LoyaltyPoints > 1000)
                    {
                        finalDiscount += 25.00m;
                    }
                    else if (customer.LoyaltyPoints > 500)
                    {
                        finalDiscount += 10.00m;
                    }

                    if (regionCode == "EU")
                    {
                        if (finalDiscount > cartTotal * 0.40m)
                        {
                            finalDiscount = cartTotal * 0.40m; // Legal max discount cap
                        }
                    }
                    else if (regionCode == "US")
                    {
                        if (finalDiscount > cartTotal * 0.50m)
                        {
                            finalDiscount = cartTotal * 0.50m;
                        }
                    }

                    if (hasGiftCard)
                    {
                        finalDiscount += 5.00m;
                    }
                }
            }

            if (finalDiscount > cartTotal)
            {
                finalDiscount = cartTotal;
            }

            return finalDiscount;
        }
    }
}
