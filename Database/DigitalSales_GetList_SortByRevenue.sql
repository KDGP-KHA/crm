/*
    RM_DigitalSales_GetList - mặc định sắp xếp tổng doanh thu dự kiến giảm dần.

    Migration này sửa trực tiếp định nghĩa hiện hành để bảo toàn toàn bộ chữ ký
    tham số, phân quyền UserName, lọc phòng ban và các điều kiện đã có.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ProcedureName SYSNAME = N'dbo.RM_DigitalSales_GetList';
DECLARE @OldOrder NVARCHAR(500) =
    N'ORDER BY ISNULL(ds.ActionTime, ISNULL(ds.LastModifiedDate, ds.CreatedDate)) DESC, ds.DigitalSalesID DESC';
DECLARE @NewOrder NVARCHAR(700) =
    N'ORDER BY ISNULL(ds.TotalExpectedRevenue, 0) DESC, ISNULL(ds.ActionTime, ISNULL(ds.LastModifiedDate, ds.CreatedDate)) DESC, ds.DigitalSalesID DESC';
DECLARE @Definition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P'));

IF @Definition IS NULL
    THROW 51000, N'Không tìm thấy stored procedure dbo.RM_DigitalSales_GetList.', 1;

DECLARE @OldOrderCount INT = (LEN(@Definition) - LEN(REPLACE(@Definition, @OldOrder, N''))) / LEN(@OldOrder);
IF @OldOrderCount <> 2
    THROW 51001, N'Định nghĩa stored không đúng phiên bản dự kiến; dừng để tránh ghi đè thay đổi khác.', 1;

SET @Definition = REPLACE(@Definition, @OldOrder, @NewOrder);

DECLARE @CreatePosition INT = CHARINDEX(N'CREATE PROCEDURE', UPPER(@Definition));
IF @CreatePosition = 0
    THROW 51002, N'Không xác định được câu lệnh CREATE PROCEDURE trong stored hiện hành.', 1;

SET @Definition = STUFF(@Definition, @CreatePosition, LEN(N'CREATE'), N'ALTER');
EXEC sys.sp_executesql @Definition;

DECLARE @VerifiedDefinition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P'));
IF @VerifiedDefinition NOT LIKE N'%ORDER BY ISNULL(ds.TotalExpectedRevenue, 0) DESC%'
    THROW 51003, N'Cập nhật sắp xếp doanh thu không thành công.', 1;

PRINT N'Đã cập nhật RM_DigitalSales_GetList: TotalExpectedRevenue DESC, sau đó ActionTime DESC.';
GO
