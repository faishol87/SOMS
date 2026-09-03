using Microsoft.AspNetCore.Mvc;
using SalesOrderService.DTOs;
using SalesOrderService.Services;

namespace SalesOrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public OrdersController(ISalesOrderService salesOrderService) => _salesOrderService = salesOrderService;

    /// <summary>Ambil daftar order, filter opsional keyword & orderDate.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? keyword, [FromQuery] DateTime? orderDate, CancellationToken ct)
    {
        var orders = await _salesOrderService.GetOrdersAsync(keyword, orderDate, ct);
        return Ok(orders);
    }

    /// <summary>Ambil detail satu order beserta item.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id, CancellationToken ct)
    {
        var order = await _salesOrderService.GetOrderAsync(id, ct);
        return order is null
            ? NotFound(new ApiResultDto { Success = false, Message = "Order tidak ditemukan" })
            : Ok(order);
    }

    /// <summary>Buat order baru.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request, CancellationToken ct)
    {
        var result = await _salesOrderService.CreateOrderAsync(request, ct);
        return result.Success
            ? StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                salesSoId = result.SalesSoId,
                message = result.Message
            })
            : BadRequest(new ApiResultDto { Success = false, Message = result.Message, Errors = result.Errors });
    }

    /// <summary>Update order beserta item (replace seluruh item).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderRequestDto request, CancellationToken ct)
    {
        var result = await _salesOrderService.UpdateOrderAsync(id, request, ct);
        return result.Success
            ? Ok(new { success = true, message = result.Message })
            : NotFound(new ApiResultDto
            {
                Success = false,
                Message = result.Message,
                Errors = result.Errors
            });
    }

    /// <summary>Hapus order beserta seluruh item.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct)
    {
        var result = await _salesOrderService.DeleteOrderAsync(id, ct);
        return result.Success
            ? Ok(new { success = true, message = result.Message })
            : NotFound(new ApiResultDto { Success = false, Message = result.Message });
    }

    /// <summary>Ekspor data yang tampil (sesuai filter) ke file .xlsx.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportOrders([FromQuery] string? keyword, [FromQuery] DateTime? orderDate, CancellationToken ct)
    {
        var fileBytes = await _salesOrderService.ExportToExcelAsync(keyword, orderDate, ct);
        var fileName = $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Validasi & hitung TOTAL baris item (per-request, dipakai front-end)
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateItems([FromBody] List<OrderItemRequestDto> items, CancellationToken ct)
    {
        var result = await _salesOrderService.ValidateItemsAsync(items, ct);
        return Ok(result);
    }
}
