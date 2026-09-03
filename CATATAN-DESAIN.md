# CATATAN-DESAIN

## Alasan pembagian service

Sistem dibagi 2 service "CustomerService" dan "SalesOrderService", karena 2 service itu memiliki fungsi yang berbeda, CustomerService untuk mengolah data master yaitu customer, dan SalesOrderService lebih ke semua proses Transaksinya

## Keputusan teknis penting

1. **Stored Procedure untuk CRUD order** — seluruh operasi header + items dikemas dalam satu SP dengan transaksi (`BEGIN TRAN ... COMMIT`) karena untuk memudahkan kalau ada kesalahan saat proses berjalan dan diperlukan rollback, untuk semua validasi dan perhitungan juga dikerjakan di dalam SP karena proses perhitungan yang ada di dalam SP bisa diproses dengan lebih cepat
2. **Kalkulasi total 100% di service** — semua kalkulasi ada di service karena proses perhitungan yang berjalan di sisi database itu dapat berjalan lebih cepat, dan tidak akan terpengaruh kalau nanti koneksi database dan website terputus ditengah proses perhitungan
3. **Blazor Server sebagai front-end**, karena menurut saya, dengan blazor kita lebih mudah untuk mengintegrasikanya dengan service -  service yang dibuat, karena memang blazor sudah dirancang untuk ekosistem .NET
4. **ADO.NET (Microsoft.Data.SqlClient)** tanpa ORM karena disini kita menggunakan SP
5. **Filter & ekspor data** ekspor data hanya mengambil data yang sedang ada di table, untuk meringankan proses eksport dan mempermudah user untuk menentukan data mana yang mau diambil
6. **Format error seragam** `{ success, message, errors }` semua error diseragamkan agar error mudah di manage dan dipahami

## Bagaimana AI membantu vs dikerjakan sendiri

AI (Copilot) membantu penulisan boilerplate awal dan penyusunan script SQL (schema + SP), serta membantu mengecek sintaks C#/Razor. Saya sendiri yang menentukan struktur folder, pemilihan teknologi (Blazor Server, ClosedXML), rancangan kontrak API, alur edit-replace-item, dan strategi validasi di SP. Setelah proses generate saya melakukan cek per code yang dibuat oleh AI agar scope yang dikerjakan sesuai dan tidak keluar dari yang seharusnya, sempat ada bug dan saya perbaiki secara mandiri

## Bagian paling menantang

Sempat ada Bug di order Item karena salah logic penerimaan error, sehingga membuat aplikasi tidak berjalan seperti seharusnya, yang kedua sempat ada masalah di cara penerimaan Error sehingga message error dari SP tidak tertampilkan di Web dan hanya menampilkan error 400
