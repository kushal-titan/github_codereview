using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CustomerType { get; set; } = string.Empty;
    }

    public class OrderService
    {
        public decimal CalculateOrder(
            Order order,
            string couponCode,
            bool applyTax,
            bool expressShipping,
            decimal minimumAmount)
        {
            decimal total = 0;

            if (order != null)
            {
                total = order.Amount;

                if (order.CustomerType == "VIP")
                {
                    total -= 20;
                }

                if (order.CustomerType == "Gold")
                {
                    total -= 10;
                }

                if (couponCode == "SAVE10")
                {
                    total -= 10;
                }

                if (couponCode == "SAVE20")
                {
                    total -= 20;
                }

                if (applyTax)
                {
                    total += total * 0.1m;
                }

                if (expressShipping)
                {
                    total += 25;
                }

                if (total < minimumAmount)
                {
                    total = minimumAmount;
                }

                if (total > 500)
                {
                    total -= 15;
                }

                if (total > 1000)
                {
                    total -= 25;
                }

                if (total < 0)
                {
                    total = 0;
                }

                if (order.CustomerType == "Employee")
                {
                    total -= 30;
                }

                if (order.CustomerType == "Partner")
                {
                    total -= 40;
                }
            }

            return total;
        }

        public bool ValidateOrder(Order order)
        {
            return order != null;
        }
    }
}
