using System.Text.Json;
using Geidea.Models;
using Geidea.Webhooks;
using NUnit.Framework;

namespace Geidea.Net.Tests
{
    [TestFixture]
    public class GeideaCallbackValidatorTests
    {
        private const string MerchantPublicKey = "11111111-2222-3333-4444-555555555555";
        private const string ApiPassword = "test-api-password";

        private static string ValidCallbackJson() => @"{
            ""orderId"": ""aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"",
            ""merchantReferenceId"": ""order-123"",
            ""amount"": 100.00,
            ""currency"": ""EGP"",
            ""status"": ""Success"",
            ""timestamp"": ""2026-01-15T10:30:00Z"",
            ""signature"": ""6U34L0Tu3n5GjB2WdUU2EFOVUL7s5Dx3U66Qyg/Cs7I="",
            ""responseCode"": ""000"",
            ""responseMessage"": ""Success"",
            ""detailedResponseCode"": ""000"",
            ""detailedResponseMessage"": ""The operation was successful"",
            ""merchantPublicKey"": ""11111111-2222-3333-4444-555555555555""
        }";

        [Test]
        public void IsValidCallback_AcceptsGenuineCallback()
        {
            Assert.IsTrue(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, ValidCallbackJson()));
        }

        [Test]
        public void IsValidCallback_RejectsTamperedAmount()
        {
            var tampered = ValidCallbackJson().Replace(@"""amount"": 100.00", @"""amount"": 1.00");
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, tampered));
        }

        [Test]
        public void IsValidCallback_RejectsWrongPassword()
        {
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, "wrong-password", ValidCallbackJson()));
        }

        [Test]
        public void IsValidCallback_RejectsMalformedJson()
        {
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, "{not json"));
        }

        [Test]
        public void IsValidCallback_RejectsMissingSignature()
        {
            var noSig = ValidCallbackJson().Replace(
                @"""signature"": ""6U34L0Tu3n5GjB2WdUU2EFOVUL7s5Dx3U66Qyg/Cs7I="",", "");
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, noSig));
        }

        [Test]
        public void IsValidCallback_RejectsNullInputs()
        {
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(null, ApiPassword, ValidCallbackJson()));
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(MerchantPublicKey, null, ValidCallbackJson()));
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(MerchantPublicKey, ApiPassword, (string)null));
            Assert.IsFalse(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, (GeideaCallback)null));
        }

        [Test]
        public void IsValidCallback_WorksWithDeserializedObject()
        {
            var callback = JsonSerializer.Deserialize<GeideaCallback>(ValidCallbackJson());
            Assert.IsNotNull(callback);
            Assert.IsTrue(callback.IsSuccess);
            Assert.AreEqual("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", callback.OrderId);

            Assert.IsTrue(GeideaCallbackValidator.IsValidCallback(
                MerchantPublicKey, ApiPassword, callback));
        }
    }
}
