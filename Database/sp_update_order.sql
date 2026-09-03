-- =============================================================
-- sp_update_order: update order header + replace semua item (atomic)
-- Dipakai oleh PUT /api/orders/{id}
-- Input  : @ItemsJson = JSON array [{itemName, quantity, price}, ...]
-- Output : @Success (bit), @Message (nvarchar)
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_update_order
    @SalesSoId  INT,
    @OrderDate  DATETIME,
    @CustomerId INT,
    @Address    NVARCHAR(500),
    @ItemsJson  NVARCHAR(MAX),
    @Success    BIT             OUTPUT,
    @Message    NVARCHAR(MAX)   OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Success = 0;
    SET @Message = N'';

    IF NOT EXISTS (SELECT 1 FROM dbo.SALES_SO WHERE SALES_SO_ID = @SalesSoId)
    BEGIN
        SET @Message = N'Order tidak ditemukan';
        RETURN;
    END

    DECLARE @ErrList TABLE (Err NVARCHAR(200));

    IF @OrderDate IS NULL
        INSERT INTO @ErrList VALUES (N'Order Date tidak boleh kosong');

    IF @CustomerId IS NULL
        INSERT INTO @ErrList VALUES (N'Customer harus dipilih');

    DECLARE @ItemCount INT = 0;
    SELECT @ItemCount = COUNT(*)
    FROM OPENJSON(@ItemsJson)
    WITH (ItemName NVARCHAR(100) '$.itemName', Quantity INT '$.quantity', Price FLOAT '$.price');

    IF @ItemCount = 0
        INSERT INTO @ErrList VALUES (N'Order harus memiliki minimal 1 item');

    IF EXISTS (SELECT 1 FROM @ErrList)
    BEGIN
        SET @Message = (SELECT STRING_AGG(Err, ' | ') FROM @ErrList);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SALES_SO
        SET ORDER_DATE      = @OrderDate,
            COM_CUSTOMER_ID = @CustomerId,
            ADDRESS         = @Address
        WHERE SALES_SO_ID = @SalesSoId;

        DELETE FROM dbo.SALES_SO_LITEM WHERE SALES_SO_ID = @SalesSoId;

        INSERT INTO dbo.SALES_SO_LITEM (SALES_SO_LITEM_ID, SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE)
        SELECT
            NEXT VALUE FOR dbo.SEQ_SALES_SO_LITEM,
            @SalesSoId,
            ItemName,
            Quantity,
            Price
        FROM OPENJSON(@ItemsJson)
        WITH (ItemName NVARCHAR(100) '$.itemName', Quantity INT '$.quantity', Price FLOAT '$.price');

        COMMIT TRANSACTION;
        SET @Success = 1;
        SET @Message = N'Order berhasil diperbarui';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        SET @Success = 0;
        SET @Message = N'Gagal memperbarui order: ' + ERROR_MESSAGE();
    END CATCH
END
GO
