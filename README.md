# Geidea.Net

[![NuGet](https://img.shields.io/nuget/v/Geidea.Net.svg)](https://www.nuget.org/packages/Geidea.Net)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0-purple.svg)](https://dotnet.microsoft.com)

The missing .NET SDK for [Geidea](https://www.geidea.net) — the MENA payment gateway operating across **Saudi Arabia, Egypt, and the UAE**.

Accept card and wallet payments through Geidea's hosted checkout, manage orders, refunds and cancellations server-side, and validate payment callbacks with HMAC-SHA256 signatures.

## Install

```bash
dotnet add package Geidea.Net
```

## Quick start

```csharp
using Geidea;
using Geidea.Models;

var client = new GeideaClient(new GeideaClientOptions
{
    MerchantPublicKey = "your-merchant-public-key",
    ApiPassword = "your-api-password",   // keep server-side only — never in frontend code
    Environment = GeideaEnvironment.Egypt, // Saudi | Egypt | UAE
});

// 1. Create a hosted-checkout session (signature is generated automatically)
var session = await client.CreateSessionAsync(new CreateSessionRequest
{
    Amount = 250.00m,
    Currency = "EGP",
    MerchantReferenceId = "order-1001",
    CallbackUrl = "https://myshop.com/api/payments/geidea/callback",
    ReturnUrl = "https://myshop.com/checkout/done",
    Language = "ar",
    Customer = new GeideaCustomer
    {
        Email = "customer@example.com",
        FirstName = "Ahmed",
        LastName = "Hassan",
        PhoneNumber = "1012345678",
        PhoneCountryCode = "+20",
    },
});

// 2. Redirect the customer to the hosted checkout page
var checkoutUrl = client.GetCheckoutUrl(session.Session.Id);
return Redirect(checkoutUrl);
```

## Handling the callback

Geidea POSTs the payment result to your `callbackUrl`. **Always verify the signature** before fulfilling the order:

```csharp
using Geidea.Models;
using Geidea.Webhooks;
using System.Text.Json;

[HttpPost("api/payments/geidea/callback")]
public async Task<IActionResult> GeideaCallback()
{
    using var reader = new StreamReader(Request.Body);
    var json = await reader.ReadToEndAsync();

    // 1. Verify the HMAC-SHA256 signature
    if (!GeideaCallbackValidator.IsValidCallback(publicKey, apiPassword, json))
        return Unauthorized();

    var callback = JsonSerializer.Deserialize<GeideaCallback>(json);

    // 2. Never trust the callback alone — check response codes and amounts
    if (!callback.IsSuccess)
        return Ok(); // log it, don't fulfill

    var order = await _orders.FindByReferenceAsync(callback.MerchantReferenceId);
    if (order == null || order.Amount != callback.Amount || order.Currency != callback.Currency)
        return BadRequest(); // amount/currency/reference mismatch — don't fulfill

    await _orders.MarkPaidAsync(order.Id, callback.OrderId);
    return Ok();
}
```

## Transaction management

```csharp
// Fetch an order with its full transaction history
var order = await client.GetOrderAsync("geidea-order-id");

// Refund (full or partial) — signature generated automatically
var refund = await client.RefundAsync(new RefundRequest
{
    OrderId = "geidea-order-id",
    RefundAmount = 50.00m,
});

// Cancel an initiated order (before payment completes)
var cancel = await client.CancelOrderAsync("geidea-order-id", "CancelledByUser");
```

## ASP.NET Core DI

```csharp
services.AddGeidea(options =>
{
    options.MerchantPublicKey = configuration["Geidea:MerchantPublicKey"];
    options.ApiPassword = configuration["Geidea:ApiPassword"];
    options.Environment = GeideaEnvironment.Saudi;
});
```

## Regional environments

| Region | API base URL | Checkout host |
|--------|-------------|---------------|
| Saudi Arabia | `https://api.ksamerchant.geidea.net` | `https://www.ksamerchant.geidea.net` |
| Egypt | `https://api.merchant.geidea.net` | `https://www.merchant.geidea.net` |
| UAE | `https://api.geidea.ae` | `https://payments.geidea.ae` |

## Security notes

- Your **API password is a secret**. Store it in a vault, environment variable, or user-secrets — never commit it, never send it to the browser.
- Create sessions **server-to-server only**. The frontend should only ever see the `sessionId`.
- Geidea signs callbacks with HMAC-SHA256; this SDK validates them with a constant-time comparison.
- Each operation (session, refund, callback) uses its own signature field list — signatures are never interchangeable.

## API coverage

- ✅ Create checkout session (HPP) with automatic HMAC-SHA256 signing
- ✅ Fetch order with transaction history
- ✅ Refund (full / partial) with automatic signing
- ✅ Cancel order
- ✅ Callback signature validation (constant-time)
- ✅ Hosted checkout URL + JS library URL builders per region
- ✅ ASP.NET Core DI extensions
- ✅ Typed `GeideaApiException` with response codes

## License

MIT — see [LICENSE](LICENSE).
