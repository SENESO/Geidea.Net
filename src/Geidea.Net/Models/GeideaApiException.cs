using System;

namespace Geidea.Models
{
    /// <summary>
    /// Thrown when the Geidea API returns a non-success response or an HTTP error.
    /// </summary>
    public class GeideaApiException : Exception
    {
        /// <summary>Geidea response code (e.g. "000" on success).</summary>
        public string ResponseCode { get; }

        /// <summary>Geidea response message.</summary>
        public string ResponseMessage { get; }

        /// <summary>Geidea detailed response code.</summary>
        public string DetailedResponseCode { get; }

        /// <summary>Geidea detailed response message.</summary>
        public string DetailedResponseMessage { get; }

        /// <summary>HTTP status code of the failed call, when available.</summary>
        public int? HttpStatusCode { get; }

        public GeideaApiException(
            string responseCode,
            string responseMessage,
            string detailedResponseCode,
            string detailedResponseMessage,
            int? httpStatusCode = null)
            : base(BuildMessage(responseCode, responseMessage, detailedResponseCode, detailedResponseMessage, httpStatusCode))
        {
            ResponseCode = responseCode;
            ResponseMessage = responseMessage;
            DetailedResponseCode = detailedResponseCode;
            DetailedResponseMessage = detailedResponseMessage;
            HttpStatusCode = httpStatusCode;
        }

        public GeideaApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private static string BuildMessage(
            string responseCode,
            string responseMessage,
            string detailedResponseCode,
            string detailedResponseMessage,
            int? httpStatusCode)
        {
            var msg = "Geidea API call failed";
            if (httpStatusCode.HasValue)
                msg += $" (HTTP {httpStatusCode.Value})";
            msg += $": responseCode={responseCode ?? "-"}, responseMessage={responseMessage ?? "-"}";
            if (!string.IsNullOrEmpty(detailedResponseCode) || !string.IsNullOrEmpty(detailedResponseMessage))
                msg += $", detailedResponseCode={detailedResponseCode ?? "-"}, detailedResponseMessage={detailedResponseMessage ?? "-"}";
            return msg;
        }
    }
}
