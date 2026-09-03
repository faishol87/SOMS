using System.Data;
using CustomerService.Models;
using Microsoft.Data.SqlClient;

namespace CustomerService.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' tidak ditemukan.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    private static Customer Map(SqlDataReader reader) => new()
    {
        CustomerId = reader.GetInt32(reader.GetOrdinal("COM_CUSTOMER_ID")),
        CustomerName = reader.GetString(reader.GetOrdinal("CUSTOMER_NAME"))
    };

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COM_CUSTOMER_ID, CUSTOMER_NAME
            FROM COM_CUSTOMER
            WHERE COM_CUSTOMER_ID = @CustomerId
            """;
        command.Parameters.AddWithValue("@CustomerId", customerId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return Map(reader);
    }

    public async Task<List<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = new List<Customer>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COM_CUSTOMER_ID, CUSTOMER_NAME
            FROM COM_CUSTOMER
            ORDER BY CUSTOMER_NAME
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            customers.Add(Map(reader));

        return customers;
    }

    public async Task<Customer> InsertAsync(Customer customer, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @NewId INT = NEXT VALUE FOR dbo.SEQ_COM_CUSTOMER;

            INSERT INTO COM_CUSTOMER (COM_CUSTOMER_ID, CUSTOMER_NAME)
            VALUES (@NewId, @CustomerName);

            SELECT @NewId;
            """;
        command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);

        var newId = (int)(await command.ExecuteScalarAsync(ct) ?? 0);
        customer.CustomerId = newId;
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE COM_CUSTOMER
            SET CUSTOMER_NAME = @CustomerName
            WHERE COM_CUSTOMER_ID = @CustomerId
            """;
        command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
        command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);

        var affected = await command.ExecuteNonQueryAsync(ct);
        return affected > 0 ? await GetByIdAsync(customer.CustomerId, ct) : null;
    }

    public async Task<bool> DeleteAsync(int customerId, CancellationToken ct = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM COM_CUSTOMER
            WHERE COM_CUSTOMER_ID = @CustomerId
            """;
        command.Parameters.AddWithValue("@CustomerId", customerId);

        var affected = await command.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }
}
