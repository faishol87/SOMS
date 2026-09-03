using System.Text.Json;

namespace SalesOrderService.DTOs;

public class OrderItemDto
{
    public int SalesSoLitemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

public class OrderListItemDto
{
    public int SalesSoId { get; set; }
    public string SoNo { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? Address { get; set; }
    public decimal GrandTotal { get; set; }
}

public class OrderDetailsDto : OrderListItemDto
{
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemRequestDto
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderRequestDto
{
    public string? SoNo { get; set; }
    public DateTime? OrderDate { get; set; }
    public int? CustomerId { get; set; }
    public string? Address { get; set; }
    public List<OrderItemRequestDto> Items { get; set; } = new();
}

public class ValidateItemsResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public List<ItemValidationDto> Items { get; set; } = new();
}

public class ItemValidationDto
{
    public int Index { get; set; }
    public decimal Total { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ApiResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}

public class CreateOrderResultDto : ApiResultDto
{
    public int SalesSoId { get; set; }
}
