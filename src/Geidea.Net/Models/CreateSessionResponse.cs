using System.Text.Json.Serialization;

namespace Geidea.Models
{
    /// <summary>
    /// Response of the Create Session API. Pass <see cref="Session"/>.<see cref="GeideaSession.Id"/>
    /// to the Geidea Checkout JS library (or the hosted checkout URL) to start the payment.
    /// </summary>
    public class CreateSessionResponse : GeideaApiResponse
    {
        [JsonPropertyName("session")]
        public GeideaSession Session { get; set; }
    }

    public class GeideaSession
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("returnUrl")]
        public string ReturnUrl { get; set; }

        [JsonPropertyName("expiryDate")]
        public string ExpiryDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("merchantId")]
        public string MerchantId { get; set; }

        [JsonPropertyName("merchantPublicKey")]
        public string MerchantPublicKey { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("merchantReferenceId")]
        public string MerchantReferenceId { get; set; }

        [JsonPropertyName("paymentIntentId")]
        public string PaymentIntentId { get; set; }

        [JsonPropertyName("paymentOperation")]
        public string PaymentOperation { get; set; }

        [JsonPropertyName("cardOnFile")]
        public bool CardOnFile { get; set; }

        [JsonPropertyName("tokenId")]
        public string TokenId { get; set; }
    }

    /// <summary>
    /// Fields present on every Geidea API response envelope.
    /// A successful operation has <see cref="ResponseCode"/> == "000".
    /// </summary>
    public class GeideaApiResponse
    {
        [JsonPropertyName("responseMessage")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("detailedResponseMessage")]
        public string DetailedResponseMessage { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("detailedResponseCode")]
        public string DetailedResponseCode { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        /// <summary>True when Geidea reports the operation as successful (responseCode "000").</summary>
        [JsonIgnore]
        public bool IsSuccess => ResponseCode == "000";
    }
}
