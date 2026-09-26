using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    namespace SampleApp
    {
        /// <summary>
        /// Contract for external order operations.
        /// </summary>
        public interface IExternalOrderBridge
        {
            Task<string> FetchOrderSummaryAsync(string orderId, CancellationToken cancellationToken = default);
            FileStream OpenOrderAuditLog(string orderId);
        }

        /// <summary>
        /// Service for bridging external order interactions and log management.
        /// </summary>
        public class ExternalOrderBridge : IExternalOrderBridge
        {
            private const int DefaultSimulationDelayMs = 50;

            /// <summary>
            /// Fetches the summary for a given order asynchronously with cancellation support.
            /// </summary>
            public async Task<string> FetchOrderSummaryAsync(string orderId, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
                }

                await Task.Delay(DefaultSimulationDelayMs, cancellationToken).ConfigureAwait(false);
                return $"Order #{orderId} Processed";
            }

            /// <summary>
            /// Opens the audit log file stream safely for read/write access.
            /// </summary>
            public FileStream OpenOrderAuditLog(string orderId)
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));
                }

                string logFileName = $"order_{orderId}.log";
                return new FileStream(
                    logFileName,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);
            }
        }

        /// <summary>
        /// Contract for payment gateway operations.
        /// </summary>
        public interface IPaymentGatewayClient
        {
            Task<string> ProcessPaymentAsync(string transactionId, CancellationToken cancellationToken = default);
        }

        /// <summary>
        /// Enterprise client for processing transactions via HttpClientFactory injection.
        /// </summary>
        public class PaymentGatewayClient : IPaymentGatewayClient
        {
            private const int DefaultSimulationDelayMs = 50;
            private static readonly Uri DefaultBaseEndpoint = new("https://api.titan.com/payments");

            private readonly HttpClient _httpClient;

            /// <summary>
            /// Initializes a new instance with injected HttpClient (prevents TCP socket exhaustion).
            /// </summary>
            public PaymentGatewayClient(HttpClient httpClient)
            {
                _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
                if (_httpClient.BaseAddress == null)
                {
                    _httpClient.BaseAddress = DefaultBaseEndpoint;
                }
            }

            /// <summary>
            /// Processes a payment transaction asynchronously with cancellation support.
            /// </summary>
            public async Task<string> ProcessPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    throw new ArgumentException("Transaction ID cannot be null or empty.", nameof(transactionId));
                }

                // Simulated async processing using injected HttpClient
                await Task.Delay(DefaultSimulationDelayMs, cancellationToken).ConfigureAwait(false);
                return $"SUCCESS_{transactionId}";
            }
        }
    }
