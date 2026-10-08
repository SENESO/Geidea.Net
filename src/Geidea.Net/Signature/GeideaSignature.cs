using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Geidea.Signature
{
    /// <summary>
    /// Generates and verifies Geidea's HMAC-SHA256 (Base64) signatures.
    ///
    /// Geidea uses operation-specific field lists — a signature generated for
    /// one operation must never be reused for another. Amounts are always
    /// formatted with exactly two decimals using the invariant culture.
    /// </summary>
    public static class GeideaSignature
    {
        /// <summary>
        /// Formats an amount exactly the way Geidea expects it in signatures: two decimals, invariant culture.
        /// </summary>
        public static string FormatAmount(decimal amount)
        {
            return amount.ToString("F2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Generates the current UTC timestamp in ISO-8601 format for use in signed requests.
        /// The same value must be sent in the request body.
        /// </summary>
        public static string GenerateTimestamp()
        {
            return DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Create Session signature: {merchantPublicKey}{amount: F2}{currency}{merchantReferenceId}{timestamp}
        /// </summary>
        public static string GenerateSessionSignature(
            string merchantPublicKey,
            decimal amount,
            string currency,
            string merchantReferenceId,
            string timestamp,
            string apiPassword)
        {
            if (string.IsNullOrEmpty(merchantPublicKey)) throw new ArgumentNullException(nameof(merchantPublicKey));
            if (string.IsNullOrEmpty(apiPassword)) throw new ArgumentNullException(nameof(apiPassword));

            var data = string.Concat(
                merchantPublicKey,
                FormatAmount(amount),
                currency ?? string.Empty,
                merchantReferenceId ?? string.Empty,
                timestamp ?? string.Empty);

            return Compute(data, apiPassword);
        }

        /// <summary>
        /// Refund signature: {timestamp}{merchantPublicKey}{refundAmount: F2}{orderId}
        /// </summary>
        public static string GenerateRefundSignature(
            string timestamp,
            string merchantPublicKey,
            decimal refundAmount,
            string orderId,
            string apiPassword)
        {
            if (string.IsNullOrEmpty(merchantPublicKey)) throw new ArgumentNullException(nameof(merchantPublicKey));
            if (string.IsNullOrEmpty(apiPassword)) throw new ArgumentNullException(nameof(apiPassword));

            var data = string.Concat(
                timestamp ?? string.Empty,
                merchantPublicKey,
                FormatAmount(refundAmount),
                orderId ?? string.Empty);

            return Compute(data, apiPassword);
        }

        /// <summary>
        /// Callback (webhook) signature: {merchantPublicKey}{orderAmount: F2}{orderCurrency}{orderId}{status}{merchantReferenceId}{timestamp}
        /// </summary>
        public static string GenerateCallbackSignature(
            string merchantPublicKey,
            decimal orderAmount,
            string orderCurrency,
            string orderId,
            string status,
            string merchantReferenceId,
            string timestamp,
            string apiPassword)
        {
            if (string.IsNullOrEmpty(merchantPublicKey)) throw new ArgumentNullException(nameof(merchantPublicKey));
            if (string.IsNullOrEmpty(apiPassword)) throw new ArgumentNullException(nameof(apiPassword));

            var data = string.Concat(
                merchantPublicKey,
                FormatAmount(orderAmount),
                orderCurrency ?? string.Empty,
                orderId ?? string.Empty,
                status ?? string.Empty,
                merchantReferenceId ?? string.Empty,
                timestamp ?? string.Empty);

            return Compute(data, apiPassword);
        }

        private static string Compute(string data, string apiPassword)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiPassword)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
