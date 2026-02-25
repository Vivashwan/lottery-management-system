using Xunit;
using Moq;
using FluentAssertions;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Models;
using ex1.Dto;

namespace ex1.Tests.ServicesTests;

public class DonorServiceTests
{
    private readonly Mock<IDonorRepository> _repo = new();
    private readonly DonorService _service;

    public DonorServiceTests()
    {
        _service = new DonorService(_repo.Object);
    }

    [Fact]
    public async Task SearchDonorByNameAsync_WhenEmpty_ReturnsEmpty_AndDoesNotCallRepo()
    {
        var result = await _service.SearchDonorByNameAsync("   ");

        result.Should().BeEmpty();
        _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDonorByIdAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Donor?)null);

        var dto = await _service.GetDonorByIdAsync(1);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task CreateDonorAsync_CreatesDonor()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<Donor>()))
            .ReturnsAsync((Donor d) => { d.Id = 7; return d; });

        var dto = await _service.CreateDonorAsync(new DonorCreateDto
        {
            FirstName = "Dana",
            LastName = "Levi",
            Email = "d@x.com",
            Phone = "050"
        });

        dto.Id.Should().Be(7);
        dto.FirstName.Should().Be("Dana");
        dto.LastName.Should().Be("Levi");

        _repo.Verify(r => r.CreateAsync(It.Is<Donor>(d =>
            d.FirstName == "Dana" &&
            d.LastName == "Levi" &&
            d.Email == "d@x.com")), Times.Once);
    }

    [Fact]
    public async Task DeleteDonorAsync_ReturnsRepoResult()
    {
        _repo.Setup(r => r.DeleteAsync(3)).ReturnsAsync(true);

        var ok = await _service.DeleteDonorAsync(3);

        ok.Should().BeTrue();
    }
}
