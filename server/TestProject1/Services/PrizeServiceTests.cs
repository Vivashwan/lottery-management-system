using Xunit;
using Moq;
using FluentAssertions;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Models;
using ex1.Dto;

namespace ex1.Tests.ServicesTests;

public class PrizeServiceTests
{
    private readonly Mock<IPrizeRepository> _repo = new();
    private readonly PrizeService _service;

    public PrizeServiceTests()
    {
        _service = new PrizeService(_repo.Object);
    }

    [Fact]
    public async Task SearchPrizeByNameAsync_WhenEmpty_ReturnsEmpty_AndNoRepoCall()
    {
        var res = await _service.SearchPrizeByNameAsync("");

        res.Should().BeEmpty();
        _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task searchPrizeByCountAsync_WhenNegative_ReturnsEmpty()
    {
        var res = await _service.searchPrizeByCountAsync(-1);

        res.Should().BeEmpty();
        _repo.Verify(r => r.SearchByCountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPrizeByIdAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Prize?)null);

        var dto = await _service.GetPrizeByIdAsync(1);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task CreatePrizeAsync_CallsCreateAsync()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<Prize>()))
            .ReturnsAsync((Prize p) => { p.Id = 99; return p; });

        var dto = await _service.CreatePrizeAsync(new PrizeCreateDto
        {
            Name = "Prize",
            Description = "Desc",
            TicketPrice = 10,
            CategoryId = 1,
            DonorId = 2,
            ImagePath = "img.png",
            IsRaffleDone = false
        });

        dto.Id.Should().Be(99);
        dto.Name.Should().Be("Prize");

        _repo.Verify(r => r.CreateAsync(It.Is<Prize>(p => p.Name == "Prize" && p.TicketPrice == 10)), Times.Once);
    }

    [Fact]
    public async Task DeletePrizeAsync_ReturnsRepoResult()
    {
        _repo.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

        var ok = await _service.DeletePrizeAsync(5);

        ok.Should().BeTrue();
    }
}
