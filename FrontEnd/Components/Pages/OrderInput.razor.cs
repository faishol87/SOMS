using System.Globalization;
using FrontEnd.Models;

namespace FrontEnd.Components.Pages;

public partial class OrderInput
{
    private bool IsEdit => Id > 0;
    private readonly List<CustomerDto> _customers = new();
    private readonly List<ItemDto> _items = new();
    private List<string>? _formError;
    private bool _headerOpen = true;
    private bool _itemsOpen = true;
    private bool _loading = true;
    private bool _saving;
    private string? _soNo;
    private DateTime? _orderDate;
    private int? _customerId;
    private string? _address;
    private decimal _grandTotal;

    private static CultureInfo IdCulture { get; } = CultureInfo.GetCultureInfo("id-ID");
    private CultureInfo Invariant => CultureInfo.InvariantCulture;

    private int _totalItemsCount => _items.Count;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        try
        {
            var customers = await CustomerApi.GetCustomersAsync();
            _customers.AddRange(customers);

            if (IsEdit)
            {
                var order = await SalesOrderApi.GetOrderAsync(Id);
                if (order is null)
                {
                    _formError = new List<string> { "Order tidak ditemukan" };
                }
                else
                {
                    _soNo = order.SoNo;
                    _orderDate = order.OrderDate;
                    _customerId = order.CustomerId;
                    _address = order.Address;
                    _grandTotal = order.GrandTotal;
                    _items.AddRange(order.Items.Select(i => new ItemDto
                    {
                        LitemId = i.SalesSoLitemId,
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Total = i.Total
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            _formError = new List<string> { $"Gagal memuat data: {ex.Message}" };
        }
        finally
        {
            _loading = false;
        }
    }

    private void AddItem()
    {
        _items.Add(new ItemDto { InputMode = true });
    }

    private void EditItemRow(ItemDto item)
    {
        item.Backup = new ItemDto
        {
            LitemId = item.LitemId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            Price = item.Price,
            Total = item.Total
        };
        item.InputMode = true;
    }

    private void CancelItemRow(ItemDto item)
    {
        if (item.Backup is null)
        {
            _items.Remove(item);
        }
        else
        {
            item.ItemName = item.Backup.ItemName;
            item.Quantity = item.Backup.Quantity;
            item.Price = item.Backup.Price;
            item.Total = item.Backup.Total;
            item.Backup = null;
            item.Errors.Clear();
            item.InputMode = false;
        }
    }

    private void RemoveItem(ItemDto item)
    {
        _items.Remove(item);
        _ = RefreshTotalsAsync();
    }

    private async Task SaveItemRowAsync(ItemDto item)
    {
        item.Errors.Clear();
        var targets = _items.Where(i => !i.InputMode || i == item).ToList();
        if (targets.Count == 0) return;

        var requests = targets.Select(t => new OrderItemRequest
        {
            ItemName = t.ItemName,
            Quantity = t.Quantity,
            Price = t.Price
        }).ToList();

        try
        {
            var result = await SalesOrderApi.ValidateItemsAsync(requests);
            if (result.Success)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var row = result.Items.FirstOrDefault(r => r.Index == i);
                    if (row is null) continue;
                    targets[i].Total = row.Total;
                    targets[i].Errors = row.Errors;
                }

                item.InputMode = item.Errors.Count != 0;
                _grandTotal = result.GrandTotal;
            }
            else if (result.Items.Count == 0)
            {
                item.Errors = new List<string> { result.Message };
            }
        }
        catch (Exception ex)
        {
            item.Errors = new List<string> { $"Gagal memvalidasi: {ex.Message}" };
        }
    }

    private async Task RefreshTotalsAsync()
    {
        if (_items.Count == 0)
        {
            _grandTotal = 0;
            return;
        }

        var targets = _items.Where(i => !i.InputMode).ToList();
        if (targets.Count == 0)
        {
            _grandTotal = 0;
            return;
        }

        try
        {
            var requests = targets.Select(t => new OrderItemRequest
            {
                ItemName = t.ItemName,
                Quantity = t.Quantity,
                Price = t.Price
            }).ToList();
            var result = await SalesOrderApi.ValidateItemsAsync(requests);
            if (result.Success)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var row = result.Items.FirstOrDefault(r => r.Index == i);
                    if (row is null) continue;
                    targets[i].Total = row.Total;
                    targets[i].Errors = row.Errors;
                }

                _grandTotal = result.GrandTotal;
            }
        }
        catch
        {
            // keep previous values shown; service unreachable
        }
    }

    private async Task SaveOrderAsync()
    {
        _formError = null;
        _saving = true;
        try
        {
            var request = new OrderRequest
            {
                SoNo = _soNo,
                OrderDate = _orderDate,
                CustomerId = _customerId,
                Address = _address,
                Items = _items.Select(i => new OrderItemRequest
                {
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            var result = IsEdit
                ? await SalesOrderApi.UpdateOrderAsync(Id, request)
                : await SalesOrderApi.CreateOrderAsync(request);

            if (result.Success)
            {
                Nav.NavigateTo("/");
            }
            else
            {
                _formError = result.Errors.Count > 0 ? result.Errors : new List<string> { result.Message };
            }
        }
        catch (Exception ex)
        {
            _formError = new List<string> { $"Gagal menyimpan: {ex.Message}" };
        }
        finally
        {
            _saving = false;
        }
    }

    private void Close() => Nav.NavigateTo("/");

    private void DismissError() => _formError = null;

    private class ItemDto
    {
        public int LitemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public bool InputMode { get; set; }
        public List<string> Errors { get; set; } = new();
        public ItemDto? Backup { get; set; }
    }
}