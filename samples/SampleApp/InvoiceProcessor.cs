using System;
using System.IO;
using Microsoft.Data.SqlClient;
using SampleApp;

namespace SampleApplication
{
    public class ReportManager
    {
        public void GenerateReport()
        {
            var gateway = new SampleApp.PaymentGatewayClient();

            // 1. Cross-file call to async method using .Result (CON003)
            string status = gateway.ProcessPaymentAsync("TXN-7788").Result;
            Console.WriteLine(status);

            // 2. Using SqlConnection (Database type) without using (SAF003)
            var conn = new SqlConnection("Server=db;Database=Invoices;Integrated Security=true;");
            conn.Open();

            // 3. Dynamic SQL query with raw string interpolation (SEC001)
            string invoiceId = "INV-1001";
            var cmd = new SqlCommand($"SELECT * FROM Invoices WHERE Id = '{invoiceId}'", conn);
            cmd.ExecuteNonQuery();

            // 4. Division by zero (SAF002)
            int totalUnits = 0;
            int unitCost = 500 / totalUnits;
            Console.WriteLine(unitCost);
        }
    }
}
