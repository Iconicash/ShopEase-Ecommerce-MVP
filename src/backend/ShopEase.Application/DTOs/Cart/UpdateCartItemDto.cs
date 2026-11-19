using System.ComponentModel.DataAnnotations;

namespace ShopEase.Application.DTOs.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100)]
    public int Quantity { get; set; }
}
