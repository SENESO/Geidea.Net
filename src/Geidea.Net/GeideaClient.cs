using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Geidea.Models;
using Geidea.Signature;

namespace Geidea
{
    /// <summary>
    /// Client for Geidea's payment gateway (hosted checkout + transaction management).
    ///
    /// Authentication is HTTP Basic with the merchant public key as username and the API
    /// password as password. Session and refund calls additionally carry an HMAC-SHA256
    /// signature, which this client generates automatically.
    ///
    /// Typical flow:
    /// <code>
    /// var client = new GeideaClient(new GeideaClientOptions
    /// {
    ///     MerchantPublicKey = "...",
    ///     ApiPassword = "...",          // keep server-side only
    ///     Environment = GeideaEnvironment.Egypt,
    /// });
    ///
    /// var session = await client.CreateSessionAsync(new CreateSessionRequest
    /// {
    ///     Amount = 250.00m,
    ///     Currency = "EGP",
    ///     MerchantReferenceId = "order-1001",
    ///     CallbackUrl = "https://myshop.com/api/payments/geidea/callback",
    ///     ReturnUrl = "https://myshop.com/checkout/done",
    ///     Language = "ar",
    /// });
    ///
    /// // Redirect the customer to the hosted checkout page:
    /// var checkoutUrl = client.GetCheckoutUrl(session.Session.Id);
    /// </code>
    ///
    /// Pass your own <see cref="HttpClient"/> (e.g. from IHttpClientFactory) to control lifetime, retries and logging.
    /// </summary>
    public class GeideaClient
    {
        private readonly GeideaClientOptions _options;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public GeideaClient(GeideaClientOptions options, HttpClient httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();

            _http = httpClient ?? new HttpClient();
            _http.BaseAddress = new Uri(_options.BaseUrl);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                _options.MerchantPublicKey + ":" + _options.ApiPassword));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        /// <summary>
        /// Creates a hosted-checkout payment session. Fills in <c>timestamp</c> and the HMAC
        /// <c>signature</c> automatically. Returns the session — use <see cref="GeideaSession.Id"/>
        /// with <see cref="GetCheckoutUrl"/> or the Geidea Checkout JS library.
        /// </summary>
        public virtual async Task<CreateSessionResponse> CreateSessionAsync(
            CreateSessionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Currency)) throw new ArgumentException("Currency is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.CallbackUrl)) throw new ArgumentException("CallbackUrl is required.", nameof(request));

            if (string.IsNullOrEmpty(request.Timestamp))
                request.Timestamp = GeideaSignature.GenerateTimestamp();

            if (string.IsNullOrEmpty(request.Signature))
                request.Signature = GeideaSignature.GenerateSessionSignature(
                    _options.MerchantPublicKey,
                    request.Amount,
                    request.Currency,
                    request.MerchantReferenceId,
                    request.Timestamp,
                    _options.ApiPassword);

            if (string.IsNullOrEmpty(request.PaymentOperation))
                request.PaymentOperation = "Pay";
            if (string.IsNullOrEmpty(request.Language))
                request.Language = "en";

            return await PostAsync<CreateSessionResponse>(
                "/payment-intent/api/v2/direct/session", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches an order with its full transaction history.
        /// </summary>
        public virtual Task<GetOrderResponse> GetOrderAsync(
            string orderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId is required.", nameof(orderId));

            return GetAsync<GetOrderResponse>(
                "/pgw/api/v1/direct/order/" + Uri.EscapeDataString(orderId), cancellationToken);
        }

        /// <summary>
        /// Refunds a completed (paid/captured/settled) transaction, fully or partially.
        /// Fills in <c>timestamp</c> and the HMAC <c>signature</c> automatically.
        /// </summary>
        public virtual async Task<RefundResponse> RefundAsync(
            RefundRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.OrderId)) throw new ArgumentException("OrderId is required.", nameof(request));
            if (request.RefundAmount <= 0.01m) throw new ArgumentException("RefundAmount must be greater than 0.01.", nameof(request));

            if (string.IsNullOrEmpty(request.Timestamp))
                request.Timestamp = GeideaSignature.GenerateTimestamp();

            if (string.IsNullOrEmpty(request.Signature))
                request.Signature = GeideaSignature.GenerateRefundSignature(
                    request.Timestamp,
                    _options.MerchantPublicKey,
                    request.RefundAmount,
                    request.OrderId,
                    _options.ApiPassword);

            return await PostAsync<RefundResponse>(
                "/pgw/api/v2/direct/refund", request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels an initiated order. Only works before the Pay step completes —
        /// cancelling after payment returns "Order is already completed".
        /// Note: a successful cancellation returns responseCode "010" ("Cancelled").
        /// </summary>
        public virtual Task<CancelOrderResponse> CancelOrderAsync(
            string orderId, string reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("orderId is required.", nameof(orderId));

            return PostAsync<CancelOrderResponse>(
                "/pgw/api/v1/direct/cancel",
                new { orderId = orderId, reason = reason },
                cancellationToken,
                // Geidea reports a successful cancellation with responseCode "010".
                "000", "010");
        }

        /// <summary>
        /// Builds the hosted checkout page URL for a session. Redirect the customer here
        /// (new tab) after <see cref="CreateSessionAsync"/>.
        /// </summary>
        public string GetCheckoutUrl(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("sessionId is required.", nameof(sessionId));
            return CheckoutHost.TrimEnd('/') + "/hpp/checkout/?" + Uri.EscapeDataString(sessionId);
        }

        /// <summary>
        /// URL of the Geidea Checkout JS library for the configured environment.
        /// </summary>
        public string GetCheckoutScriptUrl()
        {
            return CheckoutHost.TrimEnd('/') + "/hpp/geideaCheckout.min.js";
        }

        private string CheckoutHost
        {
            get
            {
                switch (_options.Environment)
                {
                    case GeideaEnvironment.Saudi:
                        return "https://www.ksamerchant.geidea.net";
                    case GeideaEnvironment.UAE:
                        return "https://payments.geidea.ae";
                    case GeideaEnvironment.Egypt:
                    default:
                        return "https://www.merchant.geidea.net";
                }
            }
        }

        private async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken, params string[] successCodes)
            where T : GeideaApiResponse
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var response = await _http.PostAsync(path, content, cancellationToken).ConfigureAwait(false))
            {
                return await ReadResponseAsync<T>(response, successCodes, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
            where T : GeideaApiResponse
        {
            return GetAsync<T>(path, cancellationToken, "000");
        }

        private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken, params string[] successCodes)
            where T : GeideaApiResponse
        {
            using (var response = await _http.GetAsync(path, cancellationToken).ConfigureAwait(false))
            {
                return await ReadResponseAsync<T>(response, successCodes, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, string[] successCodes, CancellationToken cancellationToken)
            where T : GeideaApiResponse
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // Try to extract Geidea's error envelope before falling back to a generic error.
                try
                {
                    var error = JsonSerializer.Deserialize<T>(body, JsonOptions);
                    throw new GeideaApiException(
                        error?.ResponseCode,
                        error?.ResponseMessage,
                        error?.DetailedResponseCode,
                        error?.DetailedResponseMessage,
                        (int)response.StatusCode);
                }
                catch (JsonException)
                {
                    throw new GeideaApiException(
                        $"Geidea API call failed with HTTP {(int)response.StatusCode} and an unreadable body.",
                        null);
                }
            }

            T result;
            try
            {
                result = JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new GeideaApiException("Geidea API returned an unreadable response body.", ex);
            }

            if (result == null)
                throw new GeideaApiException("Geidea API returned an empty response body.", null);

            var accepted = successCodes != null && successCodes.Length > 0
                ? successCodes
                : new[] { "000" };

            var ok = false;
            foreach (var code in accepted)
            {
                if (result.ResponseCode == code) { ok = true; break; }
            }

            if (!ok)
                throw new GeideaApiException(
                    result.ResponseCode,
                    result.ResponseMessage,
                    result.DetailedResponseCode,
                    result.DetailedResponseMessage,
                    (int)response.StatusCode);

            return result;
        }
    }
}
