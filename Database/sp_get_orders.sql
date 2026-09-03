-- =============================================================
-- sp_get_orders: daftar sales order dengan filter pencarian
-- Dipakai oleh GET /api/orders dan GET /api/orders/export
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_get_orders
    @Keyword    NVARCHAR(100) = NULL,
    @OrderDate  DATETIME      = NULL
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
    WHERE (@Keyword IS NULL
              OR so.SO_NO  LIKE '%' + @Keyword + '%'
              OR c.CUSTOMER_NAME LIKE '%' + @Keyword + '%'
              OR so.ADDRESS  LIKE '%' + @Keyword + '%')
      AND (@OrderDate IS NULL
              OR so.ORDER_DATE = @OrderDate)
    ORDER BY so.SALES_SO_ID DESC;
END
GO
