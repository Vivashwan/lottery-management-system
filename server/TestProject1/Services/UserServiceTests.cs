using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Models;
using ex1.Dto;

namespace ex1.Tests.ServicesTests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<ITokenService> _token = new();
    private readonly IConfiguration _config;
    private readonly Mock<ILogger<UserService>> _logger = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        var dict = new Dictionary<string, string?>
        {
            ["JwtSettings:ExpiryMinutes"] = "60"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        _service = new UserService(_repo.Object, _token.Object, _config, _logger.Object);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailExists_Throws()
    {
        _repo.Setup(r => r.EmailExistsAsync("a@a.com")).ReturnsAsync(true);

        var act = async () => await _service.CreateUserAsync(new UserCreateDto
        {
            FirstName = "A",
            LastName = "B",
            Email = "a@a.com",
            Password = "123",
            Address = "X",
            Phone = "050"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByEmailAsync("x@x.com")).ReturnsAsync((User?)null);

        var res = await _service.AuthenticateAsync("x@x.com", "123");

        res.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordWrong_ReturnsNull()
    {
        _repo.Setup(r => r.GetByEmailAsync("x@x.com"))
            .ReturnsAsync(new User
            {
                Id = 1,
                Email = "x@x.com",
                FirstName = "X",
                LastName = "Y",
                Role = "User",
                PasswordHash = "NOT_THE_HASH"
            });

        var res = await _service.AuthenticateAsync("x@x.com", "123");

        res.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenOk_ReturnsTokenAndUser()
    {
        var password = "123";
        var user = new User
        {
            Id = 10,
            Email = "ok@ok.com",
            FirstName = "Ok",
            LastName = "User",
            Role = "User",
            PasswordHash = ""
        };

        _repo.Setup(r => r.GetByEmailAsync("ok@ok.com")).ReturnsAsync(user);
        _token.Setup(t => t.GenerateToken(user.Id, user.Email, user.FirstName, user.LastName, user.Role))
              .Returns("TOKEN");

        var res = await _service.AuthenticateAsync("ok@ok.com", password);
    }
}
