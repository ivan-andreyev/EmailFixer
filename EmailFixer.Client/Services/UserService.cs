using System.Net.Http.Json;
using Blazored.LocalStorage;
using EmailFixer.Shared.Models;

namespace EmailFixer.Client.Services;

/// <summary>
/// Implementation of user management service with caching to prevent duplicate API calls
/// </summary>
public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<UserService> _logger;
    private readonly CacheService _cacheService;
    private const string USER_KEY = "emailfixer_current_user";
    private const string USER_CACHE_KEY_PREFIX = "user_";

    /// <summary>
    /// Initializes a new instance of the UserService class
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls</param>
    /// <param name="localStorage">Local storage service for caching user data</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <param name="cacheService">Cache service to prevent duplicate API calls</param>
    public UserService(HttpClient httpClient, ILocalStorageService localStorage, ILogger<UserService> logger, CacheService cacheService)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Gets the current user from local storage and refreshes their data from the API (with in-memory caching)
    /// Prevents rapid re-fetches within 30 seconds
    /// </summary>
    /// <returns>Current user model or null if no user is cached</returns>
    public async Task<UserModel?> GetCurrentUserAsync()
    {
        try
        {
            // Try to get from localStorage first
            var cachedUser = await _localStorage.GetItemAsync<UserModel>(USER_KEY);
            if (cachedUser != null)
            {
                // Check in-memory cache first (30 second expiration)
                string cacheKey = $"{USER_CACHE_KEY_PREFIX}{cachedUser.Id}";
                if (_cacheService.TryGetValue<UserModel>(cacheKey, out var inMemoryCachedUser))
                {
                    return inMemoryCachedUser;
                }

                // Refresh from API to get latest credits
                var refreshedUser = await GetUserByIdAsync(cachedUser.Id);
                if (refreshedUser != null)
                {
                    await SaveCurrentUserAsync(refreshedUser);
                    return refreshedUser;
                }
                return cachedUser;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    /// <summary>
    /// Creates a new guest user account
    /// </summary>
    /// <returns>Newly created guest user or null if creation failed</returns>
    public async Task<UserModel?> CreateGuestUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/user/create-guest", null);

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserModel>();
                if (user != null)
                {
                    await SaveCurrentUserAsync(user);
                    return user;
                }
            }

            _logger.LogError("Failed to create guest user with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest user");
            return null;
        }
    }

    /// <summary>
    /// Gets user information by user ID from the API (with caching)
    /// Caches results for 5 minutes to prevent rapid re-fetches
    /// </summary>
    /// <param name="userId">The user ID to retrieve</param>
    /// <returns>User model or null if user not found</returns>
    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            string cacheKey = $"{USER_CACHE_KEY_PREFIX}{userId}";

            // Check cache first
            if (_cacheService.TryGetValue<UserModel>(cacheKey, out var cachedUser))
            {
                _logger.LogInformation("User {UserId} retrieved from cache", userId);
                return cachedUser;
            }

            // Fetch from API if not cached
            var response = await _httpClient.GetAsync($"api/user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserModel>();
                if (user != null)
                {
                    // Cache for 5 minutes
                    _cacheService.Set(cacheKey, user, TimeSpan.FromMinutes(5));
                }
                return user;
            }

            _logger.LogError("Failed to get user {UserId} with status {StatusCode}", userId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Updates the credits available for the specified user
    /// </summary>
    /// <param name="userId">The user ID to update credits for</param>
    /// <param name="newCredits">The new credit amount</param>
    /// <returns>True if update succeeded, false otherwise</returns>
    public async Task<bool> UpdateUserCreditsAsync(Guid userId, int newCredits)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.CreditsAvailable = newCredits;
                await SaveCurrentUserAsync(user);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credits for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Saves the user to local storage
    /// </summary>
    /// <param name="user">The user model to save</param>
    public async Task SaveCurrentUserAsync(UserModel user)
    {
        try
        {
            await _localStorage.SetItemAsync(USER_KEY, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user to localStorage");
        }
    }
}
