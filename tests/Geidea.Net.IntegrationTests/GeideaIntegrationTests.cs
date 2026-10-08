using System;
using System.Threading.Tasks;
using Geidea.Models;
using NUnit.Framework;

namespace Geidea.Net.IntegrationTests
{
    /// <summary>
    /// Live tests against the Geidea API. All credentials come from environment variables —
    /// nothing is hardcoded. Tests are skipped when credentials are missing.
    ///
    /// Required:
    ///   GEIDEA_MERCHANT_PUBLIC_KEY
    ///   GEIDEA_API_PASSWORD
    /// Optional:
    ///   GEIDEA_ENVIRONMENT   (Saudi, Egypt, UAE — default: Egypt)
    ///   GEIDEA_CALLBACK_URL  (default: https://example.com/api/payments/geidea/callback)
    ///   GEIDEA_RETURN_URL    (default: https://example.com/checkout/done)
    /// </summary>
    [TestFixture]
    [Explicit("Requires live Geidea credentials — set GEIDEA_MERCHANT_PUBLIC_KEY and GEIDEA_API_PASSWORD.")]
    public class GeideaIntegrationTests
    {
        private GeideaClient _client;
        private string _callbackUrl;
        private string _returnUrl;

        [SetUp]
        public void SetUp()
        {
            var publicKey = Environment.GetEnvironmentVariable("GEIDEA_MERCHANT_PUBLIC_KEY");
            var apiPassword = Environment.GetEnvironmentVariable("GEIDEA_API_PASSWORD");

            if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(apiPassword))
                Assert.Ignore("Set GEIDEA_MERCHANT_PUBLIC_KEY and GEIDEA_API_PASSWORD to run live tests.");

            var envName = Environment.GetEnvironmentVariable("GEIDEA_ENVIRONMENT");
            var environment = GeideaEnvironment.Egypt;
            if (!string.IsNullOrWhiteSpace(envName) && Enum.TryParse(envName, true, out GeideaEnvironment parsed))
                environment = parsed;

            _callbackUrl = Environment.GetEnvironmentVariable("GEIDEA_CALLBACK_URL")
                ?? "https://example.com/api/payments/geidea/callback";
            _returnUrl = Environment.GetEnvironmentVariable("GEIDEA_RETURN_URL")
                ?? "https://example.com/checkout/done";

            _client = new GeideaClient(new GeideaClientOptions
            {
                MerchantPublicKey = publicKey,
                ApiPassword = apiPassword,
                Environment = environment
            });
        }

        [Test]
        public async Task CreateSession_Live_ReturnsSessionId()
        {
            var response = await _client.CreateSessionAsync(new CreateSessionRequest
            {
                Amount = 10.00m,
                Currency = "EGP",
                MerchantReferenceId = "itest-" + Guid.NewGuid().ToString("N").Substring(0, 12),
                CallbackUrl = _callbackUrl,
                ReturnUrl = _returnUrl,
                Language = "en"
            });

            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Session);
            Assert.IsNotEmpty(response.Session.Id);
            TestContext.WriteLine("SessionId: " + response.Session.Id);
            TestContext.WriteLine("CheckoutUrl: " + _client.GetCheckoutUrl(response.Session.Id));
        }
    }
}
