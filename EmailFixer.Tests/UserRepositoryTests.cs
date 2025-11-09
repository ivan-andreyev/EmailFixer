using EmailFixer.Infrastructure.Data;
using EmailFixer.Infrastructure.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

namespace EmailFixer.Tests;

/// <summary>
/// Integration tests для User repository
/// </summary>
public class UserRepositoryTests : IAsyncLifetime
{
    private EmailFixerDbContext? _context;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<EmailFixerDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmailFixerDb_{Guid.NewGuid()}")
            .Options;

        _context = new EmailFixerDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateUser_ValidUser_SavesSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreditsAvailable = 10,
            CreditsUsed = 0
        };

        // Act
        _context!.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
        savedUser.CreditsAvailable.Should().Be(10);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            CreditsAvailable = 10,
            CreditsUsed = 0
        };
        _context!.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var retrievedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateUserCredits_ValidUpdate_SavesSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreditsAvailable = 10,
            CreditsUsed = 0
        };
        _context!.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        user.CreditsAvailable -= 5;
        user.CreditsUsed += 5;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.CreditsAvailable.Should().Be(5);
        updatedUser.CreditsUsed.Should().Be(5);
    }
}
