-- =============================================================
-- Schema Database Sales Order Management System
-- Struktur tabel FINAL sesuai Functional Specification Document
-- RUN: sqlcmd -S localhost,1433 -U sa -P <password> -i schema.sql
-- =============================================================

IF DB_ID('SomSales') IS NULL
BEGIN
    CREATE DATABASE SomSales;
END
GO

USE SomSales;
GO

-- -------------------------------------------------------------
-- Hapus objek lama dulu (untuk rerun dari nol)
-- -------------------------------------------------------------
IF OBJECT_ID('dbo.SALES_SO_LITEM', 'U') IS NOT NULL DROP TABLE dbo.SALES_SO_LITEM;
IF OBJECT_ID('dbo.SALES_SO', 'U') IS NOT NULL DROP TABLE dbo.SALES_SO;
IF OBJECT_ID('dbo.COM_CUSTOMER', 'U') IS NOT NULL DROP TABLE dbo.COM_CUSTOMER;
GO

-- -------------------------------------------------------------
-- Tabel Master Pelanggan
-- -------------------------------------------------------------
CREATE TABLE COM_CUSTOMER (
    COM_CUSTOMER_ID INT NOT NULL PRIMARY KEY,
    CUSTOMER_NAME  VARCHAR(100) NOT NULL
);
GO

-- -------------------------------------------------------------
-- Tabel Sales Order Header
-- -------------------------------------------------------------
CREATE TABLE SALES_SO (
    SALES_SO_ID     INT NOT NULL PRIMARY KEY,
    SO_NO           VARCHAR(20) NOT NULL,
    ORDER_DATE      DATETIME NOT NULL,
    COM_CUSTOMER_ID INT NOT NULL,
    ADDRESS         VARCHAR(500) NULL,
    CONSTRAINT FK_SO_CUSTOMER FOREIGN KEY (COM_CUSTOMER_ID)
        REFERENCES COM_CUSTOMER(COM_CUSTOMER_ID)
);
GO

-- Nomor order harus unik
CREATE UNIQUE NONCLUSTERED INDEX UX_SALES_SO_SO_NO
    ON SALES_SO(SO_NO);
GO

-- -------------------------------------------------------------
-- Tabel Sales Order Detail Item
-- -------------------------------------------------------------
CREATE TABLE SALES_SO_LITEM (
    SALES_SO_LITEM_ID INT NOT NULL PRIMARY KEY,
    SALES_SO_ID       INT NOT NULL,
    ITEM_NAME         VARCHAR(100) NOT NULL,
    QUANTITY          INT NOT NULL,
    PRICE             FLOAT NOT NULL,
    CONSTRAINT FK_LITEM_SO FOREIGN KEY (SALES_SO_ID)
        REFERENCES SALES_SO(SALES_SO_ID)
);
GO

-- Indeks untuk query utama pada SALES_SO_LITEM
CREATE NONCLUSTERED INDEX IX_SALES_SO_LITEM_SO_ID
    ON SALES_SO_LITEM(SALES_SO_ID);
GO

-- -------------------------------------------------------------
-- Sequence generator ID (PK manual, tanpa IDENTITY)
-- -------------------------------------------------------------
CREATE SEQUENCE dbo.SEQ_COM_CUSTOMER
    AS INT START WITH 100 INCREMENT BY 1;
GO

CREATE SEQUENCE dbo.SEQ_SALES_SO
    AS INT START WITH 100 INCREMENT BY 1;
GO

CREATE SEQUENCE dbo.SEQ_SALES_SO_LITEM
    AS INT START WITH 100 INCREMENT BY 1;
GO

-- -------------------------------------------------------------
-- Data contoh master pelanggan (minimal 3 record)
-- -------------------------------------------------------------
INSERT INTO COM_CUSTOMER (COM_CUSTOMER_ID, CUSTOMER_NAME) VALUES
    (1, 'PT Maju Bersama'),
    (2, 'CV Sejahtera Abadi'),
    (3, 'PT Karya Utama'),
    (4, 'TITAN Distribution'),
    (5, 'DIPS Pharmanet');
GO

-- -------------------------------------------------------------
-- Data contoh sales order (untuk keperluan pengembangan/testing)
-- -------------------------------------------------------------
INSERT INTO SALES_SO (SALES_SO_ID, SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS) VALUES
    (1, 'SO-2024-001', '2024-01-01 10:00:00', 1, 'Jl. Sudirman No. 1, Jakarta'),
    (2, 'SO-2024-002', '2024-01-03 09:30:00', 2, 'Jl. Gatot Subroto No. 5, Jakarta'),
    (3, 'SO-2024-003', '2024-01-05 14:15:00', 3, 'Jl. Thamrin No. 10, Jakarta'),
    (4, 'SO-2024-004', '2024-01-08 11:00:00', 4, 'Jl. Gading Serpong No. 2, Tangerang'),
    (5, 'SO-2024-005', '2024-01-10 08:45:00', 5, 'Jl. Raya Serpong Km 8, Tangerang');
GO

INSERT INTO SALES_SO_LITEM (SALES_SO_LITEM_ID, SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE) VALUES
    (1, 1, 'Laptop Dell XPS 13',     2,   15000000),
    (2, 1, 'Mouse Logitech MX',      3,   600000),
    (3, 2, 'Monitor Samsung 24"',    5,   2500000),
    (4, 2, 'Keyboard Mech. K1',      2,   850000),
    (5, 3, 'Printer Epson L3210',    1,   2400000),
    (6, 4, 'Kertas Sinar Dunia A4', 20,   45000),
    (7, 5, 'Ink Epson 003 Black',   10,   70000);
GO

PRINT 'Schema + seed data berhasil dibuat.';
GO
