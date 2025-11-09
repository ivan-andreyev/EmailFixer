# Paddle.com Payment Integration

## Overview

This document describes the Paddle.com payment integration for Email Fixer. The integration enables users to purchase email validation credits through Paddle's secure checkout process.

## Architecture

### Components

1. **IPaddlePaymentService** - Service interface for Paddle payment operations
2. **PaddlePaymentService** - Implementation of Paddle payment service
3. **PaddleConfiguration** - Configuration settings for Paddle API
4. **DTO Classes** - Data Transfer Objects for Paddle API communication
5. **CreditTransaction** - Entity for tracking payment transactions
6. **ICreditTransactionRepository** - Repository for transaction data access

### Flow Diagram

```
User Request -> CreateCheckoutUrlAsync -> Paddle API -> Checkout URL
                                       |
                                       v
                                  Pending Transaction (DB)

User Completes Payment -> Paddle Webhook -> HandleTransactionWebhookAsync
                                          |
                                          v
                                     Validate Signature
                                          |
                                          v
                                    Update Transaction
                                          |
                                          v
                                    Add Credits to User
```

## Configuration

### Required Settings

Add the following configuration to your `appsettings.json`:

```json
{
  "Paddle": {
    "ApiKey": "your-paddle-api-key",
    "SellerId": "your-seller-id",
    "WebhookSecret": "your-webhook-secret",
    "ApiBaseUrl": "https://api.paddle.com"
  }
}
```

### Dependency Injection Setup

In `Program.cs` or `Startup.cs`:

```csharp
// Register Paddle configuration
var paddleConfig = new PaddleConfiguration(
    apiKey: configuration["Paddle:ApiKey"],
    sellerId: configuration["Paddle:SellerId"],
    webhookSecret: configuration["Paddle:WebhookSecret"],
    apiBaseUrl: configuration["Paddle:ApiBaseUrl"] ?? "https://api.paddle.com"
);
services.AddSingleton(paddleConfig);

// Register Paddle service
services.AddScoped<IPaddlePaymentService, PaddlePaymentService>();

// Register HTTP client factory (if not already registered)
services.AddHttpClient();
```

## Pricing Model

- **Base Price**: $1.00 USD = 100 email validation credits
- **Currency**: USD (configurable via Paddle dashboard)
- **Minimum Purchase**: 100 credits (1 package)

### Calculating Price

```csharp
// Example: 500 credits
int emailsCount = 500;
decimal amount = Math.Round((decimal)emailsCount / 100m, 2); // $5.00
```

## API Usage

### 1. Creating a Checkout Session

```csharp
var checkoutResult = await _paddlePaymentService.CreateCheckoutUrlAsync(
    userId: userId,
    emailsCount: 500,
    cancellationToken: cancellationToken
);

// Redirect user to checkoutResult.CheckoutUrl
return Redirect(checkoutResult.CheckoutUrl);
```

**Response:**
```csharp
PaddleCheckoutResult {
    CheckoutUrl: "https://checkout.paddle.com/...",
    TransactionId: "txn_01hsdn...",
    Amount: 5.00,
    CreditsCount: 500
}
```

### 2. Handling Webhooks

Set up a webhook endpoint in your API controller:

```csharp
[HttpPost("api/webhooks/paddle")]
public async Task<IActionResult> HandlePaddleWebhook()
{
    // Read raw body
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync();

    // Get signature from header
    var signature = Request.Headers["Paddle-Signature"].ToString();

    // Validate signature
    var isValid = await _paddlePaymentService.ValidateWebhookSignatureAsync(
        body,
        signature,
        cancellationToken
    );

    if (!isValid)
    {
        return Unauthorized("Invalid signature");
    }

    // Process webhook
    var result = await _paddlePaymentService.HandleTransactionWebhookAsync(
        body,
        cancellationToken
    );

    if (!result.Success)
    {
        _logger.LogError("Webhook processing failed: {Error}", result.ErrorMessage);
        return BadRequest(result.ErrorMessage);
    }

    return Ok();
}
```

### 3. Signature Validation

The service automatically validates webhook signatures using HMAC-SHA256:

```csharp
var isValid = await _paddlePaymentService.ValidateWebhookSignatureAsync(
    body: webhookBody,
    signature: paddleSignatureHeader,
    cancellationToken: cancellationToken
);
```

## Webhook Setup

### Configure in Paddle Dashboard

1. Log in to Paddle dashboard
2. Go to **Developer Tools** → **Webhooks**
3. Add new webhook endpoint: `https://yourdomain.com/api/webhooks/paddle`
4. Select events:
   - `transaction.completed`
   - `transaction.updated`
5. Save webhook secret to configuration

### Supported Events

- **transaction.completed** - Payment successful, credits added to user
- **transaction.updated** - Transaction status changed

### Webhook Payload Example

```json
{
  "event_type": "transaction.completed",
  "event_id": "evt_01hsdn...",
  "occurred_at": "2025-11-09T12:00:00Z",
  "data": {
    "id": "txn_01hsdn...",
    "status": "completed",
    "customer_id": "ctm_01hsdn...",
    "currency_code": "USD",
    "custom_data": {
      "user_id": "123e4567-e89b-12d3-a456-426614174000",
      "credits_count": 500
    },
    "details": {
      "totals": {
        "total": "500",
        "currency_code": "USD"
      }
    }
  }
}
```

## Error Handling

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `Invalid signature` | Webhook secret mismatch | Verify webhook secret in configuration |
| `User not found` | Invalid user ID in webhook | Check user_id in custom_data |
| `Transaction not found` | Transaction ID not in database | Ensure checkout was created before webhook |
| `Paddle API error` | API key invalid or network issue | Verify API key and network connectivity |

### Idempotency

The service handles duplicate webhooks gracefully:
- Checks if transaction is already completed
- Skips credit addition if already processed
- Returns success to prevent webhook retries

## Testing

### Test Mode

Paddle provides a sandbox environment:

1. Use sandbox API key and seller ID
2. Update `ApiBaseUrl` to `https://sandbox-api.paddle.com`
3. Test webhooks using Paddle's webhook simulator

### Manual Testing

```bash
# Create test checkout
POST /api/payments/checkout
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "emailsCount": 100
}

# Simulate webhook (development only)
POST /api/webhooks/paddle
Headers:
  Paddle-Signature: [computed HMAC-SHA256]
Body:
  {
    "event_type": "transaction.completed",
    "data": { ... }
  }
```

## Troubleshooting

### Webhook Not Receiving

1. Check firewall/NAT rules
2. Verify endpoint is publicly accessible
3. Check Paddle dashboard webhook logs
4. Enable detailed logging in PaddlePaymentService

### Credits Not Added

1. Check webhook signature validation
2. Verify event type is `transaction.completed`
3. Check transaction status is `completed`
4. Review application logs for errors
5. Verify custom_data contains correct user_id

### API Errors

```
Error: Paddle API returned 401
Solution: Check API key is correct and not expired

Error: Paddle API returned 400
Solution: Check request payload format matches Paddle API spec

Error: No checkout URL in response
Solution: Verify price_id is configured in Paddle dashboard
```

## Security Considerations

1. **Signature Validation**: Always validate webhook signatures before processing
2. **HTTPS Only**: Only accept webhooks over HTTPS
3. **Secrets Management**: Store API keys and secrets securely (Azure Key Vault, AWS Secrets Manager, etc.)
4. **Constant-Time Comparison**: Signature validation uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks
5. **Logging**: Sensitive data (API keys, signatures) should not be logged

## Migration from Stripe

The integration maintains backward compatibility with Stripe:

- `CreditTransaction` entity has both `StripePaymentIntentId` and `PaddleTransactionId`
- `ICreditTransactionRepository` supports both lookup methods
- Existing Stripe transactions remain functional

To migrate:
1. Complete all pending Stripe transactions
2. Update configuration to use Paddle
3. Deploy new code with Paddle integration
4. Update checkout flow to use Paddle
5. (Optional) Migrate historical data by populating `PaddleTransactionId`

## References

- [Paddle API Documentation](https://developer.paddle.com/api-reference/overview)
- [Paddle Webhooks Guide](https://developer.paddle.com/webhooks/overview)
- [Paddle Transaction API](https://developer.paddle.com/api-reference/transactions/create-transaction)
- [Paddle Signature Validation](https://developer.paddle.com/webhooks/signature-verification)

## Support

For issues or questions:
- Check Paddle Dashboard webhook logs
- Review application logs
- Contact Paddle Support: https://www.paddle.com/support
