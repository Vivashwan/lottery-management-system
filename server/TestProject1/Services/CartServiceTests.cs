using Xunit;
using Moq;
using FluentAssertions;
using ex1.services;
using ex1.Interfaces.Repositorys;
using ex1.Models;

namespace ex1.Tests.ServicesTests;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _repo = new();
    private readonly CartService _service;

    public CartServiceTests()
    {
        _service = new CartService(_repo.Object);
    }

    [Fact]
    public async Task AddPrizeToCart_WhenItemMissing_AddsNewItem()
    {
        _repo.Setup(r => r.GetOrCreateDraftCartByUserId(1))
            .ReturnsAsync(new Cart { Id = 100 });

        _repo.Setup(r => r.GetItemInCart(100, 5))
            .ReturnsAsync((CartItem?)null);

        var ok = await _service.AddPrizeToCart(1, 5);

        ok.Should().BeTrue();
        _repo.Verify(r => r.AddItem(It.Is<CartItem>(ci => ci.CartId == 100 && ci.PrizeId == 5 && ci.Quantity == 1)), Times.Once);
        _repo.Verify(r => r.UpdateItem(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddPrizeToCart_WhenItemExists_IncrementsAndUpdates()
    {
        _repo.Setup(r => r.GetOrCreateDraftCartByUserId(1))
            .ReturnsAsync(new Cart { Id = 100 });

        var existing = new CartItem { Id = 7, CartId = 100, PrizeId = 5, Quantity = 2 };
        _repo.Setup(r => r.GetItemInCart(100, 5)).ReturnsAsync(existing);

        var ok = await _service.AddPrizeToCart(1, 5);

        ok.Should().BeTrue();
        existing.Quantity.Should().Be(3);
        _repo.Verify(r => r.UpdateItem(existing), Times.Once);
        _repo.Verify(r => r.AddItem(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WhenZeroOrLess_ReturnsFalse_AndNoRepoCall()
    {
        var ok = await _service.UpdateItemQuantityAsync(1, 0);

        ok.Should().BeFalse();
        _repo.Verify(r => r.UpdateItemQuantityAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetExpensiveSalesAsync_OrdersByTicketPriceDesc()
    {
        var carts = new List<Cart>
        {
            new Cart
            {
                Items = new List<CartItem>
                {
                    new CartItem { PrizeId = 1, Quantity = 1, Prize = new Prize { Name="P1", TicketPrice=10 } },
                    new CartItem { PrizeId = 2, Quantity = 1, Prize = new Prize { Name="P2", TicketPrice=50 } }
                }
            }
        };

        _repo.Setup(r => r.GetAllPaidCarts()).ReturnsAsync(carts);

        var res = (await _service.GetExpensiveSalesAsync()).ToList();

        res[0].TicketPrice.Should().Be(50);
        res[1].TicketPrice.Should().Be(10);
    }
}
