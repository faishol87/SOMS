using System.Net.Http.Json;
using FrontEnd.Models;

namespace FrontEnd.Services;

public class SalesOrderApiService
{
    private readonly HttpClient _http;
    public SalesOrderApiService(HttpClient http) => _http = http;

    public async Task<List<OrderListItemDto>> GetOrdersAsync(string? keyword = null, DateTime? orderDate = null, CancellationToken ct = default)
    {
        var url = "api/orders";
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            qs.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        if (orderDate.HasValue)
            qs.Add($"orderDate={orderDate.Value:yyyy-MM-dd}");
        if (qs.Count > 0)
            url += "?" + string.Join("&", qs);

        var orders = await _http.GetFromJsonAsync<List<OrderListItemDto>>(url, ct);
        return orders ?? new List<OrderListItemDto>();
    }

    public async Task<OrderDetailDto?> GetOrderAsync(int id, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<OrderDetailDto>($"api/orders/{id}", ct);

    public async Task<ApiResponse> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
        => await PostAsync<ApiResponse>("api/orders", request, ct);

    public async Task<ApiResponse> UpdateOrderAsync(int id, OrderRequest request, CancellationToken ct = default)
        => await PutAsync<ApiResponse>($"api/orders/{id}", request, ct);

    public async Task<ApiResponse> DeleteOrderAsync(int id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"api/orders/{id}", ct);
        return await ReadApiResponseAsync(response, ct, new ApiResponse());
    }

    public async Task<ValidateItemsResult> ValidateItemsAsync(List<OrderItemRequest> items, CancellationToken ct = default)
        => await PostAsync<ValidateItemsResult>("api/orders/validate", items, ct);

    public async Task<byte[]> ExportOrdersAsync(string? keyword = null, DateTime? orderDate = null, CancellationToken ct = default)
    {
        var url = "api/orders/export";
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            qs.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        if (orderDate.HasValue)
            qs.Add($"orderDate={orderDate.Value:yyyy-MM-dd}");
        if (qs.Count > 0)
            url += "?" + string.Join("&", qs);

        return await _http.GetByteArrayAsync(url, ct);
    }

    private async Task<T> PostAsync<T>(string url, object body, CancellationToken ct) where T : new()
    {
        using var response = await _http.PostAsJsonAsync(url, body, ct);
        return await ReadApiResponseAsync(response, ct, new T());
    }

    private async Task<T> PutAsync<T>(string url, object body, CancellationToken ct) where T : new()
    {
        using var response = await _http.PutAsJsonAsync(url, body, ct);
        return await ReadApiResponseAsync(response, ct, new T());
    }

    /// <summary>
    /// Baca body sebagai JSON terlebih dahulu — termasuk status error (400)
    /// yang berisi pesan validasi dari service. Hanya throw ketika body
    /// tidak JSON dan status-nya error (mis. 500, HTML page).
    /// </summary>
    private static async Task<T> ReadApiResponseAsync<T>(HttpResponseMessage response, CancellationToken ct, T fallback) where T : new()
    {
        try
        {
            var api = await response.Content.ReadFromJsonAsync<T>(ct);
            if (api is not null)
                return api;
        }
        catch
        {
            // body bukan JSON — jatuh ke bawah
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Service gagal dengan status {(int)response.StatusCode}");

        return fallback;
    }
}
