using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class BillingAccount
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccountType { get; set; } = "Enterprise";
        public decimal OutstandingBalance { get; set; }
        public bool IsDelinquent { get; set; }
        public bool AutoPayEnabled { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class InvoiceProcessor
    {
        // Deliberate test violations for the new PR:
        // 1. Method Length > 50 lines (CQ001)
        // 2. Cyclomatic Complexity > 10 (CQ002)
        // 3. Parameter Count = 5 > 4 (CQ003)
        // 4. Nesting Depth = 4 > 3 (CQ004)
        public decimal ProcessMonthlyBillingBatch(
            BillingAccount account,
            decimal billingCycleAmount,
            string promoCoupon,
            bool isEndOfQuarter,
            decimal taxRateOverride)
        {
            decimal totalCalculated = 0m;
            decimal appliedDiscount = 0m;
            decimal calculatedTax = 0m;

            if (account != null)
            {
                if (!account.IsDelinquent)
                {
                    if (billingCycleAmount > 0)
                    {
                        totalCalculated = billingCycleAmount;

                        if (account.AccountType == "Enterprise")
                        {
                            if (billingCycleAmount > 10000m)
                            {
                                appliedDiscount += billingCycleAmount * 0.15m;
                            }
                            else
                            {
                                appliedDiscount += billingCycleAmount * 0.08m;
                            }
                        }
                        else if (account.AccountType == "MidMarket")
                        {
                            appliedDiscount += billingCycleAmount * 0.05m;
                        }

                        if (!string.IsNullOrEmpty(promoCoupon))
                        {
                            if (promoCoupon == "CORP2026")
                            {
                                appliedDiscount += 500m;
                            }
                            else if (promoCoupon == "LOYALTY50")
                            {
                                appliedDiscount += billingCycleAmount * 0.05m;
                            }
                        }

                        if (isEndOfQuarter)
                        {
                            if (appliedDiscount > 0)
                            {
                                appliedDiscount += 250m;
                            }
                        }

                        if (taxRateOverride > 0)
                        {
                            calculatedTax = (totalCalculated - appliedDiscount) * taxRateOverride;
                        }
                        else
                        {
                            if (account.Currency == "EUR")
                            {
                                calculatedTax = (totalCalculated - appliedDiscount) * 0.20m;
                            }
                            else if (account.Currency == "GBP")
                            {
                                calculatedTax = (totalCalculated - appliedDiscount) * 0.18m;
                            }
                            else
                            {
                                calculatedTax = (totalCalculated - appliedDiscount) * 0.08m;
                            }
                        }

                        if (account.AutoPayEnabled)
                        {
                            appliedDiscount += 25m;
                        }
                    }
                }
            }

            decimal netCharge = (totalCalculated - appliedDiscount) + calculatedTax;
            if (netCharge < 0)
            {
                netCharge = 0;
            }

            return netCharge;
        }
    }
}
