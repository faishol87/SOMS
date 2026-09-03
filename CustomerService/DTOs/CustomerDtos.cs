namespace CustomerService.DTOs;

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
}

public class CustomerRequestDto
{
    public string CustomerName { get; set; } = "";
}

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}
