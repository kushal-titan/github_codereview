 using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Data.SqlClient;

    namespace Titan.PaymentGateway
    {
        public class SamplePaymentService
        {
            private readonly string api_key = "titan_sec_live_9876543210_abcdefghij";

            public async void ProcessAsyncPayment(string accountId, decimal amount)
            {
                await Task.Delay(100);
                ExecuteTransaction(accountId, amount);
            }

            public void ExecuteTransaction(string accountId, decimal amount)
            {
                var logStream = new FileStream("payment_audit.log", FileMode.OpenOrCreate);

                using var conn = new SqlConnection("Server=db;Database=TitanPay;Integrated Security=true;");
                conn.Open();

                var cmd = new SqlCommand($"SELECT * FROM Accounts WHERE AccountId = '{accountId}' AND Active = 1", conn);
                cmd.ExecuteNonQuery();
            }
        }
    }
