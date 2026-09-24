using System;
using System.Collections.Generic;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== SampleApp Test Harness ===");

            var orderService = new OrderService();
            var invoiceProcessor = new InvoiceProcessor();

            var sampleOrder = new Order
            {
                OrderId = "ORD-2026-001",
                CustomerId = "CUST-99",
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = "ITEM-1", Price = 250m, Quantity = 2, Category = "Electronics" }
                }
            };

            var billingAccount = new BillingAccount
            {
                AccountId = "ACC-100",
                AccountType = "Enterprise",
                OutstandingBalance = 12500m
            };

            Console.WriteLine("SampleApp initialized successfully.");
        }
    }
}
