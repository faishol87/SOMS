using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SalesOrderService.DTOs;

namespace SalesOrderService.Repositories;

public class SalesOrderRepository : ISalesOrderRepository
{
    private static readonly JsonSerializerOptions JsonWebOptions = new(JsonSerializerDefaults.Web);

    private readonly string _connectionString;

    public SalesOrderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' tidak ditemukan.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    private static SqlCommand CreateProc(SqlConnection connection, string procName)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procName;
        return command;
    }

    public async Task<List<OrderDetailsDto>> GetOrdersAsync(string? keyword, DateTime? orderDate, CancellationToken ct = default)
    {
        var orders = new List<OrderDetailsDto>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_get_orders");
        command.Parameters.Add(new SqlParameter("@Keyword", SqlDbType.NVarChar, 100) { Value = (object?)keyword ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.DateTime) { Value = (object?)orderDate ?? DBNull.Value });

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            orders.Add(ReadOrder(reader));

        return orders;
    }

    public async Task<OrderDetailsDto?> GetOrderByIdAsync(int salesSoId, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_get_order_by_id");
        command.Parameters.AddWithValue("@SalesSoId", salesSoId);

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        var order = ReadOrder(reader);

        if (await reader.NextResultAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                order.Items.Add(new OrderItemDto
                {
                    SalesSoLitemId = reader.GetInt32(0),
                    ItemName = reader.GetString(2),
                    Quantity = reader.GetInt32(3),
                    Price = (decimal)reader.GetDouble(4),
                    Total = (decimal)reader.GetDouble(5)
                });
            }
        }

        return order;
    }

    public async Task<ValidateItemsResultDto> ValidateItemsAsync(List<OrderItemRequestDto> items, CancellationToken ct = default)
    {
        var itemsJson = JsonSerializer.Serialize(items, JsonWebOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_validate_items");
        command.Parameters.Add(new SqlParameter("@ItemsJson", SqlDbType.NVarChar, -1) { Value = itemsJson });

        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new ValidateItemsResultDto();
        if (!await reader.ReadAsync(ct))
            return result;

        result.Success = reader.IsDBNull(0) ? false : reader.GetInt32(0) == 1;
        result.Message = reader.GetString(1);
        result.GrandTotal = (decimal)reader.GetDouble(2);

        var itemsJsonRaw = reader.IsDBNull(3) ? null : reader.GetString(3);
        if (!string.IsNullOrWhiteSpace(itemsJsonRaw))
        {
            using var doc = JsonDocument.Parse(itemsJsonRaw);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var validation = new ItemValidationDto
                {
                    Index = item.TryGetProperty("index", out var idx) && idx.ValueKind == JsonValueKind.Number ? idx.GetInt32() : 0,
                    Total = item.TryGetProperty("total", out var total) && total.ValueKind == JsonValueKind.Number ? total.GetDecimal() : 0m
                };

                if (item.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.String)
                {
                    var raw = errors.GetString();
                    if (!string.IsNullOrEmpty(raw))
                        validation.Errors.AddRange(raw.Split(" | ", StringSplitOptions.RemoveEmptyEntries));
                }

                result.Items.Add(validation);
            }
        }

        return result;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(OrderRequestDto request, CancellationToken ct = default)
    {
        var itemsJson = JsonSerializer.Serialize(request.Items, JsonWebOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_create_order");
        command.Parameters.Add(new SqlParameter("@SoNo", SqlDbType.NVarChar, 20) { Value = (object?)request.SoNo ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.DateTime) { Value = (object?)request.OrderDate ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = (object?)request.CustomerId ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 500) { Value = (object?)request.Address ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ItemsJson", SqlDbType.NVarChar, -1) { Value = itemsJson });

        var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        var idParam = new SqlParameter("@SalesSoId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.AddRange(new[] { successParam, messageParam, idParam });

        await command.ExecuteNonQueryAsync(ct);

        var result = new CreateOrderResultDto
        {
            Success = (bool)(successParam.Value ?? false),
            Message = (string)(messageParam.Value ?? ""),
            SalesSoId = (int)(idParam.Value ?? 0)
        };

        if (!result.Success)
            result.Errors = SplitErrors(result.Message);

        return result;
    }

    public async Task<ApiResultDto> UpdateOrderAsync(int salesSoId, OrderRequestDto request, CancellationToken ct = default)
    {
        var itemsJson = JsonSerializer.Serialize(request.Items, JsonWebOptions);

        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_update_order");
        command.Parameters.Add(new SqlParameter("@SalesSoId", SqlDbType.Int) { Value = salesSoId });
        command.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.DateTime) { Value = (object?)request.OrderDate ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = (object?)request.CustomerId ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 500) { Value = (object?)request.Address ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@ItemsJson", SqlDbType.NVarChar, -1) { Value = itemsJson });

        var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        command.Parameters.AddRange(new[] { successParam, messageParam });

        await command.ExecuteNonQueryAsync(ct);

        var result = new ApiResultDto
        {
            Success = (bool)(successParam.Value ?? false),
            Message = (string)(messageParam.Value ?? "")
        };

        if (!result.Success)
            result.Errors = SplitErrors(result.Message);

        return result;
    }

    public async Task<ApiResultDto> DeleteOrderAsync(int salesSoId, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = CreateProc(connection, "dbo.sp_delete_order");
        command.Parameters.AddWithValue("@SalesSoId", salesSoId);

        var successParam = new SqlParameter("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
        command.Parameters.AddRange(new[] { successParam, messageParam });

        await command.ExecuteNonQueryAsync(ct);

        return new ApiResultDto
        {
            Success = (bool)(successParam.Value ?? false),
            Message = (string)(messageParam.Value ?? ""),
            Errors = (bool)(successParam.Value ?? false) ? new List<string>() : SplitErrors((string)(messageParam.Value ?? ""))
        };
    }

    /// <summary>
    /// Pesan error dari SP memakai pemisah " | ", ubah menjadi list agar
    /// sesuai format api response { success, message, errors }.
    /// </summary>
    private static List<string> SplitErrors(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new List<string>();

        return message.Split(" | ", StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static OrderDetailsDto ReadOrder(SqlDataReader reader)
    {
        var soId = reader.GetInt32(0);
        var customerId = reader.GetInt32(3);

        return new OrderDetailsDto
        {
            SalesSoId = soId,
            SoNo = reader.GetString(1),
            OrderDate = reader.GetDateTime(2),
            CustomerId = customerId,
            CustomerName = reader.GetString(4),
            Address = reader.IsDBNull(5) ? null : reader.GetString(5),
            GrandTotal = (decimal)reader.GetDouble(6)
        };
    }
}
