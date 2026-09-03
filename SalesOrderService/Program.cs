using Microsoft.AspNetCore.Mvc;
using SalesOrderService.DTOs;
using SalesOrderService.Repositories;
using Services = SalesOrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Unifikasi format error 400 agar sama dengan format validasi SP:
        // { success, message, errors[] }
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Nilai request tidak valid" : e.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(new ApiResultDto
            {
                Success = false,
                Message = string.Join(" | ", errors),
                Errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<Services.ISalesOrderService, Services.SalesOrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
