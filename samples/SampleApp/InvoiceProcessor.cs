using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SampleApp;

namespace SampleApplication
{
    public class ReportManager
    {
        private readonly PaymentGatewayClient _gateway;

        public ReportManager(PaymentGatewayClient? gateway = null)
        {
            _gateway = gateway ?? new PaymentGatewayClient();
        }

        public async Task GenerateReportAsync(CancellationToken cancellationToken = default)
        {
            // 1. CON003 FIXED: Use 'await' instead of blocking '.Result'
            string status = await _gateway.ProcessPaymentAsync("TXN-7788", cancellationToken).ConfigureAwait(false);
            Console.WriteLine(status);

            // 2. SAF003 FIXED: Wrapped in 'using var' for deterministic connection pooling
            using var conn = new SqlConnection("Server=db;Database=Invoices;Integrated Security=true;TrustServerCertificate=True;");
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            // 3. SEC001 FIXED: Parameterized SQL query preventing SQL injection
            string invoiceId = "INV-1001";
            using var cmd = new SqlCommand("SELECT * FROM Invoices WHERE Id = @InvoiceId", conn);
            cmd.Parameters.Add("@InvoiceId", SqlDbType.VarChar, 50).Value = invoiceId;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // 4. SAF002 FIXED: Safe division guard preventing DivideByZeroException
            int totalUnits = 0;
            int unitCost = totalUnits > 0 ? 500 / totalUnits : 0;
            Console.WriteLine(unitCost);
        }
    }
}
