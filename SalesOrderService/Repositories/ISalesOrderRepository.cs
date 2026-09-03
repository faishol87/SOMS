using SalesOrderService.DTOs;

namespace SalesOrderService.Repositories;

public interface ISalesOrderRepository
{
    Task<List<OrderDetailsDto>> GetOrdersAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default);
    Task<OrderDetailsDto?> GetOrderByIdAsync(int salesSoId, CancellationToken ct = default);
    Task<ValidateItemsResultDto> ValidateItemsAsync(List<OrderItemRequestDto> items, CancellationToken ct = default);
    Task<CreateOrderResultDto> CreateOrderAsync(OrderRequestDto request, CancellationToken ct = default);
    Task<ApiResultDto> UpdateOrderAsync(int salesSoId, OrderRequestDto request, CancellationToken ct = default);
    Task<ApiResultDto> DeleteOrderAsync(int salesSoId, CancellationToken ct = default);
}
