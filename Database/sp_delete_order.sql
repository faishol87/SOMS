-- =============================================================
-- sp_delete_order: hapus order header beserta seluruh item (atomic)
-- Dipakai oleh DELETE /api/orders/{id}
-- Output : @Success (bit), @Message (nvarchar)
-- =============================================================
USE SomSales;
GO

CREATE OR ALTER PROCEDURE dbo.sp_delete_order
    @SalesSoId INT,
    @Success   BIT           OUTPUT,
    @Message   NVARCHAR(MAX) OUTPUT
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

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.SALES_SO_LITEM WHERE SALES_SO_ID = @SalesSoId;
        DELETE FROM dbo.SALES_SO     WHERE SALES_SO_ID = @SalesSoId;

        COMMIT TRANSACTION;
        SET @Success = 1;
        SET @Message = N'Order berhasil dihapus';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        SET @Success = 0;
        SET @Message = N'Gagal menghapus order: ' + ERROR_MESSAGE();
    END CATCH
END
GO
