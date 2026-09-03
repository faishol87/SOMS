-- =============================================================
-- sp_validate_items: validasi item & kalkulasi TOTAL per baris
-- Dipakai oleh POST /api/orders/validate (front-end saat Save Baris)
-- Input  : @ItemsJson = JSON array [{itemName, quantity, price}, ...]
-- Output : JSON {success, message, grandTotal, items:[{index,total,errors}]}
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_validate_items
    @ItemsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Idx     INT,
            @ItemName NVARCHAR(100),
            @Quantity INT,
            @Price    FLOAT;

    DECLARE @GrandTotal FLOAT = 0;
    DECLARE @RowsValid  BIT   = 1;
    DECLARE @ItemErrors NVARCHAR(MAX) = N'';

    DECLARE @Result TABLE
    (
        Idx       INT          NOT NULL,
        Total     FLOAT        NOT NULL,
        Errors    NVARCHAR(MAX) NULL
    );

    DECLARE cur CURSOR FOR
        SELECT SUB.*
        FROM OPENJSON(@ItemsJson)
        WITH (
            ItemName NVARCHAR(100) '$.itemName',
            Quantity INT           '$.quantity',
            Price    FLOAT         '$.price'
        ) SUB
        ORDER BY (SELECT NULL);

    OPEN cur;
    FETCH NEXT FROM cur INTO @ItemName, @Quantity, @Price;

    SET @Idx = 0;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @ItemErrors = N'';

        IF @ItemName IS NULL OR LTRIM(RTRIM(@ItemName)) = ''
            SET @ItemErrors = 'Item Name tidak boleh kosong';

        IF @Quantity IS NULL OR @Quantity <= 0
            SET @ItemErrors = CONCAT(@ItemErrors, CONCAT(
                CASE WHEN LEN(@ItemErrors) > 0 THEN ' | ' ELSE '' END,
                'QTY harus berupa angka lebih dari 0'));

        IF @Price IS NULL OR @Price <= 0
            SET @ItemErrors = CONCAT(@ItemErrors, CONCAT(
                CASE WHEN LEN(@ItemErrors) > 0 THEN ' | ' ELSE '' END,
                'Price harus berupa angka lebih dari 0'));

        INSERT INTO @Result (Idx, Total, Errors)
        VALUES (@Idx,
                CASE WHEN LEN(@ItemErrors) > 0 THEN 0 ELSE @Quantity * @Price END,
                NULLIF(@ItemErrors, ''));

        IF LEN(@ItemErrors) > 0 SET @RowsValid = 0;

        SELECT @GrandTotal = @GrandTotal + CASE WHEN LEN(@ItemErrors) > 0 THEN 0 ELSE @Quantity * @Price END;

        SET @Idx = @Idx + 1;
        FETCH NEXT FROM cur INTO @ItemName, @Quantity, @Price;
    END
    CLOSE cur;
    DEALLOCATE cur;

    SELECT
        CASE WHEN @RowsValid = 1 THEN 1 ELSE 0 END                          AS Success,
        CASE WHEN @RowsValid = 1 THEN 'OK' ELSE 'Terdapat item tidak valid' END AS Message,
        @GrandTotal                                                          AS GrandTotal,
        (SELECT Idx AS [index], Total AS [total], Errors AS [errors] FROM @Result ORDER BY Idx
         FOR JSON PATH)                                                      AS Items
    ;
END
GO
