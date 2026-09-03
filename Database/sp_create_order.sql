-- =============================================================
-- sp_create_order: simpan order baru (header + item) atomic
-- Dipakai oleh POST /api/orders
-- Input  : @ItemsJson = JSON array [{itemName, quantity, price}, ...]
-- Output : @Success (bit), @Message (nvarchar), @SalesSoId (int)
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_create_order
    @SoNo       NVARCHAR(20),
    @OrderDate  DATETIME,
    @CustomerId INT,
    @Address    NVARCHAR(500),
    @ItemsJson  NVARCHAR(MAX),
    @Success    BIT             OUTPUT,
    @Message    NVARCHAR(MAX)   OUTPUT,
    @SalesSoId  INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Success   = 0;
    SET @Message   = N'';
    SET @SalesSoId = 0;

    DECLARE @ErrList TABLE (Err NVARCHAR(200));

    IF @SoNo IS NULL OR LTRIM(RTRIM(@SoNo)) = ''
        INSERT INTO @ErrList VALUES (N'Order Number tidak boleh kosong');

    IF @SoNo IS NOT NULL AND LTRIM(RTRIM(@SoNo)) <> ''
        AND EXISTS (SELECT 1 FROM dbo.SALES_SO WHERE SO_NO = LTRIM(RTRIM(@SoNo)))
        INSERT INTO @ErrList VALUES (N'Order Number sudah digunakan');

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

        SET @SalesSoId = NEXT VALUE FOR dbo.SEQ_SALES_SO;

        INSERT INTO dbo.SALES_SO (SALES_SO_ID, SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS)
        VALUES (@SalesSoId, LTRIM(RTRIM(@SoNo)), @OrderDate, @CustomerId, @Address);

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
        SET @Message = N'Order berhasil dibuat';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        SET @Success = 0;
        SET @Message = N'Gagal menyimpan order: ' + ERROR_MESSAGE();
    END CATCH
END
GO
