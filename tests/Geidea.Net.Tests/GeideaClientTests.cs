using System.Net.Http;
using System.Threading.Tasks;
using Geidea.Models;
using NUnit.Framework;

namespace Geidea.Net.Tests
{
    [TestFixture]
    public class GeideaClientTests
    {
        private static GeideaClientOptions Options() => new GeideaClientOptions
        {
            MerchantPublicKey = "11111111-2222-3333-4444-555555555555",
            ApiPassword = "test-api-password",
            Environment = GeideaEnvironment.Egypt
        };

        private static CreateSessionRequest SessionRequest() => new CreateSessionRequest
        {
            Amount = 250.00m,
            Currency = "EGP",
            MerchantReferenceId = "order-1001",
            CallbackUrl = "https://myshop.com/api/payments/geidea/callback",
            ReturnUrl = "https://myshop.com/checkout/done",
            Language = "ar",
            Timestamp = "2026-01-15T10:30:00Z"
        };

        private const string SessionJson = @"{
            ""session"": {
                ""id"": ""0b02b503-fbf8-49dd-cdec-08dcdb056790"",
                ""amount"": 250.00,
                ""currency"": ""EGP"",
                ""callbackUrl"": ""https://myshop.com/api/payments/geidea/callback"",
                ""returnUrl"": ""https://myshop.com/checkout/done"",
                ""expiryDate"": ""2026-01-15T10:45:00Z"",
                ""status"": ""Initiated"",
                ""merchantId"": ""e80ece7e-fb2a-4c0b-de94-08d8a29a107b"",
                ""merchantPublicKey"": ""11111111-2222-3333-4444-555555555555"",
                ""language"": ""ar"",
                ""merchantReferenceId"": ""order-1001"",
                ""paymentOperation"": ""Pay"",
                ""cardOnFile"": false
            },
            ""responseMessage"": ""Success"",
            ""detailedResponseMessage"": ""The operation was successful"",
            ""language"": ""EN"",
            ""responseCode"": ""000"",
            ""detailedResponseCode"": ""000""
        }";

        [Test]
        public async Task CreateSessionAsync_PostsToSessionEndpointWithBasicAuth()
        {
            var handler = new SequencedHandler(new[] { SessionJson });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            var result = await client.CreateSessionAsync(SessionRequest());

            Assert.AreEqual("0b02b503-fbf8-49dd-cdec-08dcdb056790", result.Session.Id);
            Assert.AreEqual("Initiated", result.Session.Status);
            Assert.IsTrue(result.IsSuccess);

            Assert.AreEqual(1, handler.Requests.Count);
            var req = handler.Requests[0];
            Assert.AreEqual("POST", req.Method);
            Assert.IsTrue(req.Url.EndsWith("/payment-intent/api/v2/direct/session"));

            // Basic auth: base64("merchantPublicKey:apiPassword")
            Assert.IsTrue(req.Authorization.StartsWith("Basic "));
            var decoded = System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(req.Authorization.Substring("Basic ".Length)));
            Assert.AreEqual("11111111-2222-3333-4444-555555555555:test-api-password", decoded);
        }

        [Test]
        public async Task CreateSessionAsync_IncludesHmacSignatureInBody()
        {
            var handler = new SequencedHandler(new[] { SessionJson });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            await client.CreateSessionAsync(SessionRequest());

            var body = handler.Requests[0].Body;
            Assert.IsTrue(body.Contains("\"signature\""));
            // The signature must NOT be empty.
            Assert.IsFalse(body.Contains("\"signature\":null"));
            Assert.IsFalse(body.Contains("\"signature\":\"\""));
        }

        [Test]
        public async Task CreateSessionAsync_AutoGeneratesTimestampWhenMissing()
        {
            var handler = new SequencedHandler(new[] { SessionJson });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            var request = SessionRequest();
            request.Timestamp = null;

            await client.CreateSessionAsync(request);

            Assert.IsNotNull(request.Timestamp);
            Assert.IsNotEmpty(request.Signature);
            Assert.IsTrue(handler.Requests[0].Body.Contains(request.Timestamp));
        }

        [Test]
        public void CreateSessionAsync_ThrowsGeideaApiExceptionOnGatewayError()
        {
            var handler = new SequencedHandler(new[]
            {
                @"{ ""responseMessage"": ""Failed"", ""detailedResponseMessage"": ""Invalid signature"",
                    ""responseCode"": ""101"", ""detailedResponseCode"": ""101"" }"
            });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            var ex = Assert.ThrowsAsync<GeideaApiException>(async () =>
                await client.CreateSessionAsync(SessionRequest()));

            Assert.AreEqual("101", ex.ResponseCode);
            Assert.AreEqual("Invalid signature", ex.DetailedResponseMessage);
        }

        [Test]
        public void CreateSessionAsync_ValidatesRequiredFields()
        {
            var client = new GeideaClient(Options(), new HttpClient(new SequencedHandler(new string[0])));

            Assert.ThrowsAsync<System.ArgumentException>(() =>
                client.CreateSessionAsync(new CreateSessionRequest { Amount = 0, Currency = "EGP", CallbackUrl = "https://x.com" }));
            Assert.ThrowsAsync<System.ArgumentException>(() =>
                client.CreateSessionAsync(new CreateSessionRequest { Amount = 10, Currency = "", CallbackUrl = "https://x.com" }));
            Assert.ThrowsAsync<System.ArgumentException>(() =>
                client.CreateSessionAsync(new CreateSessionRequest { Amount = 10, Currency = "EGP", CallbackUrl = "" }));
        }

        [Test]
        public async Task GetOrderAsync_CallsFetchOrderEndpoint()
        {
            var orderId = "47bbd481-fdc0-44e2-5dda-08db15bcae4b";
            var handler = new SequencedHandler(new[]
            {
                @"{ ""order"": { ""orderId"": """ + orderId + @""", ""amount"": 123.21, ""currency"": ""SAR"",
                    ""detailedStatus"": ""Paid"", ""status"": ""Success"", ""transactions"": [] },
                   ""responseCode"": ""000"", ""responseMessage"": ""Success"" }"
            });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            var result = await client.GetOrderAsync(orderId);

            Assert.AreEqual(orderId, result.Order.OrderId);
            Assert.AreEqual("Paid", result.Order.DetailedStatus);
            Assert.AreEqual(123.21m, result.Order.Amount);

            Assert.AreEqual(1, handler.Requests.Count);
            Assert.AreEqual("GET", handler.Requests[0].Method);
            Assert.IsTrue(handler.Requests[0].Url.EndsWith("/pgw/api/v1/direct/order/" + orderId));
        }

        [Test]
        public async Task RefundAsync_PostsToRefundEndpointWithSignature()
        {
            var orderId = "47bbd481-fdc0-44e2-5dda-08db15bcae4b";
            var handler = new SequencedHandler(new[]
            {
                @"{ ""order"": { ""orderId"": """ + orderId + @""", ""amount"": 123.21, ""currency"": ""SAR"",
                    ""detailedStatus"": ""PartiallyRefunded"", ""status"": ""Success"" },
                   ""responseCode"": ""000"", ""responseMessage"": ""Success"" }"
            });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            var result = await client.RefundAsync(new RefundRequest
            {
                OrderId = orderId,
                RefundAmount = 50.00m,
                Timestamp = "2026-01-15T10:30:00Z"
            });

            Assert.AreEqual("PartiallyRefunded", result.Order.DetailedStatus);

            var req = handler.Requests[0];
            Assert.IsTrue(req.Url.EndsWith("/pgw/api/v2/direct/refund"));
            Assert.IsTrue(req.Body.Contains("\"signature\""));
            Assert.IsFalse(req.Body.Contains("\"signature\":null"));
        }

        [Test]
        public async Task CancelOrderAsync_Accepts010AsSuccess()
        {
            var orderId = "ddaf3eb8-645e-43d3-99ef-08db103ef12c";
            var handler = new SequencedHandler(new[]
            {
                @"{ ""orderId"": """ + orderId + @""", ""responseMessage"": ""Cancelled"",
                    ""detailedResponseMessage"": ""Order cancelled by user"",
                    ""language"": ""en"",
                    ""responseCode"": ""010"", ""detailedResponseCode"": ""001"" }"
            });
            var client = new GeideaClient(Options(), new HttpClient(handler));

            // Geidea reports a successful cancellation with responseCode "010" — must not throw.
            var result = await client.CancelOrderAsync(orderId, "CancelledByUser");

            Assert.AreEqual(orderId, result.OrderId);
            Assert.AreEqual("010", result.ResponseCode);

            var req = handler.Requests[0];
            Assert.AreEqual("POST", req.Method);
            Assert.IsTrue(req.Url.EndsWith("/pgw/api/v1/direct/cancel"));
            Assert.IsTrue(req.Body.Contains(orderId));
        }

        [Test]
        public void GetCheckoutUrl_BuildsEnvironmentSpecificUrl()
        {
            var sessionId = "0b02b503-fbf8-49dd-cdec-08dcdb056790";

            var egypt = new GeideaClient(Options(), new HttpClient(new SequencedHandler(new string[0])));
            Assert.AreEqual(
                "https://www.merchant.geidea.net/hpp/checkout/?" + sessionId,
                egypt.GetCheckoutUrl(sessionId));

            var saudiOptions = Options();
            saudiOptions.Environment = GeideaEnvironment.Saudi;
            var saudi = new GeideaClient(saudiOptions, new HttpClient(new SequencedHandler(new string[0])));
            Assert.AreEqual(
                "https://www.ksamerchant.geidea.net/hpp/checkout/?" + sessionId,
                saudi.GetCheckoutUrl(sessionId));

            var uaeOptions = Options();
            uaeOptions.Environment = GeideaEnvironment.UAE;
            var uae = new GeideaClient(uaeOptions, new HttpClient(new SequencedHandler(new string[0])));
            Assert.AreEqual(
                "https://payments.geidea.ae/hpp/checkout/?" + sessionId,
                uae.GetCheckoutUrl(sessionId));
        }

        [Test]
        public void BaseUrl_ResolvesPerEnvironment()
        {
            Assert.AreEqual("https://api.merchant.geidea.net", Options().BaseUrl);

            var saudi = Options();
            saudi.Environment = GeideaEnvironment.Saudi;
            Assert.AreEqual("https://api.ksamerchant.geidea.net", saudi.BaseUrl);

            var uae = Options();
            uae.Environment = GeideaEnvironment.UAE;
            Assert.AreEqual("https://api.geidea.ae", uae.BaseUrl);
        }

        [Test]
        public void Constructor_RejectsMissingCredentials()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new GeideaClient(new GeideaClientOptions { ApiPassword = "x" }));
            Assert.Throws<System.InvalidOperationException>(() =>
                new GeideaClient(new GeideaClientOptions { MerchantPublicKey = "x" }));
        }
    }
}
