using System.IO;
using System.Threading.Tasks;

namespace SampleApp
{
    public class ExternalOrderBridge
    {
        // Async method to be invoked by other files
        public async Task<string> FetchOrderSummaryAsync(string orderId)
        {
            await Task.Delay(50);
            return $"Order #{orderId} Processed";
        }

        // Method returning IDisposable resource
        public FileStream OpenOrderAuditLog(string orderId)
        {
            return new FileStream($"order_{orderId}.log", FileMode.OpenOrCreate);
        }
    }

    public class PaymentGatewayClient
    {
        public async System.Threading.Tasks.Task<string> ProcessPaymentAsync(string transactionId)
        {
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.BaseAddress = new Uri("https://api.titan.com/payments");
            await System.Threading.Tasks.Task.Delay(50);
            return $"SUCCESS_{transactionId}";
        }
    }
}
