using FrontEnd.Components;
using FrontEnd.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var customerServiceUrl = builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001";
var salesOrderServiceUrl = builder.Configuration["ServiceUrls:SalesOrderService"] ?? "http://localhost:5002";

builder.Services.AddHttpClient<CustomerApiService>(client => client.BaseAddress = new Uri(customerServiceUrl));
builder.Services.AddHttpClient<SalesOrderApiService>(client => client.BaseAddress = new Uri(salesOrderServiceUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
