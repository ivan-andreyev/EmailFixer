using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace EmailFixer.Client.Services;

/// <summary>
/// HTTP message handler that adds JWT token to outgoing requests
/// and handles 401 Unauthorized responses
/// </summary>
public class AuthHttpClientHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<AuthHttpClientHandler> _logger;

    public AuthHttpClientHandler(
        IAuthService authService,
        NavigationManager navigationManager,
        ILogger<AuthHttpClientHandler> logger)
    {
        _authService = authService;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get JWT token from auth service
        var token = await _authService.GetTokenAsync();

        // Add Authorization header if token exists
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Send request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized. Clearing auth and redirecting to login.");

            // Clear authentication
            await _authService.LogoutAsync();

            // Redirect to login
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        return response;
    }
}
