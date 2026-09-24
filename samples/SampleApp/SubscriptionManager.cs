using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class SubscriberInfo
    {
        public string SubscriberId { get; set; } = string.Empty;
        public string Tier { get; set; } = "Standard";
        public int ActiveSeats { get; set; } = 1;
        public bool IsNonProfit { get; set; }
        public bool AnnualBilling { get; set; }
        public string BillingRegion { get; set; } = "APAC";
    }

    public class SubscriptionManager
    {
        // Deliberate violations for the fresh PR test:
        // 1. Method Length > 50 lines (CQ001)
        // 2. Cyclomatic Complexity > 10 (CQ002)
        // 3. Parameter Count = 5 > 4 (CQ003)
        // 4. Nesting Depth = 4 > 3 (CQ004)
        public decimal CalculateTieredSubscriptionPrice(
            SubscriberInfo subscriber,
            decimal baseSeatRate,
            string campaignPromo,
            bool applyCustomTax,
            decimal storageAddonGb)
        {
            decimal totalMonthly = 0m;
            decimal discountApplied = 0m;
            decimal calculatedTax = 0m;

            if (subscriber != null)
            {
                if (subscriber.ActiveSeats > 0)
                {
                    if (baseSeatRate > 0)
                    {
                        totalMonthly = subscriber.ActiveSeats * baseSeatRate;

                        if (subscriber.Tier == "Enterprise")
                        {
                            if (subscriber.ActiveSeats > 100)
                            {
                                discountApplied += totalMonthly * 0.25m;
                            }
                            else if (subscriber.ActiveSeats > 50)
                            {
                                discountApplied += totalMonthly * 0.15m;
                            }
                            else
                            {
                                discountApplied += totalMonthly * 0.10m;
                            }
                        }
                        else if (subscriber.Tier == "Professional")
                        {
                            discountApplied += totalMonthly * 0.05m;
                        }

                        if (!string.IsNullOrEmpty(campaignPromo))
                        {
                            if (campaignPromo == "LAUNCH2026")
                            {
                                discountApplied += 150m;
                            }
                            else if (campaignPromo == "PARTNER20")
                            {
                                discountApplied += totalMonthly * 0.20m;
                            }
                        }

                        if (subscriber.AnnualBilling)
                        {
                            discountApplied += (totalMonthly - discountApplied) * 0.10m;
                        }

                        if (subscriber.IsNonProfit)
                        {
                            discountApplied += (totalMonthly - discountApplied) * 0.30m;
                        }

                        if (storageAddonGb > 500)
                        {
                            totalMonthly += (storageAddonGb - 500) * 0.02m;
                        }

                        if (applyCustomTax)
                        {
                            if (subscriber.BillingRegion == "EU")
                            {
                                calculatedTax = (totalMonthly - discountApplied) * 0.21m;
                            }
                            else if (subscriber.BillingRegion == "UK")
                            {
                                calculatedTax = (totalMonthly - discountApplied) * 0.20m;
                            }
                            else
                            {
                                calculatedTax = (totalMonthly - discountApplied) * 0.08m;
                            }
                        }
                    }
                }
            }

            decimal finalInvoiceAmount = (totalMonthly - discountApplied) + calculatedTax;
            if (finalInvoiceAmount < 0)
            {
                finalInvoiceAmount = 0;
            }

            return finalInvoiceAmount;
        }
    }
}
