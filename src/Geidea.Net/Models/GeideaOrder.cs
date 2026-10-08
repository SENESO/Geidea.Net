using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Geidea.Models
{
    /// <summary>
    /// Full order details as returned by the Fetch Order API and embedded in refund responses.
    /// </summary>
    public class GeideaOrder
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("settleCurrency")]
        public string SettleCurrency { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        /// <summary>Detailed order state, e.g. "Paid", "PartiallyRefunded", "InProgress", "Cancelled".</summary>
        [JsonPropertyName("detailedStatus")]
        public string DetailedStatus { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("merchantId")]
        public string MerchantId { get; set; }

        [JsonPropertyName("merchantPublicKey")]
        public string MerchantPublicKey { get; set; }

        [JsonPropertyName("merchantReferenceId")]
        public string MerchantReferenceId { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("returnUrl")]
        public string ReturnUrl { get; set; }

        [JsonPropertyName("paymentOperation")]
        public string PaymentOperation { get; set; }

        [JsonPropertyName("customerEmail")]
        public string CustomerEmail { get; set; }

        [JsonPropertyName("cardOnFile")]
        public bool CardOnFile { get; set; }

        [JsonPropertyName("tokenId")]
        public string TokenId { get; set; }

        /// <summary>
        /// All transaction attempts under this order (authentication attempts + the final pay).
        /// A paid callback consolidates every attempt into this list.
        /// </summary>
        [JsonPropertyName("transactions")]
        public List<GeideaTransaction> Transactions { get; set; }
    }

    public class GeideaTransaction
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        /// <summary>Transaction type: "Pay", "Authentication", "Refund", "Capture", "Void", ...</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("authorizationCode")]
        public string AuthorizationCode { get; set; }

        [JsonPropertyName("rrn")]
        public string Rrn { get; set; }

        [JsonPropertyName("paymentMethod")]
        public GeideaPaymentMethod PaymentMethod { get; set; }

        [JsonPropertyName("codes")]
        public GeideaTransactionCodes Codes { get; set; }

        [JsonPropertyName("createdDate")]
        public string CreatedDate { get; set; }
    }

    public class GeideaPaymentMethod
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("cardholderName")]
        public string CardholderName { get; set; }

        [JsonPropertyName("maskedCardNumber")]
        public string MaskedCardNumber { get; set; }
    }

    public class GeideaTransactionCodes
    {
        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("responseMessage")]
        public string ResponseMessage { get; set; }

        [JsonPropertyName("detailedResponseCode")]
        public string DetailedResponseCode { get; set; }

        [JsonPropertyName("detailedResponseMessage")]
        public string DetailedResponseMessage { get; set; }
    }

    /// <summary>
    /// Response of the Fetch Order API (GET /pgw/api/v1/direct/order/{orderId}).
    /// </summary>
    public class GetOrderResponse : GeideaApiResponse
    {
        [JsonPropertyName("order")]
        public GeideaOrder Order { get; set; }
    }
}
