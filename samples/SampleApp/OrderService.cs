using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class OrderItem
    {
        public string ItemId { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerTier { get; set; } = "Standard";
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsPriority { get; set; }
        public string CountryCode { get; set; } = "US";
    }

    public class OrderService
    {
        // Deliberate violation: Method length (> 50 lines), High Complexity (> 10), and Deep Nesting (> 3)
        public decimal ProcessAndCalculateOrder(
            Order order, 
            string couponCode, 
            bool applyTax, 
            bool expressShipping, 
            string overrideRegion, 
            decimal minimumThreshold) // Deliberate violation: 6 parameters (> 4)
        {
            decimal total = 0m;
            decimal discount = 0m;
            decimal tax = 0m;

            if (order != null)
            {
                if (order.Items != null && order.Items.Count > 0)
                {
                    foreach (var item in order.Items)
                    {
                        if (item.Quantity > 0)
                        {
                            if (item.Price > 0)
                            {
                                total += item.Price * item.Quantity;

                                if (item.Category == "Electronics")
                                {
                                    discount += (item.Price * item.Quantity) * 0.05m;
                                }
                                else if (item.Category == "Clothing")
                                {
                                    discount += (item.Price * item.Quantity) * 0.10m;
                                }
                                else if (item.Category == "Books")
                                {
                                    discount += (item.Price * item.Quantity) * 0.15m;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(couponCode))
                    {
                        if (couponCode == "SUMMER20")
                        {
                            discount += total * 0.20m;
                        }
                        else if (couponCode == "VIP50" && order.CustomerTier == "VIP")
                        {
                            discount += total * 0.50m;
                        }
                        else if (couponCode == "FLAT10")
                        {
                            discount += 10.00m;
                        }
                    }

                    if (order.CustomerTier == "VIP")
                    {
                        discount += 15.00m;
                    }
                    else if (order.CustomerTier == "Gold")
                    {
                        discount += 10.00m;
                    }
                    else if (order.CustomerTier == "Silver")
                    {
                        discount += 5.00m;
                    }

                    if (applyTax)
                    {
                        if (order.CountryCode == "US")
                        {
                            tax = (total - discount) * 0.08m;
                        }
                        else if (order.CountryCode == "CA")
                        {
                            tax = (total - discount) * 0.13m;
                        }
                        else if (order.CountryCode == "UK")
                        {
                            tax = (total - discount) * 0.20m;
                        }
                        else
                        {
                            tax = (total - discount) * 0.10m;
                        }
                    }

                    if (expressShipping)
                    {
                        total += 25.00m;
                    }
                    else
                    {
                        total += 5.00m;
                    }

                    if (total < minimumThreshold)
                    {
                        total = minimumThreshold;
                    }
                }
            }

            decimal finalAmount = (total - discount) + tax;
            if (finalAmount < 0)
            {
                finalAmount = 0;
            }

            if (order != null)
            {
                order.TotalAmount = total;
                order.DiscountAmount = discount;
                order.TaxAmount = tax;
                order.FinalAmount = finalAmount;
            }

            return finalAmount;
        }

        // Clean helper method that satisfies all rules
        public bool ValidateOrder(Order order)
        {
            if (order == null || order.Items == null)
            {
                return false;
            }

            return order.Items.Count > 0;
        }
    }
}
