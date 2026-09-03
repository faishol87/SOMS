using CustomerService.DTOs;
using CustomerService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    /// <summary>Ambil semua data pelanggan.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers(CancellationToken ct)
    {
        var customers = await _customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    /// <summary>Ambil satu data pelanggan.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomer(int id, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(id, ct);
        return customer is null
            ? NotFound(new ApiErrorResponse { Message = "Customer tidak ditemukan" })
            : Ok(customer);
    }

    /// <summary>Buat pelanggan baru.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerRequestDto request, CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, customer);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>Update data pelanggan.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerRequestDto request, CancellationToken ct)
    {
        try
        {
            var customer = await _customerService.UpdateAsync(id, request, ct);
            return customer is null
                ? NotFound(new ApiErrorResponse { Message = "Customer tidak ditemukan" })
                : Ok(customer);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>Hapus data pelanggan.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
    {
        var deleted = await _customerService.DeleteAsync(id, ct);
        return deleted
            ? Ok(new ApiErrorResponse { Success = true, Message = "Customer berhasil dihapus" })
            : NotFound(new ApiErrorResponse { Message = "Customer tidak ditemukan" });
    }
}
