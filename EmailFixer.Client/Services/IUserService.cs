using EmailFixer.Shared.Models;

namespace EmailFixer.Client.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the current user from local storage and refreshes their data from the API
    /// </summary>
    /// <returns>Current user model or null if no user is cached</returns>
    Task<UserModel?> GetCurrentUserAsync();

    /// <summary>
    /// Creates a new guest user account
    /// </summary>
    /// <returns>Newly created guest user or null if creation failed</returns>
    Task<UserModel?> CreateGuestUserAsync();

    /// <summary>
    /// Gets user information by user ID from the API
    /// </summary>
    /// <param name="userId">The user ID to retrieve</param>
    /// <returns>User model or null if user not found</returns>
    Task<UserModel?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Updates the credits available for the specified user
    /// </summary>
    /// <param name="userId">The user ID to update credits for</param>
    /// <param name="newCredits">The new credit amount</param>
    /// <returns>True if update succeeded, false otherwise</returns>
    Task<bool> UpdateUserCreditsAsync(Guid userId, int newCredits);

    /// <summary>
    /// Saves the user to local storage
    /// </summary>
    /// <param name="user">The user model to save</param>
    Task SaveCurrentUserAsync(UserModel user);
}
