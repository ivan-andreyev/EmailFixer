using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;
using Stripe;

namespace EmailFixer.Infrastructure.Services.Payment;

/// <summary>
/// Implementation of Stripe payment processing service
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly ICreditTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly string _webhookSecret;

    /// <summary>
    /// Price per 100 email checks in USD
    /// </summary>
    private const decimal PricePerHundredChecks = 1.00m;

    /// <summary>
    /// Initializes a new instance of StripePaymentService
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="transactionRepository">Credit transaction repository</param>
    /// <param name="userRepository">User repository</param>
    /// <param name="webhookSecret">Stripe webhook secret for signature validation</param>
    public StripePaymentService(
        ILogger<StripePaymentService> logger,
        ICreditTransactionRepository transactionRepository,
        IUserRepository userRepository,
        string webhookSecret)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _webhookSecret = webhookSecret ?? throw new ArgumentNullException(nameof(webhookSecret));
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid userId,
        decimal amount,
        int emailsCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating payment intent for user {UserId}, amount: {Amount}, emails: {EmailsCount}",
                userId, amount, emailsCount);

            // Validate amount matches expected price
            var expectedAmount = CalculateAmount(emailsCount);
            if (Math.Abs(amount - expectedAmount) > 0.01m)
            {
                throw new InvalidOperationException(
                    $"Amount mismatch. Expected {expectedAmount:F2} for {emailsCount} checks, got {amount:F2}");
            }

            // Get user to retrieve or create Stripe customer
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            // Ensure user has Stripe customer ID
            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", userId.ToString() }
                    }
                }, cancellationToken: cancellationToken);

                user.StripeCustomerId = customer.Id;
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}",
                    customer.Id, userId);
            }

            // Create payment intent
            var paymentIntentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = "usd",
                Customer = user.StripeCustomerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId.ToString() },
                    { "EmailsCount", emailsCount.ToString() },
                    { "CreditsCount", emailsCount.ToString() }
                }
            };

            var paymentIntent = await paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

            // Create pending transaction record
            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreditsChange = emailsCount,
                Amount = amount,
                Type = TransactionType.Purchase,
                Description = $"Purchase of {emailsCount} email checks",
                StripePaymentIntentId = paymentIntent.Id,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _transactionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment intent {PaymentIntentId} created successfully for user {UserId}",
                paymentIntent.Id, userId);

            return new PaymentIntentResult(
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                amount,
                emailsCount);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe error creating payment intent for user {UserId}: {ErrorMessage}",
                userId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating payment intent for user {UserId}",
                userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionResult> HandlePaymentSuccessAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing successful payment for intent {PaymentIntentId}", paymentIntentId);

            // Find transaction by payment intent ID
            var transaction = await _transactionRepository.GetByStripePaymentIntentIdAsync(
                paymentIntentId, cancellationToken);

            if (transaction == null)
            {
                throw new InvalidOperationException(
                    $"Transaction not found for payment intent {paymentIntentId}");
            }

            // Check if already processed
            if (transaction.Status == PaymentStatus.Completed)
            {
                _logger.LogWarning(
                    "Payment intent {PaymentIntentId} already processed, skipping",
                    paymentIntentId);

                return new TransactionResult(
                    transaction.Id,
                    transaction.UserId,
                    transaction.CreditsChange,
                    transaction.Amount,
                    transaction.CompletedAt ?? DateTime.UtcNow);
            }

            // Update transaction status
            transaction.Status = PaymentStatus.Completed;
            transaction.CompletedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            // Add credits to user account
            var user = await _userRepository.GetByIdAsync(transaction.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"User {transaction.UserId} not found");
            }

            user.CreditsAvailable += transaction.CreditsChange;
            user.TotalSpent += transaction.Amount;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Save all changes
            await _transactionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed payment {PaymentIntentId}. Added {Credits} credits to user {UserId}",
                paymentIntentId, transaction.CreditsChange, transaction.UserId);

            return new TransactionResult(
                transaction.Id,
                transaction.UserId,
                transaction.CreditsChange,
                transaction.Amount,
                transaction.CompletedAt.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing successful payment for intent {PaymentIntentId}",
                paymentIntentId);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(
        string json,
        string stripeSignature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating webhook signature");

            // Validate webhook signature using Stripe SDK
            EventUtility.ConstructEvent(json, stripeSignature, _webhookSecret);

            _logger.LogDebug("Webhook signature validated successfully");
            return Task.FromResult(true);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook signature: {ErrorMessage}", ex.Message);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Calculates the amount in USD for given number of email checks
    /// Formula: $1.00 per 100 checks
    /// </summary>
    /// <param name="emailsCount">Number of email checks</param>
    /// <returns>Amount in USD</returns>
    private static decimal CalculateAmount(int emailsCount)
    {
        return (emailsCount / 100m) * PricePerHundredChecks;
    }
}
