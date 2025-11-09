using System.Text.Json.Serialization;

namespace EmailFixer.Infrastructure.Services.Payment.Paddle;

/// <summary>
/// DTO for Paddle transaction data
/// Represents transaction information from Paddle API
/// </summary>
public class PaddleTransactionDto
{
    /// <summary>
    /// Paddle transaction ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Transaction status (completed, billed, paid, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID who made the purchase
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, etc.)
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Details about the transaction
    /// </summary>
    [JsonPropertyName("details")]
    public PaddleTransactionDetails? Details { get; set; }

    /// <summary>
    /// Custom data passed during checkout creation
    /// </summary>
    [JsonPropertyName("custom_data")]
    public PaddleCustomData? CustomData { get; set; }

    /// <summary>
    /// Checkout information
    /// </summary>
    [JsonPropertyName("checkout")]
    public PaddleCheckout? Checkout { get; set; }
}

/// <summary>
/// Transaction details including totals
/// </summary>
public class PaddleTransactionDetails
{
    /// <summary>
    /// Line items in the transaction
    /// </summary>
    [JsonPropertyName("line_items")]
    public List<PaddleLineItem>? LineItems { get; set; }

    /// <summary>
    /// Totals for the transaction
    /// </summary>
    [JsonPropertyName("totals")]
    public PaddleTotals? Totals { get; set; }
}

/// <summary>
/// Line item in a transaction
/// </summary>
public class PaddleLineItem
{
    /// <summary>
    /// Price information
    /// </summary>
    [JsonPropertyName("price")]
    public PaddlePrice? Price { get; set; }

    /// <summary>
    /// Quantity purchased
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

/// <summary>
/// Price information for a line item
/// </summary>
public class PaddlePrice
{
    /// <summary>
    /// Price ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unit price amount
    /// </summary>
    [JsonPropertyName("unit_price")]
    public PaddleUnitPrice? UnitPrice { get; set; }
}

/// <summary>
/// Unit price details
/// </summary>
public class PaddleUnitPrice
{
    /// <summary>
    /// Amount in smallest currency unit (e.g., cents for USD)
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0";

    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>
/// Transaction totals
/// </summary>
public class PaddleTotals
{
    /// <summary>
    /// Subtotal amount
    /// </summary>
    [JsonPropertyName("subtotal")]
    public string Subtotal { get; set; } = "0";

    /// <summary>
    /// Total amount charged
    /// </summary>
    [JsonPropertyName("total")]
    public string Total { get; set; } = "0";

    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>
/// Custom data passed to Paddle
/// Used to store application-specific information
/// </summary>
public class PaddleCustomData
{
    /// <summary>
    /// User ID from our system
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Number of email credits purchased
    /// </summary>
    [JsonPropertyName("credits_count")]
    public int CreditsCount { get; set; }
}

/// <summary>
/// Checkout information
/// </summary>
public class PaddleCheckout
{
    /// <summary>
    /// Checkout URL for user to complete payment
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Response from Paddle transaction creation API
/// </summary>
public class PaddleTransactionResponse
{
    /// <summary>
    /// Transaction data
    /// </summary>
    [JsonPropertyName("data")]
    public PaddleTransactionDto? Data { get; set; }
}
