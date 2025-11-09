using EmailFixer.Api.Models;
using EmailFixer.Infrastructure.Data.Entities;
using EmailFixer.Infrastructure.Data.Repositories;
using EmailFixer.Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EmailFixer.Api.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaddlePaymentService _paddleService;
    private readonly IUserRepository _userRepository;
    private readonly ICreditTransactionRepository _transactionRepository;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaddlePaymentService paddleService,
        IUserRepository userRepository,
        ICreditTransactionRepository transactionRepository,
        ILogger<PaymentController> logger)
    {
        _paddleService = paddleService;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a Paddle checkout for purchasing email credits
    /// </summary>
    /// <param name="request">Checkout creation request</param>
    /// <returns>Checkout URL and details</returns>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CreateCheckoutResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request)
    {
        _logger.LogInformation("Creating checkout for user {UserId} for {EmailsCount} emails",
            request.UserId, request.EmailsCount);

        // Validate emails count
        if (request.EmailsCount < 100)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Minimum purchase is 100 email credits",
                Details = $"Requested: {request.EmailsCount}"
            });
        }

        if (request.EmailsCount > 100000)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Maximum purchase is 100,000 email credits",
                Details = $"Requested: {request.EmailsCount}"
            });
        }

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        try
        {
            // Create Paddle checkout
            var result = await _paddleService.CreateCheckoutUrlAsync(request.UserId, request.EmailsCount);

            _logger.LogInformation("Checkout created successfully. Transaction ID: {TransactionId}",
                result.TransactionId);

            return Ok(new CreateCheckoutResponse
            {
                CheckoutUrl = result.CheckoutUrl,
                TransactionId = result.TransactionId,
                Amount = result.Amount,
                CreditsCount = result.CreditsCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Paddle checkout for user {UserId}", request.UserId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "Failed to create checkout",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Handle Paddle webhook for transaction events
    /// </summary>
    /// <returns>Webhook processing result</returns>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(WebhookProcessedResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> HandleWebhook()
    {
        _logger.LogInformation("Received Paddle webhook");

        try
        {
            // Read raw body
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Webhook body is empty");
                return BadRequest(new ErrorResponse { Message = "Empty webhook body" });
            }

            // Get signature from header
            var signature = Request.Headers["Paddle-Signature"].ToString();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing Paddle-Signature header");
                return BadRequest(new ErrorResponse { Message = "Missing signature" });
            }

            // Validate signature
            var isValidSignature = await _paddleService.ValidateWebhookSignatureAsync(body, signature);
            if (!isValidSignature)
            {
                _logger.LogWarning("Invalid webhook signature");
                return Unauthorized(new ErrorResponse { Message = "Invalid signature" });
            }

            // Process webhook
            var result = await _paddleService.HandleTransactionWebhookAsync(body);

            if (!result.Success)
            {
                _logger.LogError("Webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest(new ErrorResponse
                {
                    Message = "Webhook processing failed",
                    Details = result.ErrorMessage
                });
            }

            _logger.LogInformation("Webhook processed successfully. User {UserId} received {Credits} credits",
                result.UserId, result.CreditsAdded);

            return Ok(new WebhookProcessedResponse
            {
                Success = result.Success,
                TransactionId = result.TransactionId,
                UserId = result.UserId,
                CreditsAdded = result.CreditsAdded
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new ErrorResponse
            {
                Message = "Internal server error",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get transaction history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of transactions</returns>
    [HttpGet("transactions/{userId}")]
    [ProducesResponseType(typeof(List<CreditTransaction>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetTransactions(Guid userId)
    {
        _logger.LogInformation("Getting transactions for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return NotFound(new ErrorResponse { Message = "User not found" });
        }

        var transactions = await _transactionRepository.GetByUserIdAsync(userId);

        _logger.LogInformation("Retrieved {Count} transactions for user {UserId}",
            transactions.Count, userId);

        return Ok(transactions);
    }
}
