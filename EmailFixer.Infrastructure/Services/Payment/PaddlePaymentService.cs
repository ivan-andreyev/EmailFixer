using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using EmailFixer.Infrastructure.Services.Payment.Paddle;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EmailFixer.Infrastructure.Services.Payment;

/// <summary>
/// Implementation of Paddle.com payment processing service
/// Handles checkout creation, webhook processing, and signature validation
/// </summary>
public class PaddlePaymentService : IPaddlePaymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaddleConfiguration _configuration;
    private readonly ILogger<PaddlePaymentService> _logger;
    private readonly ICreditTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of PaddlePaymentService
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for API calls</param>
    /// <param name="configuration">Paddle configuration settings</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="transactionRepository">Credit transaction repository</param>
    /// <param name="userRepository">User repository</param>
    public PaddlePaymentService(
        IHttpClientFactory httpClientFactory,
        PaddleConfiguration configuration,
        ILogger<PaddlePaymentService> logger,
        ICreditTransactionRepository transactionRepository,
        IUserRepository userRepository)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        _httpClient = _httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration.ApiKey}");
    }

    /// <inheritdoc />
    public async Task<PaddleCheckoutResult> CreateCheckoutUrlAsync(
        Guid userId,
        int emailsCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating Paddle checkout for user {UserId}, {EmailsCount} credits", userId, emailsCount);

            // Calculate price: $1.00 per 100 credits
            var amount = Math.Round((decimal)emailsCount / 100m, 2);

            // Verify user exists
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogError("User {UserId} not found", userId);
                throw new InvalidOperationException($"User {userId} not found");
            }

            // Create pending transaction in database
            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditsChange = emailsCount,
                Amount = amount,
                Type = TransactionType.Purchase,
                Description = $"Purchase of {emailsCount} email credits",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            // Prepare Paddle API request
            // NOTE: In real implementation, you would need a price_id from Paddle dashboard
            // For now, we'll use a placeholder that needs to be configured
            var request = new PaddleCreateTransactionRequest
            {
                Items = new List<PaddleTransactionItemRequest>
                {
                    new PaddleTransactionItemRequest
                    {
                        PriceId = "pri_01hsdn9w7k3x8sj8f3kjwfd6vb", // PLACEHOLDER - should be from configuration
                        Quantity = emailsCount / 100 // Quantity of $1 packages
                    }
                },
                CustomData = new PaddleCustomData
                {
                    UserId = userId.ToString(),
                    CreditsCount = emailsCount
                }
            };

            // Make API call to Paddle
            var response = await _httpClient.PostAsJsonAsync(
                "/transactions",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Paddle API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException(
                    $"Paddle API returned {response.StatusCode}: {errorContent}");
            }

            // Parse response
            var paddleResponse = await response.Content.ReadFromJsonAsync<PaddleTransactionResponse>(
                cancellationToken: cancellationToken);

            if (paddleResponse?.Data == null)
            {
                _logger.LogError("Invalid Paddle response: missing transaction data");
                throw new InvalidOperationException("Invalid response from Paddle API");
            }

            // Update transaction with Paddle transaction ID
            transaction.PaddleTransactionId = paddleResponse.Data.Id;
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            var checkoutUrl = paddleResponse.Data.Checkout?.Url
                ?? throw new InvalidOperationException("No checkout URL in Paddle response");

            _logger.LogInformation(
                "Paddle checkout created successfully. Transaction ID: {TransactionId}, Checkout URL created",
                paddleResponse.Data.Id);

            return new PaddleCheckoutResult(
                CheckoutUrl: checkoutUrl,
                TransactionId: paddleResponse.Data.Id,
                Amount: amount,
                CreditsCount: emailsCount);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not HttpRequestException)
        {
            _logger.LogError(ex, "Error creating Paddle checkout for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PaddleWebhookResult> HandleTransactionWebhookAsync(
        string webhookPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Paddle webhook");

            // Validate input
            if (string.IsNullOrWhiteSpace(webhookPayload))
            {
                _logger.LogWarning("Webhook payload is null or empty");
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: null,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: null,
                    ErrorMessage: "Webhook payload is null or empty");
            }

            // Deserialize webhook payload
            PaddleWebhookDto? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<PaddleWebhookDto>(webhookPayload);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize webhook payload");
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: null,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: null,
                    ErrorMessage: $"Invalid JSON: {ex.Message}");
            }

            if (webhook?.Data == null)
            {
                _logger.LogWarning("Webhook data is null");
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: null,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: webhook?.EventType,
                    ErrorMessage: "Webhook data is null");
            }

            _logger.LogInformation("Processing webhook event: {EventType}, Transaction: {TransactionId}",
                webhook.EventType, webhook.Data.Id);

            // Check if event type is supported
            if (webhook.EventType != "transaction.completed" && webhook.EventType != "transaction.updated")
            {
                _logger.LogInformation("Ignoring unsupported event type: {EventType}", webhook.EventType);
                return new PaddleWebhookResult(
                    Success: true,
                    TransactionId: webhook.Data.Id,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: null);
            }

            // Only process if status is completed
            if (webhook.Data.Status != "completed")
            {
                _logger.LogInformation("Transaction status is {Status}, skipping credit addition",
                    webhook.Data.Status);
                return new PaddleWebhookResult(
                    Success: true,
                    TransactionId: webhook.Data.Id,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: null);
            }

            // Extract user ID and credits from custom data
            if (webhook.Data.CustomData == null ||
                string.IsNullOrWhiteSpace(webhook.Data.CustomData.UserId))
            {
                _logger.LogError("Missing user ID in webhook custom data");
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: webhook.Data.Id,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: "Missing user ID in custom data");
            }

            if (!Guid.TryParse(webhook.Data.CustomData.UserId, out var userId))
            {
                _logger.LogError("Invalid user ID format: {UserId}", webhook.Data.CustomData.UserId);
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: webhook.Data.Id,
                    UserId: null,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: "Invalid user ID format");
            }

            var creditsToAdd = webhook.Data.CustomData.CreditsCount;

            // Find transaction by Paddle transaction ID
            var transaction = await _transactionRepository.GetByPaddleTransactionIdAsync(
                webhook.Data.Id,
                cancellationToken);

            if (transaction == null)
            {
                _logger.LogError("Transaction not found for Paddle ID: {TransactionId}", webhook.Data.Id);
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: webhook.Data.Id,
                    UserId: userId,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: "Transaction not found");
            }

            // Idempotency check: if already completed, don't process again
            if (transaction.Status == PaymentStatus.Completed)
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} already completed (idempotent webhook), skipping",
                    webhook.Data.Id);
                return new PaddleWebhookResult(
                    Success: true,
                    TransactionId: webhook.Data.Id,
                    UserId: userId,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: null);
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogError("User {UserId} not found", userId);
                return new PaddleWebhookResult(
                    Success: false,
                    TransactionId: webhook.Data.Id,
                    UserId: userId,
                    CreditsAdded: 0,
                    EventType: webhook.EventType,
                    ErrorMessage: "User not found");
            }

            // Update transaction status
            transaction.Status = PaymentStatus.Completed;
            transaction.CompletedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            // Add credits to user
            user.CreditsAvailable += creditsToAdd;
            user.TotalSpent += transaction.Amount;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation(
                "Successfully processed webhook. User {UserId} received {Credits} credits. Transaction {TransactionId} completed",
                userId, creditsToAdd, webhook.Data.Id);

            return new PaddleWebhookResult(
                Success: true,
                TransactionId: webhook.Data.Id,
                UserId: userId,
                CreditsAdded: creditsToAdd,
                EventType: webhook.EventType,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paddle webhook");
            return new PaddleWebhookResult(
                Success: false,
                TransactionId: null,
                UserId: null,
                CreditsAdded: 0,
                EventType: null,
                ErrorMessage: $"Internal error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(
        string body,
        string signature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Webhook signature validation failed: body is null or empty");
                throw new ArgumentException("Webhook body cannot be null or empty", nameof(body));
            }

            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Webhook signature validation failed: signature is null or empty");
                throw new ArgumentException("Webhook signature cannot be null or empty", nameof(signature));
            }

            // Compute HMAC-SHA256 hash of the body using webhook secret
            var secretBytes = Encoding.UTF8.GetBytes(_configuration.WebhookSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            using var hmac = new HMACSHA256(secretBytes);
            var computedHashBytes = hmac.ComputeHash(bodyBytes);
            var computedSignature = Convert.ToBase64String(computedHashBytes);

            // Perform constant-time comparison to prevent timing attacks
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(signature));

            if (isValid)
            {
                _logger.LogInformation("Webhook signature validation successful");
            }
            else
            {
                _logger.LogWarning("Webhook signature validation failed: signature mismatch");
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error during webhook signature validation");
            return Task.FromResult(false);
        }
    }
}
