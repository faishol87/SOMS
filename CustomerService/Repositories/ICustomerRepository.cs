using CustomerService.Models;

namespace CustomerService.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct = default);
    Task<List<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer> InsertAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(int customerId, CancellationToken ct = default);
}
