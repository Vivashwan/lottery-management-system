using Xunit;
using Moq;
using FluentAssertions;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Models;
using ex1.Dto;

namespace ex1.Tests.ServicesTests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_repo.Object);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsDtos()
    {
        _repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category>
            {
                new() { Id = 1, Name = "A" },
                new() { Id = 2, Name = "B" }
            });

        var result = (await _service.GetAllCategoriesAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenExists_ReturnsDto()
    {
        _repo.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Category { Id = 5, Name = "Toys" });

        var dto = await _service.GetCategoryByIdAsync(5);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(5);
        dto.Name.Should().Be("Toys");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Category?)null);

        var dto = await _service.GetCategoryByIdAsync(5);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task AddCategoryAsync_CallsAddAsync_AndReturnsDto()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => { c.Id = 10; return c; });

        var dto = await _service.AddCategoryAsync(new CategoryCreateDto { Name = "NewCat" });

        dto.Id.Should().Be(10);
        dto.Name.Should().Be("NewCat");

        _repo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "NewCat")), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Category?)null);

        var dto = await _service.UpdateCategoryAsync(1, new CategoryUpdateDto { Name = "X" });

        dto.Should().BeNull();
    }
}
