using System.Text.Json.Serialization;

namespace Geidea.Models
{
    /// <summary>
    /// Payload Geidea POSTs to your <c>callbackUrl</c> when a payment reaches a final state.
    ///
    /// Security rules (enforced by <see cref="Webhooks.GeideaCallbackValidator"/>):
    /// <list type="bullet">
    ///   <item>Always verify <see cref="Signature"/> before processing.</item>
    ///   <item>Only treat the order as paid when <see cref="GeideaApiResponse.ResponseCode"/> is "000".</item>
    ///   <item>Compare amount, currency, order ID and merchant reference against your records.</item>
    ///   <item>Never fulfill from the browser return URL or the JS onSuccess event alone.</item>
    /// </list>
    /// </summary>
    public class GeideaCallback
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("merchantReferenceId")]
        public string MerchantReferenceId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        /// <summary>Order status at callback time, e.g. "Success", "Failed", "Cancelled".</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("responseMessage")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("detailedResponseCode")]
        public string DetailedResponseCode { get; set; }

        [JsonPropertyName("detailedResponseMessage")]
        public string DetailedResponseMessage { get; set; }

        [JsonPropertyName("merchantPublicKey")]
        public string MerchantPublicKey { get; set; }

        /// <summary>True when Geidea reports success (responseCode "000").</summary>
        [JsonIgnore]
        public bool IsSuccess => ResponseCode == "000";
    }
}
