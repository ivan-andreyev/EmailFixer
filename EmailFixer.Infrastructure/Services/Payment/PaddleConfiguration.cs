namespace EmailFixer.Infrastructure.Services.Payment;

/// <summary>
/// Configuration settings for Paddle.com payment integration
/// </summary>
public class PaddleConfiguration
{
    /// <summary>
    /// Paddle API key for authentication
    /// </summary>
    public string ApiKey { get; }

    /// <summary>
    /// Paddle Seller ID (merchant ID)
    /// </summary>
    public string SellerId { get; }

    /// <summary>
    /// Paddle webhook secret for signature validation
    /// </summary>
    public string WebhookSecret { get; }

    /// <summary>
    /// Paddle API base URL (default: https://api.paddle.com)
    /// </summary>
    public string ApiBaseUrl { get; }

    /// <summary>
    /// Initializes a new instance of PaddleConfiguration
    /// </summary>
    /// <param name="apiKey">Paddle API key</param>
    /// <param name="sellerId">Paddle Seller ID</param>
    /// <param name="webhookSecret">Paddle webhook secret</param>
    /// <param name="apiBaseUrl">Paddle API base URL (optional, defaults to https://api.paddle.com)</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty</exception>
    public PaddleConfiguration(
        string apiKey,
        string sellerId,
        string webhookSecret,
        string apiBaseUrl = "https://api.paddle.com")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(sellerId))
        {
            throw new ArgumentException("Seller ID cannot be null or empty", nameof(sellerId));
        }

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("Webhook secret cannot be null or empty", nameof(webhookSecret));
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentException("API base URL cannot be null or empty", nameof(apiBaseUrl));
        }

        ApiKey = apiKey;
        SellerId = sellerId;
        WebhookSecret = webhookSecret;
        ApiBaseUrl = apiBaseUrl;
    }
}
