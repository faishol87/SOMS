using FrontEnd.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FrontEnd.Components.Pages;

public partial class OrderList
{
    private const int PageSize = 10;

    private string? _keyword;
    private DateTime? _orderDate;
    private List<OrderListItemDto> _orders = new();
    private int _currentPage;
    private bool _loading = true;
    private string? _errorMessage;
    private OrderListItemDto? _deleteTarget;
    private string? _deleteError;
    private bool _showSuccess;
    private string _successMessage = "";

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_orders.Count / (double)PageSize));

    private List<OrderListItemDto> PageItems => _orders
        .Skip(CurrentPage * PageSize)
        .Take(PageSize)
        .ToList();

    private string PageInfo
    {
        get
        {
            if (_orders.Count == 0) return "0 - 0 of 0 items";
            var start = CurrentPage * PageSize + 1;
            var end = Math.Min((CurrentPage + 1) * PageSize, _orders.Count);
            return $"{start} - {end} of {_orders.Count} items";
        }
    }

    private List<int> VisiblePages
    {
        get
        {
            var pages = new List<int>();
            for (var p = 0; p < TotalPages && p < 5; p++) pages.Add(p);
            return pages;
        }
    }

    private int CurrentPage
    {
        get => _currentPage;
        set => _currentPage = value;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _errorMessage = null;
        try
        {
            _orders = await SalesOrderApi.GetOrdersAsync(_keyword, _orderDate);
            _currentPage = 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Gagal memuat data: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SearchAsync() => await LoadAsync();

    private void OnSearchEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") _ = SearchAsync();
    }

    private void ChangePage(int page)
    {
        _currentPage = Math.Clamp(page, 0, TotalPages - 1);
    }

    private void EditOrder(int id) => Nav.NavigateTo($"/orders/edit/{id}");

    private void AddNewOrder() => Nav.NavigateTo("/orders/new");

    private void OpenDeleteConfirm(OrderListItemDto order)
    {
        _deleteTarget = order;
        _deleteError = null;
    }

    private void CloseDeleteConfirm() => _deleteTarget = null;

    private void DismissSuccess() => _showSuccess = false;

    private async Task ConfirmDeleteAsync()
    {
        if (_deleteTarget is null) return;
        _deleteError = null;
        try
        {
            var result = await SalesOrderApi.DeleteOrderAsync(_deleteTarget.SalesSoId);
            if (result.Success)
            {
                _showSuccess = true;
                _successMessage = result.Message;
                _deleteTarget = null;
                await LoadAsync();
            }
            else
            {
                _deleteError = result.Message;
            }
        }
        catch (Exception ex)
        {
            _deleteError = $"Gagal menghapus: {ex.Message}";
        }
    }

    private async Task ExportExcelAsync()
    {
        try
        {
            var bytes = await SalesOrderApi.ExportOrdersAsync(_keyword, _orderDate);
            var fileName = $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";
            var base64 = Convert.ToBase64String(bytes);
            await JS.InvokeVoidAsync("SomsDownload", fileName, base64);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Gagal mengekspor: {ex.Message}";
        }
    }
}