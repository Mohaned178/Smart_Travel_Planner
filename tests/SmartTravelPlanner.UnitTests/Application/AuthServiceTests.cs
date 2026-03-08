using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartTravelPlanner.Application.Interfaces;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.UnitTests.Application;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _tokenServiceMock = new Mock<IJwtTokenService>();
        _service = new AuthService(_userManagerMock.Object, _tokenServiceMock.Object, new NullLogger<AuthService>());
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsError()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(new User());

        var result = await _service.RegisterAsync("test@test.com", "Password123!", "Test User");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Email already registered");
    }

    [Fact]
    public async Task RegisterAsync_ValidNewUser_Succeeds()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((User?)null);
            
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.RegisterAsync("new@test.com", "Password123!", "New User");

        result.Success.Should().BeTrue();
        result.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsError()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync("test@test.com", "WrongPassword");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", DisplayName = "Test" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Password123!"))
            .ReturnsAsync(true);
        _tokenServiceMock.Setup(x => x.GenerateToken(user.Id, user.Email!, user.DisplayName))
            .Returns(("token-string", DateTime.UtcNow.AddHours(1)));

        var result = await _service.LoginAsync("test@test.com", "Password123!");

        result.Success.Should().BeTrue();
        result.Token.Should().Be("token-string");
    }
}
