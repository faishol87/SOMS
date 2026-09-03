# Sales Order Management System

Technical Test — .NET Developer (Profescipta). Sistem manajemen sales order berbasis web dengan arsitektur microservices: **C# / .NET 8 · SQL Server · Blazor Web App (Blazor Server)**.

## Struktur Repository

```
SOMS/
├── CustomerService/     ← Web API .NET 8 (port 5001) — data master pelanggan
├── SalesOrderService/   ← Web API .NET 8 (port 5002) — sales order, total, ekspor Excel
├── FrontEnd/            ← Blazor Web App (port 5000) — lapisan tampilan
├── Database/            ← script SQL: schema + stored procedure
├── SOMS.slnx            ← solution
└── README.md
```

## Prasyarat

- **.NET 8 SDK** (atau lebih baru)
- **SQL Server** (lokal atau container) — contoh: `docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=password -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`
- `sqlcmd` (opsional, bisa lewat Docker)

## Setup Database (dari nol)

```bash
# 1. Jalankan schema (buat database SomSales, tabel, seed data, sequence)
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/schema.sql

# 2. Buat seluruh stored procedure (jalankan tiap file)
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_get_orders.sql
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_get_order_by_id.sql
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_validate_items.sql
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_create_order.sql
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_update_order.sql
sqlcmd -S localhost,1433 -U sa -P 'password' -i Database/sp_delete_order.sql
```

Tanpa `sqlcmd` di host, bisa pakai container: ganti prefix dengan
`docker run --rm --network host -v /path/ke/Database:/sql mcr.microsoft.com/mssql/server:2022-latest /opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -P 'password' -C -i /sql/...`.

> Seed data memuat 5 pelanggan, 5 sales order, dan 7 item contoh.

## Menjalankan (3 terminal)

```bash
dotnet run --project CustomerService   # http://localhost:5001 (Swagger: /swagger)
dotnet run --project SalesOrderService # http://localhost:5002 (Swagger: /swagger)
dotnet run --project FrontEnd          # http://localhost:5000
```

Buka **http://localhost:5000**.

> Port mengikuti saran FSD (5000/5001/5002). Ubah via `Properties/launchSettings.json`.

### Konfigurasi

Connection string di `appsettings.json` masing-masing service (bisa dioverride environment variable):

```json
"ConnectionStrings": {
  "SqlServer": "Server=localhost,1433;Database=SomSales;User Id=sa;Password=password;TrustServerCertificate=True"
}
```

URL service yang dipanggil FrontEnd di `FrontEnd/appsettings.json`:

```json
"ServiceUrls": {
  "CustomerService": "http://localhost:5001",
  "SalesOrderService": "http://localhost:5002"
}
```

## Arsitektur & Alur

- **Front-End = cangkang tampilan saja.** Tidak menyentuh database, tidak ada connection string di project FrontEnd
- Semua validasi & **kalkulasi TOTAL / Grand Total dilakukan di SalesOrderService** (SP `sp_validate_items` / `sp_create_order` / `sp_update_order`), bukan JavaScript/Blazor di front-end.
- Komunikasi antar service hanya melalui **HTTP REST API**.
- Database shared `SomSales`, diakses sesuai domain masing-masing service.

```
Browser → FrontEnd (5000) ──GET /api/customers──→ CustomerService (5001) ──→ SQL Server
                       └──/api/orders*──────────→ SalesOrderService (5002) ──→ SQL Server
```

## API

### CustomerService — `http://localhost:5001`

| Method | Endpoint | Keterangan |
| --- | --- | --- |
| GET | `/api/customers` | Semua pelanggan `[{customerId, customerName}]` |
| GET | `/api/customers/{id}` | Satu pelanggan |
| POST | `/api/customers` | Buat pelanggan `{ customerName }` |
| PUT | `/api/customers/{id}` | Update pelanggan |
| DELETE | `/api/customers/{id}` | Hapus pelanggan |

### SalesOrderService — `http://localhost:5002`

| Method | Endpoint | Keterangan |
| --- | --- | --- |
| GET | `/api/orders?keyword=&orderDate=` | Daftar order (filter opsional `keyword` + `orderDate` format `YYYY-MM-DD`) |
| GET | `/api/orders/{id}` | Detail order + items |
| POST | `/api/orders` | Buat order — 400 dengan `{ success:false, message, errors[] }` bila validasi gagal |
| PUT | `/api/orders/{id}` | Update order (replace seluruh item) |
| DELETE | `/api/orders/{id}` | Hapus order beserta seluruh item (transaksi atomik) — **wajib header `X-Api-Key`** |
| GET | `/api/orders/export?keyword=&orderDate=` | Unduh `.xlsx` (data sesuai filter aktif) |
| POST | `/api/orders/validate` | Validasi & hitung TOTAL baris + grand total |

**Format error seragam** (semua service):

```json
{ "success": false, "message": "Penjelasan singkat", "errors": ["detail error 1", "detail error 2"] }
```

## Keamanan API (API Key)

Saat ini **satu endpoint** yang diamankan dengan API key:

| Service | Endpoint | Syarat |
| --- | --- | --- |
| SalesOrderService | `DELETE /api/orders/{id}` | Header `X-Api-Key: <key>` wajib dikirim |

Key didaftarkan di config **dengan nilai yang sama** di kedua project:

```json
// SalesOrderService/appsettings.json — dicek oleh attribute [ApiKeyAuth]
"ApiKey": {
  "Value": "ganti-dengan-kunci-rahasia-kuat"
}
```

```json
// FrontEnd/appsettings.json — dikirim otomatis oleh ApiKeyHandler
"ServiceUrls": {
  "ApiKey": "ganti-dengan-kunci-rahasia-kuat"
}
```

> FrontEnd (Blazor Server) mengirim header `X-Api-Key` otomatis pada tiap panggilan ke SalesOrderService, jadi key tidak pernah terekspos ke browser.
>
> Di produksi, jangan simpan key asli di `appsettings.json` yang ter-commit — gunakan environment variable (`ApiKey__Value`, `ServiceUrls__ApiKey`) atau secret manager.

## Contoh Pemanggilan API

Semua contoh memakai `curl`. Hanya `DELETE /api/orders/{id}` yang wajib menyertakan header `X-Api-Key`.

```bash
# 1) Ambil daftar pelanggan — tanpa key
curl http://localhost:5001/api/customers

# 2) Ambil daftar order + filter (keyword / orderDate opsional)
curl "http://localhost:5002/api/orders?keyword=SO-001&orderDate=2025-01-15"

# 3) Detail order
curl http://localhost:5002/api/orders/1

# 4) Buat order — tanpa key
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{ "orderDate": "2025-01-15", "customerId": 1, "items": [ { "itemName": "Item A", "quantity": 2, "price": 50000 } ] }'

# 5) Hapus order — WAJIB header X-Api-Key
curl -X DELETE http://localhost:5002/api/orders/1 \
  -H "X-Api-Key: ganti-dengan-kunci-rahasia-kuat"
```

Format `.http` (bisa dijalankan dari Visual Studio / VS Code REST Client):

```http
### Hapus order (butuh API key)
DELETE http://localhost:5002/api/orders/1
X-Api-Key: ganti-dengan-kunci-rahasia-kuat

###
```

Request tanpa key / dengan key salah akan ditolak `401`:

```json
{ "message": "API key tidak valid atau tidak disediakan" }
```

## Fitur Front-End

- **Order List**: cari keyword (SO_NO / customer / address, case-insensitive + tanggal), pagination, edit, hapus dengan konfirmasi popup dinamis, ekspor Excel.
- **Order Input (Create)**: input header, tambah baris item (simpan/cancel/edit/hapus per baris), total dihitung service, tombol save/close.
- **Order Input (Edit)**: order number read-only, replace semua item saat save (PUT).

## Build

```bash
dotnet build SOMS.slnx
dotnet build CustomerService  # atau tiap project
```
