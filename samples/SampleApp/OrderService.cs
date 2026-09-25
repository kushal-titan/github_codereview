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
}
