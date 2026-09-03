using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SalesOrderService.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();

        string? key = context.HttpContext.Request.Headers["X-Api-Key"];
        string? expected = config["ApiKey:Value"];

        if (string.IsNullOrEmpty(expected) || key != expected)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                Message = "API key tidak valid atau tidak disediakan"
            });
        }
    }
}