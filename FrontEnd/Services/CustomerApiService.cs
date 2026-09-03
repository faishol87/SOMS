using System.Net.Http.Json;
using FrontEnd.Models;

namespace FrontEnd.Services;

public class CustomerApiService
{
    private readonly HttpClient _http;
    public CustomerApiService(HttpClient http) => _http = http;

    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken ct = default)
    {
        var customers = await _http.GetFromJsonAsync<List<CustomerDto>>("api/customers", ct);
        return customers ?? new List<CustomerDto>();
    }
}
