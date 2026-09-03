namespace FrontEnd.Models;

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

public class OrderItemDto
{
    public int SalesSoLitemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

public class OrderDetailDto
{
    public int SalesSoId { get; set; }
    public string SoNo { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? Address { get; set; }
    public decimal GrandTotal { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderRequest
{
    public string? SoNo { get; set; }
    public DateTime? OrderDate { get; set; }
    public int? CustomerId { get; set; }
    public string? Address { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public int SalesSoId { get; set; }
}

public class ItemValidationResult
{
    public int Index { get; set; }
    public decimal Total { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ValidateItemsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public decimal GrandTotal { get; set; }
    public List<ItemValidationResult> Items { get; set; } = new();
}
