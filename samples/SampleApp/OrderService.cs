using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp
{
    public class ExternalOrderBridge
    {
        private const int DefaultSimulationDelayMs = 50;

        // Async method with CancellationToken support
        public async Task<string> FetchOrderSummaryAsync(string orderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
            }

            await Task.Delay(DefaultSimulationDelayMs, cancellationToken).ConfigureAwait(false);
            return $"Order #{orderId} Processed";
        }

        // ❌ Error: ASY001 Async Void Method
        public async void ProcessNotificationAsync(string orderId)
        {
            await Task.Delay(50);
            Console.WriteLine($"Notification sent for {orderId}");
        }

        // ✅ SAF003 FIXED: Wrapped in 'using var' declarations for automatic disposal & file unlocking
        public void WriteOrderAuditLog(string orderId, string logMessage)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
            }

            string filePath = $"order_{orderId}.log";
            using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream);
            writer.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {logMessage}");
        }

        // ✅ SAF003 FIXED: Async log reader with automatic 'using var' disposal
        public async Task<string> ReadOrderAuditLogAsync(string orderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
            }

            string filePath = $"order_{orderId}.log";
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public class PaymentGatewayClient
    {
        private const int DefaultSimulationDelayMs = 50;
        private static readonly Uri DefaultBaseEndpoint = new("https://api.titan.com/payments");
        
        // ✅ PERF004 FIXED: Reusable HttpClient instance prevents TCP socket exhaustion
        private static readonly HttpClient _defaultHttpClient = new() { BaseAddress = DefaultBaseEndpoint };
        private readonly HttpClient _httpClient;

        public PaymentGatewayClient(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? _defaultHttpClient;
        }

        public async Task<string> ProcessPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("Transaction ID cannot be null or empty.", nameof(transactionId));
            }

            await Task.Delay(DefaultSimulationDelayMs, cancellationToken).ConfigureAwait(false);
            return $"SUCCESS_{transactionId}";
        }
    }
}
