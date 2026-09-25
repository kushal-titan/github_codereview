using System;
using System.IO;
using SampleApp; // Referencing OrderService namespace

namespace SampleApplication
{
    public class ReportManager
    {
        public void GenerateReport()
        {
            var bridge = new ExternalOrderBridge();

            // 1. Cross-File Sync-over-Async Deadlock (Calls OrderService async method with .Result)
            string summary = bridge.FetchOrderSummaryAsync("ORD-1001").Result;
            Console.WriteLine(summary);

            // 2. Cross-File Disposable Resource Leak (Receives FileStream from OrderService without 'using')
            FileStream auditStream = bridge.OpenOrderAuditLog("ORD-1001");

            // 3. Cross-File SQL Injection (Passing raw string into data command)
            string customerInput = "1001; DROP TABLE Invoices;";
            var cmd = new Microsoft.Data.SqlClient.SqlCommand($"SELECT * FROM Orders WHERE Id = '{customerInput}'");
            cmd.ExecuteNonQuery();

            // 4. Division by zero calculation using cross-file returned value
            int totalUnits = 0;
            int unitCost = 500 / totalUnits;
            Console.WriteLine(unitCost);
        }
    }
}
