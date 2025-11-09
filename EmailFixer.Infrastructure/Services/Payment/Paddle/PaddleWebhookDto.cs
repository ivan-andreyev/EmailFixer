using System.Text.Json.Serialization;

namespace EmailFixer.Infrastructure.Services.Payment.Paddle;

/// <summary>
/// DTO for Paddle webhook payload
/// Represents webhook events from Paddle (transaction.completed, transaction.updated, etc.)
/// </summary>
public class PaddleWebhookDto
{
    /// <summary>
    /// Event type (e.g., "transaction.completed", "transaction.updated")
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event ID from Paddle
    /// </summary>
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// Timestamp when event occurred
    /// </summary>
    [JsonPropertyName("occurred_at")]
    public DateTime? OccurredAt { get; set; }

    /// <summary>
    /// Transaction data associated with this event
    /// </summary>
    [JsonPropertyName("data")]
    public PaddleTransactionDto? Data { get; set; }
}

/// <summary>
/// Request for creating Paddle transaction
/// </summary>
public class PaddleCreateTransactionRequest
{
    /// <summary>
    /// Items to purchase
    /// </summary>
    [JsonPropertyName("items")]
    public List<PaddleTransactionItemRequest> Items { get; set; } = new();

    /// <summary>
    /// Custom data to attach to transaction
    /// </summary>
    [JsonPropertyName("custom_data")]
    public PaddleCustomData? CustomData { get; set; }
}

/// <summary>
/// Item in transaction creation request
/// </summary>
public class PaddleTransactionItemRequest
{
    /// <summary>
    /// Price ID from Paddle dashboard
    /// For email credits: price_id for $1.00 = 100 credits
    /// </summary>
    [JsonPropertyName("price_id")]
    public string PriceId { get; set; } = string.Empty;

    /// <summary>
    /// Quantity to purchase
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
