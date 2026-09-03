using CustomerService.DTOs;
using CustomerService.Models;
using CustomerService.Repositories;

namespace CustomerService.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CustomerRequestDto request, CancellationToken ct = default);
    Task<CustomerDto?> UpdateAsync(int customerId, CustomerRequestDto request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int customerId, CancellationToken ct = default);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public async Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _repository.GetAllAsync(ct);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(customerId, ct);
        return customer is null ? null : ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ArgumentException("Nama pelanggan tidak boleh kosong");

        var customer = await _repository.InsertAsync(
            new Customer { CustomerName = request.CustomerName.Trim() }, ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int customerId, CustomerRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ArgumentException("Nama pelanggan tidak boleh kosong");

        var updated = await _repository.UpdateAsync(
            new Customer { CustomerId = customerId, CustomerName = request.CustomerName.Trim() }, ct);

        return updated is null ? null : ToDto(updated);
    }

    public Task<bool> DeleteAsync(int customerId, CancellationToken ct = default)
        => _repository.DeleteAsync(customerId, ct);

    private static CustomerDto ToDto(Customer customer) => new()
    {
        CustomerId = customer.CustomerId,
        CustomerName = customer.CustomerName
    };
}
