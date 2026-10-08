using Geidea.Signature;
using NUnit.Framework;

namespace Geidea.Net.Tests
{
    [TestFixture]
    public class GeideaSignatureTests
    {
        private const string MerchantPublicKey = "11111111-2222-3333-4444-555555555555";
        private const string ApiPassword = "test-api-password";
        private const string Timestamp = "2026-01-15T10:30:00Z";
        private const string OrderId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        [Test]
        public void GenerateSessionSignature_MatchesDocumentedAlgorithm()
        {
            // Vector computed with the algorithm from Geidea's docs:
            // Base64(HMAC-SHA256(apiPassword, merchantPublicKey + amount(F2) + currency + merchantReferenceId + timestamp))
            var signature = GeideaSignature.GenerateSessionSignature(
                MerchantPublicKey, 100.00m, "EGP", "order-123", Timestamp, ApiPassword);

            Assert.AreEqual("IwS1c/UyoYhziCrcEQUVRQJwgfdpS8RPiCDvUZ6kvPw=", signature);
        }

        [Test]
        public void GenerateSessionSignature_FormatsAmountWithTwoDecimals()
        {
            // 100 and 100.00 must produce the same signature — amounts are always F2.
            var a = GeideaSignature.GenerateSessionSignature(MerchantPublicKey, 100m, "EGP", "r", Timestamp, ApiPassword);
            var b = GeideaSignature.GenerateSessionSignature(MerchantPublicKey, 100.00m, "EGP", "r", Timestamp, ApiPassword);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void GenerateRefundSignature_MatchesDocumentedAlgorithm()
        {
            // Base64(HMAC-SHA256(apiPassword, timestamp + merchantPublicKey + refundAmount(F2) + orderId))
            var signature = GeideaSignature.GenerateRefundSignature(
                Timestamp, MerchantPublicKey, 50.50m, OrderId, ApiPassword);

            Assert.AreEqual("JdBm45UPf/TMyWmiujaC5tUFXV75xYVD5aC+JvMdZxc=", signature);
        }

        [Test]
        public void GenerateCallbackSignature_MatchesDocumentedAlgorithm()
        {
            // Base64(HMAC-SHA256(apiPassword, merchantPublicKey + amount(F2) + currency + orderId + status + merchantReferenceId + timestamp))
            var signature = GeideaSignature.GenerateCallbackSignature(
                MerchantPublicKey, 100.00m, "EGP", OrderId, "Success", "order-123", Timestamp, ApiPassword);

            Assert.AreEqual("6U34L0Tu3n5GjB2WdUU2EFOVUL7s5Dx3U66Qyg/Cs7I=", signature);
        }

        [Test]
        public void Signatures_DifferPerOperation()
        {
            // Operation-specific field lists must not be interchangeable.
            var session = GeideaSignature.GenerateSessionSignature(MerchantPublicKey, 100m, "EGP", "r", Timestamp, ApiPassword);
            var refund = GeideaSignature.GenerateRefundSignature(Timestamp, MerchantPublicKey, 100m, OrderId, ApiPassword);
            var callback = GeideaSignature.GenerateCallbackSignature(MerchantPublicKey, 100m, "EGP", OrderId, "Success", "r", Timestamp, ApiPassword);

            Assert.AreNotEqual(session, refund);
            Assert.AreNotEqual(session, callback);
            Assert.AreNotEqual(refund, callback);
        }

        [Test]
        public void FormatAmount_AlwaysTwoDecimalsInvariant()
        {
            Assert.AreEqual("100.00", GeideaSignature.FormatAmount(100m));
            Assert.AreEqual("100.50", GeideaSignature.FormatAmount(100.5m));
            Assert.AreEqual("0.01", GeideaSignature.FormatAmount(0.01m));
        }

        [Test]
        public void GenerateTimestamp_ReturnsNonEmptyValue()
        {
            var ts = GeideaSignature.GenerateTimestamp();
            Assert.IsNotNull(ts);
            Assert.IsNotEmpty(ts);
        }
    }
}
