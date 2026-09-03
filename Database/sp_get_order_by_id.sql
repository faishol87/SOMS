-- =============================================================
-- sp_get_order_by_id: detail satu order beserta seluruh item
-- Dipakai oleh GET /api/orders/{id}
-- Hasil: result set pertama = header, kedua = list item
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_get_order_by_id
    @SalesSoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        so.SALES_SO_ID,
        so.SO_NO,
        so.ORDER_DATE,
        so.COM_CUSTOMER_ID,
        c.CUSTOMER_NAME,
        so.ADDRESS,
        ISNULL((SELECT SUM(l.QUANTITY * l.PRICE)
                FROM dbo.SALES_SO_LITEM l
                WHERE l.SALES_SO_ID = so.SALES_SO_ID), 0) AS GRAND_TOTAL
    FROM dbo.SALES_SO so
    INNER JOIN dbo.COM_CUSTOMER c
        ON c.COM_CUSTOMER_ID = so.COM_CUSTOMER_ID
    WHERE so.SALES_SO_ID = @SalesSoId;

    SELECT
        l.SALES_SO_LITEM_ID,
        l.SALES_SO_ID,
        l.ITEM_NAME,
        l.QUANTITY,
        l.PRICE,
        (l.QUANTITY * l.PRICE) AS TOTAL
    FROM dbo.SALES_SO_LITEM l
    WHERE l.SALES_SO_ID = @SalesSoId
    ORDER BY l.SALES_SO_LITEM_ID;
END
GO
