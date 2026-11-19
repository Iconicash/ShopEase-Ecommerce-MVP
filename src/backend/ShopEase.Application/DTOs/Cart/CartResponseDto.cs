namespace ShopEase.Application.DTOs.Cart;

public class CartResponseDto
{
    public int Id { get; set; }
    public List<CartLineDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal Subtotal { get; set; }
}

public class CartLineDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal LineTotal { get; set; }
}
