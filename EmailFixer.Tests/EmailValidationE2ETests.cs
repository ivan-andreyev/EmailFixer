using FluentAssertions;
using Microsoft.Playwright;

namespace EmailFixer.Tests;

/// <summary>
/// E2E тесты для полного пути пользователя: браузер → фронт → API → БД
/// Использует Playwright для автоматизации браузера
/// </summary>
public class EmailValidationE2ETests : IAsyncLifetime
{
    private IBrowser? _browser;
    private IPage? _page;
    private const string BaseUrl = "https://storage.googleapis.com/emailfixer-client";

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task UserCanValidateSingleEmail()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/index.html");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Find input field and enter email
        var emailInput = await _page.QuerySelectorAsync("textarea");
        await emailInput!.FillAsync("test@gmail.com");

        // Find and click validate button
        var validateButton = await _page.QuerySelectorAsync("button:has-text('Проверить')");
        await validateButton!.ClickAsync();

        // Wait for results
        await _page.WaitForSelectorAsync("table", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var resultTable = await _page.QuerySelectorAsync("table");
        resultTable.Should().NotBeNull();

        // Verify result shows valid status
        var statusCell = await _page.QuerySelectorAsync("td:has-text('Valid')");
        statusCell.Should().NotBeNull();
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task UserCanValidateMultipleEmails()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/index.html");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Enter multiple emails
        var emailInput = await _page.QuerySelectorAsync("textarea");
        var emails = "user@example.com\ninvalid-email\ntest@gmail.com";
        await emailInput!.FillAsync(emails);

        // Click validate button
        var validateButton = await _page.QuerySelectorAsync("button:has-text('Проверить')");
        await validateButton!.ClickAsync();

        // Wait for results
        await _page.WaitForSelectorAsync("table", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var rows = await _page.QuerySelectorAllAsync("tbody tr");
        rows.Should().HaveCount(3);
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task UserCanViewValidationHistory()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/index.html");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Click History link
        var historyLink = await _page.QuerySelectorAsync("a:has-text('История')");
        await historyLink!.ClickAsync();

        // Wait for history page to load
        await _page.WaitForSelectorAsync("table", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Assert
        var historyTable = await _page.QuerySelectorAsync("table");
        historyTable.Should().NotBeNull();
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task UserCanNavigateToPaymentPage()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/index.html");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Click Purchase link
        var purchaseLink = await _page.QuerySelectorAsync("a:has-text('Купить')");
        await purchaseLink!.ClickAsync();

        // Wait for purchase page to load
        await _page.WaitForSelectorAsync("button:has-text('Купить')", new PageWaitForSelectorOptions { Timeout = 5000 });

        // Assert
        var purchaseButton = await _page.QuerySelectorAsync("button:has-text('Купить')");
        purchaseButton.Should().NotBeNull();
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task ApiHealthCheckEndpoint_ReturnsHealthy()
    {
        // Arrange
        const string apiUrl = "https://emailfixer-api-tqq4othz7a-uc.a.run.app";

        // Act
        var response = await _page!.Context.APIRequest.GetAsync($"{apiUrl}/health");

        // Assert
        response.Status.Should().Be(200);
    }

    [Fact(Skip = "E2E test requires deployed application")]
    public async Task ApiValidateEndpoint_ValidEmail_ReturnsOk()
    {
        // Arrange
        const string apiUrl = "https://emailfixer-api-tqq4othz7a-uc.a.run.app";

        // Act
        var response = await _page!.Context.APIRequest.PostAsync(
            $"{apiUrl}/api/email/validate",
            new APIRequestContextOptions
            {
                DataObject = new { email = "test@gmail.com" }
            });

        // Assert
        response.Status.Should().Be(200);
        var json = await response.JsonAsync();
        json.Should().NotBeNull();
    }
}
