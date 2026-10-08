using System;
using System.Text.Json;
using Geidea.Models;
using Geidea.Signature;

namespace Geidea.Webhooks
{
    /// <summary>
    /// Validates Geidea payment callbacks using the HMAC-SHA256 signature Geidea
    /// attaches to every callback payload.
    ///
    /// Per Geidea's docs, the callback signature is:
    /// Base64(HMAC-SHA256(apiPassword,
    ///   merchantPublicKey + orderAmount(F2) + orderCurrency + orderId + status + merchantReferenceId + timestamp))
    ///
    /// Always validate callbacks — otherwise anyone can forge a "payment succeeded"
    /// POST to your notification URL.
    /// </summary>
    public static class GeideaCallbackValidator
    {
        /// <summary>
        /// Validates a raw callback JSON body. Returns false on any parsing or signature problem.
        /// </summary>
        public static bool IsValidCallback(string merchantPublicKey, string apiPassword, string callbackJson)
        {
            if (string.IsNullOrEmpty(merchantPublicKey)
                || string.IsNullOrEmpty(apiPassword)
                || string.IsNullOrEmpty(callbackJson))
                return false;

            GeideaCallback callback;
            try
            {
                callback = JsonSerializer.Deserialize<GeideaCallback>(callbackJson);
            }
            catch (JsonException)
            {
                return false;
            }

            return IsValidCallback(merchantPublicKey, apiPassword, callback);
        }

        /// <summary>
        /// Validates a deserialized callback payload against its signature.
        /// </summary>
        public static bool IsValidCallback(string merchantPublicKey, string apiPassword, GeideaCallback callback)
        {
            if (string.IsNullOrEmpty(merchantPublicKey)
                || string.IsNullOrEmpty(apiPassword)
                || callback == null
                || string.IsNullOrEmpty(callback.Signature))
                return false;

            string expected;
            try
            {
                expected = GeideaSignature.GenerateCallbackSignature(
                    merchantPublicKey,
                    callback.Amount,
                    callback.Currency,
                    callback.OrderId,
                    callback.Status,
                    callback.MerchantReferenceId,
                    callback.Timestamp,
                    apiPassword);
            }
            catch (ArgumentNullException)
            {
                return false;
            }

            return FixedTimeEquals(expected, callback.Signature);
        }

        // Constant-time comparison to avoid leaking signature bytes through timing.
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
