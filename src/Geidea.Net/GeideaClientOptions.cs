using System;

namespace Geidea
{
    /// <summary>
    /// Configuration for <see cref="GeideaClient"/>.
    /// Keep <see cref="ApiPassword"/> server-side only — never expose it to browsers or mobile clients.
    /// </summary>
    public class GeideaClientOptions
    {
        /// <summary>
        /// Your Geidea merchant public key. Used as the Basic-auth username and as part of every signature.
        /// </summary>
        public string MerchantPublicKey { get; set; }

        /// <summary>
        /// Your Geidea API password. Used as the Basic-auth password and as the HMAC key for signatures.
        /// This is a secret — store it in a vault, environment variable, or user-secrets, never in code.
        /// </summary>
        public string ApiPassword { get; set; }

        /// <summary>
        /// Regional environment. Defaults to <see cref="GeideaEnvironment.Egypt"/>.
        /// </summary>
        public GeideaEnvironment Environment { get; set; } = GeideaEnvironment.Egypt;

        /// <summary>
        /// Optional override for the API base URL (e.g. for testing). When set, <see cref="Environment"/> is ignored.
        /// </summary>
        public string BaseUrlOverride { get; set; }

        /// <summary>
        /// Resolves the API base URL from <see cref="Environment"/> unless <see cref="BaseUrlOverride"/> is set.
        /// </summary>
        public string BaseUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(BaseUrlOverride))
                    return BaseUrlOverride.TrimEnd('/');

                switch (Environment)
                {
                    case GeideaEnvironment.Saudi:
                        return "https://api.ksamerchant.geidea.net";
                    case GeideaEnvironment.UAE:
                        return "https://api.geidea.ae";
                    case GeideaEnvironment.Egypt:
                    default:
                        return "https://api.merchant.geidea.net";
                }
            }
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(MerchantPublicKey))
                throw new InvalidOperationException("MerchantPublicKey is required. Set GeideaClientOptions.MerchantPublicKey.");
            if (string.IsNullOrWhiteSpace(ApiPassword))
                throw new InvalidOperationException("ApiPassword is required. Set GeideaClientOptions.ApiPassword.");
        }
    }
}
