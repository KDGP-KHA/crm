/*
    RM_DigitalSales_GetList - Lọc theo Nhân viên: hỗ trợ tìm kiếm cả Nhân viên phụ trách (AssignedEmployeeID)
    và các cán bộ thuộc Thành viên tham gia & Vai trò (RM_DigitalSalesMember).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ProcedureName SYSNAME = N'dbo.RM_DigitalSales_GetList';
DECLARE @Definition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P'));

IF @Definition IS NULL
    THROW 51000, N'Không tìm thấy stored procedure dbo.RM_DigitalSales_GetList.', 1;

DECLARE @Target NVARCHAR(100) = N'OR ds.AssignedEmployeeID = @EmployeeID';

DECLARE @Replacement NVARCHAR(500) = N'OR ds.AssignedEmployeeID = @EmployeeID
          OR EXISTS
          (
              SELECT 1
              FROM dbo.RM_DigitalSalesMember member
              WHERE member.DigitalSalesID = ds.DigitalSalesID
                AND member.UserID = @EmployeeID
                AND ISNULL(member.IsActive, 0) = 1
          )';

-- Đếm số vị trí cần thay thế
DECLARE @Occurrences INT = (LEN(@Definition) - LEN(REPLACE(@Definition, @Target, N''))) / LEN(@Target);
IF @Occurrences <> 2
    THROW 51001, N'Số lượng vị trí @EmployeeID không khớp (cần đúng 2 vị trí: đếm tổng và phân trang).', 1;

SET @Definition = REPLACE(@Definition, @Target, @Replacement);

DECLARE @CreatePosition INT = CHARINDEX(N'CREATE PROCEDURE', UPPER(@Definition));
IF @CreatePosition = 0
    THROW 51002, N'Không xác định được câu lệnh CREATE PROCEDURE trong stored hiện hành.', 1;

SET @Definition = STUFF(@Definition, @CreatePosition, LEN(N'CREATE'), N'ALTER');
EXEC sys.sp_executesql @Definition;

DECLARE @VerifiedDefinition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P'));
IF @VerifiedDefinition NOT LIKE N'%FROM dbo.RM_DigitalSalesMember member%member.UserID = @EmployeeID%'
    THROW 51003, N'Cập nhật lọc nhân viên theo thành viên tham gia không thành công.', 1;

PRINT N'Đã cập nhật thành công RM_DigitalSales_GetList: lọc nhân viên bao gồm cả Thành viên tham gia & Vai trò.';
GO
