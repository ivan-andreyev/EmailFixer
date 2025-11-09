# 💻 Phase 3: Blazor Client Development Coordinator

**Phase ID:** phase3-client
**Parent Plan:** [emailfixer-completion-plan.md](emailfixer-completion-plan.md)
**Duration:** 6 hours
**Dependencies:** Phase 2 API (partial - endpoints defined)
**Priority:** P0 - Critical Path
**Parallel Execution:** Components can be built independently

## 📋 Phase Overview

Build Blazor WebAssembly client with responsive UI components for email validation, user management, and payment integration. Focus on user experience and real-time validation feedback.

## ⚡ Parallel Component Development

```mermaid
Task 3.1 (Setup) [30 min]
    ├─→ Task 3.2 (Email Service) [40 min]
    ├─→ Task 3.5 (User Service) [45 min]
    └─→ Task 3.6 (Payment Service) [50 min]
         ├─→ Task 3.3 (Main Page) [60 min]
         ├─→ Task 3.4 (Suggestion Modal) [35 min]
         ├─→ Task 3.7 (History) [40 min]
         └─→ Task 3.8 (Navigation) [30 min]
              └─→ Task 3.9 (Error Handling) [35 min]
```

**Optimal distribution for 2 developers:**
- Dev A: 3.1 → 3.2 → 3.3 → 3.4 → 3.9
- Dev B: Wait for 3.1 → 3.5 → 3.6 → 3.7 → 3.8

## 📝 Task Breakdown

### Task 3.1: Blazor WebAssembly Setup
**Duration:** 30 minutes
**LLM Readiness:** 100%
**Blocking:** All client tasks

**File:** `EmailFixer.Client/Program.cs`

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EmailFixer.Client;
using EmailFixer.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001/")
});

// Register Services
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add Blazored.LocalStorage if using
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
```

**File:** `EmailFixer.Client/wwwroot/index.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Email Fixer - Professional Email Validation</title>
    <base href="/" />

    <!-- Bootstrap 5 -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">

    <!-- Custom CSS -->
    <link rel="stylesheet" href="css/app.css" />
    <link rel="stylesheet" href="css/email-validator.css" />

    <!-- Blazor -->
    <link rel="stylesheet" href="EmailFixer.Client.styles.css" />

    <!-- Font Awesome for icons -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
</head>

<body>
    <div id="app">
        <div class="loading-spinner">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-3">Loading Email Fixer...</p>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <!-- Bootstrap JS -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>

    <!-- Paddle.js for payments -->
    <script src="https://cdn.paddle.com/paddle/v2/paddle.js"></script>

    <!-- Blazor -->
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

**Package installations:**
```powershell
# Add required NuGet packages
cd EmailFixer.Client
dotnet add package Blazored.LocalStorage
dotnet add package Microsoft.AspNetCore.Components.WebAssembly
dotnet add package System.Net.Http.Json
```

### Task 3.2: Email Validation Service
**Duration:** 40 minutes
**LLM Readiness:** 100%
**Dependencies:** Task 3.1

**File:** `EmailFixer.Client/Services/EmailValidationService.cs`

```csharp
using System.Net.Http.Json;
using EmailFixer.Client.Models;

namespace EmailFixer.Client.Services;

public interface IEmailValidationService
{
    Task<EmailValidationResult> ValidateSingleAsync(string email, Guid userId);
    Task<BatchValidationResult> ValidateBatchAsync(List<string> emails, Guid userId);
}

public class EmailValidationService : IEmailValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailValidationService> _logger;

    public EmailValidationService(HttpClient httpClient, ILogger<EmailValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmailValidationResult> ValidateSingleAsync(string email, Guid userId)
    {
        try
        {
            var request = new EmailValidationRequest
            {
                UserId = userId,
                Email = email
            };

            var response = await _httpClient.PostAsJsonAsync("api/email/validate", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmailValidationResult>()
                    ?? new EmailValidationResult { Success = false, Error = "Invalid response" };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
            {
                return new EmailValidationResult
                {
                    Success = false,
                    Error = "Insufficient credits. Please purchase more credits to continue."
                };
            }

            return new EmailValidationResult
            {
                Success = false,
                Error = $"Validation failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email");
            return new EmailValidationResult
            {
                Success = false,
                Error = "Network error. Please check your connection."
            };
        }
    }

    public async Task<BatchValidationResult> ValidateBatchAsync(List<string> emails, Guid userId)
    {
        try
        {
            var request = new BatchEmailValidationRequest
            {
                UserId = userId,
                Emails = emails
            };

            var response = await _httpClient.PostAsJsonAsync("api/email/validate-batch", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BatchValidationResult>();
                return result ?? new BatchValidationResult { Success = false };
            }

            return new BatchValidationResult
            {
                Success = false,
                Error = response.StatusCode == System.Net.HttpStatusCode.PaymentRequired
                    ? "Insufficient credits for batch validation"
                    : "Batch validation failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch validation");
            return new BatchValidationResult { Success = false, Error = ex.Message };
        }
    }
}
```

### Task 3.3: Main Email Validator Page
**Duration:** 60 minutes
**LLM Readiness:** 95%
**Dependencies:** Task 3.2

**File:** `EmailFixer.Client/Pages/Index.razor`

```razor
@page "/"
@inject IEmailValidationService ValidationService
@inject IUserService UserService
@inject INotificationService NotificationService
@implements IDisposable

<PageTitle>Email Fixer - Validate Email Addresses</PageTitle>

<div class="container-fluid mt-4">
    <div class="row">
        <div class="col-lg-5">
            <div class="card shadow">
                <div class="card-header bg-primary text-white">
                    <h4><i class="fas fa-envelope-circle-check"></i> Email Validator</h4>
                </div>
                <div class="card-body">
                    <div class="mb-3">
                        <label class="form-label">Enter email addresses (one per line):</label>
                        <textarea @bind="EmailInput" @oninput="OnEmailInputChanged"
                                  class="form-control font-monospace"
                                  rows="12"
                                  placeholder="john@example.com&#10;jane@company.org&#10;info@website.net"
                                  disabled="@IsValidating">
                        </textarea>
                    </div>

                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <div>
                            <span class="badge bg-info fs-6">
                                <i class="fas fa-list"></i> @EmailCount emails
                            </span>
                            @if (CurrentUser != null)
                            {
                                <span class="badge bg-success fs-6 ms-2">
                                    <i class="fas fa-coins"></i> @CurrentUser.Credits credits
                                </span>
                            }
                        </div>
                        <div>
                            <button class="btn btn-secondary me-2" @onclick="ClearAll" disabled="@IsValidating">
                                <i class="fas fa-eraser"></i> Clear
                            </button>
                            <button class="btn btn-primary" @onclick="ValidateEmails"
                                    disabled="@(IsValidating || EmailCount == 0 || !HasSufficientCredits)">
                                @if (IsValidating)
                                {
                                    <span class="spinner-border spinner-border-sm me-2"></span>
                                    <span>Validating...</span>
                                }
                                else
                                {
                                    <i class="fas fa-check-circle"></i>
                                    <span>Validate</span>
                                }
                            </button>
                        </div>
                    </div>

                    @if (!HasSufficientCredits && EmailCount > 0)
                    {
                        <div class="alert alert-warning">
                            <i class="fas fa-exclamation-triangle"></i>
                            You need @EmailCount credits but only have @(CurrentUser?.Credits ?? 0).
                            <a href="/purchase" class="alert-link">Purchase more credits</a>
                        </div>
                    }
                </div>
            </div>
        </div>

        <div class="col-lg-7">
            @if (ValidationResults.Any())
            {
                <div class="card shadow">
                    <div class="card-header bg-secondary text-white">
                        <h4><i class="fas fa-clipboard-check"></i> Validation Results</h4>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <button class="btn btn-sm btn-outline-success me-2" @onclick="ExportValid">
                                <i class="fas fa-download"></i> Export Valid
                            </button>
                            <button class="btn btn-sm btn-outline-danger me-2" @onclick="ExportInvalid">
                                <i class="fas fa-download"></i> Export Invalid
                            </button>
                            <button class="btn btn-sm btn-outline-primary" @onclick="ExportAll">
                                <i class="fas fa-file-csv"></i> Export All
                            </button>
                        </div>

                        <div class="table-responsive">
                            <table class="table table-hover">
                                <thead>
                                    <tr>
                                        <th>Email</th>
                                        <th>Status</th>
                                        <th>Suggestion</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    @foreach (var result in ValidationResults)
                                    {
                                        <tr class="@GetRowClass(result.Status)">
                                            <td class="font-monospace">@result.Email</td>
                                            <td>
                                                <span class="badge @GetStatusBadgeClass(result.Status)">
                                                    @result.Status
                                                </span>
                                            </td>
                                            <td>
                                                @if (!string.IsNullOrEmpty(result.Suggestion))
                                                {
                                                    <button class="btn btn-sm btn-link"
                                                            @onclick="() => ShowSuggestion(result)">
                                                        <i class="fas fa-lightbulb"></i> @result.Suggestion
                                                    </button>
                                                }
                                            </td>
                                            <td>
                                                <button class="btn btn-sm btn-outline-secondary"
                                                        @onclick="() => CopyEmail(result.Email)">
                                                    <i class="fas fa-copy"></i>
                                                </button>
                                            </td>
                                        </tr>
                                    }
                                </tbody>
                            </table>
                        </div>

                        <div class="mt-3">
                            <div class="row text-center">
                                <div class="col">
                                    <h5 class="text-success">@ValidCount Valid</h5>
                                </div>
                                <div class="col">
                                    <h5 class="text-danger">@InvalidCount Invalid</h5>
                                </div>
                                <div class="col">
                                    <h5 class="text-warning">@SuspiciousCount Suspicious</h5>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            }
        </div>
    </div>
</div>

@if (ShowSuggestionModal && SelectedResult != null)
{
    <SuggestionModal Result="@SelectedResult"
                     OnAccept="AcceptSuggestion"
                     OnReject="RejectSuggestion" />
}

@code {
    private string EmailInput = string.Empty;
    private List<EmailValidationResultDto> ValidationResults = new();
    private bool IsValidating = false;
    private User? CurrentUser = null;
    private EmailValidationResultDto? SelectedResult = null;
    private bool ShowSuggestionModal = false;

    private int EmailCount => EmailInput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
    private bool HasSufficientCredits => CurrentUser?.Credits >= EmailCount;

    private int ValidCount => ValidationResults.Count(r => r.Status == "Valid");
    private int InvalidCount => ValidationResults.Count(r => r.Status == "Invalid");
    private int SuspiciousCount => ValidationResults.Count(r => r.Status == "Suspicious");

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await UserService.GetCurrentUserAsync();
        if (CurrentUser == null)
        {
            // Create guest user
            CurrentUser = await UserService.CreateGuestUserAsync();
        }
    }

    private async Task ValidateEmails()
    {
        if (CurrentUser == null) return;

        IsValidating = true;
        ValidationResults.Clear();

        try
        {
            var emails = EmailInput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct()
                .ToList();

            if (emails.Count == 1)
            {
                var result = await ValidationService.ValidateSingleAsync(emails[0], CurrentUser.Id);
                if (result.Success)
                {
                    ValidationResults.Add(result);
                    CurrentUser.Credits = result.RemainingCredits;
                }
                else
                {
                    await NotificationService.ShowError(result.Error ?? "Validation failed");
                }
            }
            else
            {
                var result = await ValidationService.ValidateBatchAsync(emails, CurrentUser.Id);
                if (result.Success)
                {
                    ValidationResults.AddRange(result.Results);
                    CurrentUser.Credits = result.RemainingCredits;
                }
                else
                {
                    await NotificationService.ShowError(result.Error ?? "Batch validation failed");
                }
            }

            if (ValidationResults.Any())
            {
                await NotificationService.ShowSuccess($"Validated {ValidationResults.Count} email(s)");
            }
        }
        finally
        {
            IsValidating = false;
        }
    }

    private void ClearAll()
    {
        EmailInput = string.Empty;
        ValidationResults.Clear();
    }

    private string GetRowClass(string status) => status switch
    {
        "Valid" => "table-success",
        "Invalid" => "table-danger",
        "Suspicious" => "table-warning",
        _ => ""
    };

    private string GetStatusBadgeClass(string status) => status switch
    {
        "Valid" => "bg-success",
        "Invalid" => "bg-danger",
        "Suspicious" => "bg-warning",
        _ => "bg-secondary"
    };

    // Additional methods for export, copy, suggestions...
}
```

### Task 3.4: Suggestion Modal Component
**Duration:** 35 minutes
**LLM Readiness:** 100%

**File:** `EmailFixer.Client/Components/SuggestionModal.razor`

```razor
@if (IsVisible)
{
    <div class="modal fade show d-block" tabindex="-1">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header bg-warning text-dark">
                    <h5 class="modal-title">
                        <i class="fas fa-lightbulb"></i> Email Suggestion
                    </h5>
                    <button type="button" class="btn-close" @onclick="OnReject"></button>
                </div>
                <div class="modal-body">
                    <p>We found a potential typo in the email address:</p>

                    <div class="mb-3">
                        <label class="form-label text-muted">Original:</label>
                        <div class="alert alert-light font-monospace">
                            @Result.Email
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label text-success">Suggested:</label>
                        <div class="alert alert-success font-monospace">
                            @Result.Suggestion
                        </div>
                    </div>

                    <p class="text-muted small">
                        Did you mean to type this email address instead?
                    </p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" @onclick="OnReject">
                        <i class="fas fa-times"></i> Keep Original
                    </button>
                    <button type="button" class="btn btn-success" @onclick="AcceptSuggestion">
                        <i class="fas fa-check"></i> Accept Suggestion
                    </button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop fade show"></div>
}

@code {
    [Parameter] public EmailValidationResultDto Result { get; set; } = null!;
    [Parameter] public EventCallback<string> OnAccept { get; set; }
    [Parameter] public EventCallback OnReject { get; set; }

    private bool IsVisible = true;

    private async Task AcceptSuggestion()
    {
        IsVisible = false;
        await OnAccept.InvokeAsync(Result.Suggestion);
    }
}
```

### Task 3.5-3.9: Additional Components
**Duration:** Variable
**LLM Readiness:** 95%

Due to space constraints, here's the structure for remaining components:

- **Task 3.5: User Service** - User management and localStorage
- **Task 3.6: Payment Service** - Paddle integration
- **Task 3.7: History Component** - Validation history display
- **Task 3.8: Navigation/Layout** - Main layout and menu
- **Task 3.9: Error Handling** - Global error boundary

## 🎨 CSS Styling

**File:** `EmailFixer.Client/wwwroot/css/email-validator.css`

```css
.loading-spinner {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100vh;
}

.table-success {
    background-color: rgba(25, 135, 84, 0.1) !important;
}

.table-danger {
    background-color: rgba(220, 53, 69, 0.1) !important;
}

.table-warning {
    background-color: rgba(255, 193, 7, 0.1) !important;
}

.font-monospace {
    font-family: 'Courier New', monospace;
    font-size: 0.95rem;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .container-fluid {
        padding: 10px;
    }

    .card {
        margin-bottom: 20px;
    }
}
```

## ✅ Phase Completion Checklist

- [ ] Blazor project configured and running
- [ ] HttpClient configured for API calls
- [ ] Email validation service functional
- [ ] Main validation page complete
- [ ] Suggestion modal working
- [ ] User service with localStorage
- [ ] Payment integration with Paddle
- [ ] History component displaying data
- [ ] Navigation and layout polished
- [ ] Error handling implemented
- [ ] Responsive design tested
- [ ] All components styled with Bootstrap

## 🧪 Testing Commands

```powershell
# Run Blazor app
dotnet run --project EmailFixer.Client

# Test with hot reload
dotnet watch run --project EmailFixer.Client

# Build for production
dotnet publish EmailFixer.Client -c Release -o ./publish

# Test production build
dotnet serve -o -p 5000 ./publish/wwwroot
```

## 📊 Performance Metrics

- ✅ Initial load: < 3 seconds
- ✅ Validation response: < 500ms visual feedback
- ✅ Bundle size: < 2MB compressed
- ✅ Lighthouse score: > 90

## 🔗 Next Phase

After successful completion:
1. ✅ Mark Phase 3 complete in master plan
2. ➡️ Proceed to [Phase 4: Containerization](phase4-containerization-coordinator.md)
3. 📝 Document any UI/UX decisions

---

**Estimated Time:** 6 hours
**Actual Time:** _[To be filled by executor]_
**Executor Notes:** _[To be filled by executor]_