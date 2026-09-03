using ClosedXML.Excel;
using SalesOrderService.DTOs;
using SalesOrderService.Repositories;

namespace SalesOrderService.Services;

public interface ISalesOrderService
{
    Task<List<OrderListItemDto>> GetOrdersAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default);
    Task<OrderDetailResponseDto?> GetOrderAsync(int salesSoId, CancellationToken ct = default);
    Task<ValidateItemsResultDto> ValidateItemsAsync(List<OrderItemRequestDto> items, CancellationToken ct = default);
    Task<CreateOrderResponseDto> CreateOrderAsync(OrderRequestDto request, CancellationToken ct = default);
    Task<ApiResultDto> UpdateOrderAsync(int salesSoId, OrderRequestDto request, CancellationToken ct = default);
    Task<ApiResultDto> DeleteOrderAsync(int salesSoId, CancellationToken ct = default);
    Task<byte[]> ExportToExcelAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default);
}

public class SalesOrderService : ISalesOrderService
{
    private readonly ISalesOrderRepository _repository;

    public SalesOrderService(ISalesOrderRepository repository) => _repository = repository;

    public async Task<List<OrderListItemDto>> GetOrdersAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default)
    {
        var orders = await _repository.GetOrdersAsync(keyword, orderDate, ct);
        return orders.Select(o => (OrderListItemDto)o).ToList();
    }

    public async Task<OrderDetailResponseDto?> GetOrderAsync(int salesSoId, CancellationToken ct = default)
    {
        var order = await _repository.GetOrderByIdAsync(salesSoId, ct);
        return order is null ? null : new OrderDetailResponseDto
        {
            SalesSoId = order.SalesSoId,
            SoNo = order.SoNo,
            OrderDate = order.OrderDate,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Address = order.Address,
            GrandTotal = order.GrandTotal,
            Items = order.Items
        };
    }

    public Task<ValidateItemsResultDto> ValidateItemsAsync(List<OrderItemRequestDto> items, CancellationToken ct = default)
        => _repository.ValidateItemsAsync(items, ct);

    public async Task<CreateOrderResponseDto> CreateOrderAsync(OrderRequestDto request, CancellationToken ct = default)
    {
        var result = await _repository.CreateOrderAsync(request, ct);
        return new CreateOrderResponseDto
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors,
            SalesSoId = result.SalesSoId
        };
    }

    public Task<ApiResultDto> UpdateOrderAsync(int salesSoId, OrderRequestDto request, CancellationToken ct = default)
        => _repository.UpdateOrderAsync(salesSoId, request, ct);

    public Task<ApiResultDto> DeleteOrderAsync(int salesSoId, CancellationToken ct = default)
        => _repository.DeleteOrderAsync(salesSoId, ct);

    public async Task<byte[]> ExportToExcelAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default)
    {
        var orders = await _repository.GetOrdersAsync(keyword, orderDate, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sales Orders");

        sheet.Cell(1, 1).Value = "SO Number";
        sheet.Cell(1, 2).Value = "Order Date";
        sheet.Cell(1, 3).Value = "Customer Name";
        sheet.Cell(1, 4).Value = "Address";

        var header = sheet.Range(1, 1, 1, 4);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromArgb(29, 53, 87);
        header.Style.Font.FontColor = XLColor.White;

        var row = 2;
        foreach (var order in orders)
        {
            sheet.Cell(row, 1).Value = order.SoNo;
            sheet.Cell(row, 2).Value = order.OrderDate;
            sheet.Cell(row, 3).Value = order.CustomerName;
            sheet.Cell(row, 4).Value = order.Address ?? "";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

public class CreateOrderResponseDto : ApiResultDto
{
    public int SalesSoId { get; set; }
}

public class OrderDetailResponseDto
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
