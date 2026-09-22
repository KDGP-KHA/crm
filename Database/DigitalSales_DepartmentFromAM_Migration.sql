/*
    Hiển thị đơn vị của AM chủ trì qua Sys_Users.MaBoPhan.
    Không dùng RM_DigitalSales.DepartmentID cho trường DepartmentName.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @OldJoin NVARCHAR(300) = N'LEFT JOIN dbo.MN_BoPhan bp ON ds.DepartmentID = bp.BoPhan_ID';
DECLARE @NewJoin NVARCHAR(300) = N'LEFT JOIN dbo.MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan';
DECLARE @Procedures TABLE (ProcedureName SYSNAME NOT NULL);
INSERT INTO @Procedures (ProcedureName)
VALUES (N'dbo.RM_DigitalSales_GetList'), (N'dbo.RM_DigitalSales_GetByID');

DECLARE @ProcedureName SYSNAME;
WHILE EXISTS (SELECT 1 FROM @Procedures)
BEGIN
    SELECT TOP 1 @ProcedureName = ProcedureName FROM @Procedures ORDER BY ProcedureName;

    DECLARE @Definition NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P'));
    IF @Definition IS NULL
        THROW 51010, N'Không tìm thấy stored procedure DigitalSales cần cập nhật.', 1;

    DECLARE @JoinCount INT = (LEN(@Definition) - LEN(REPLACE(@Definition, @OldJoin, N''))) / LEN(@OldJoin);
    IF @JoinCount <> 1
        THROW 51011, N'Định nghĩa stored không đúng phiên bản dự kiến; dừng để tránh ghi đè thay đổi khác.', 1;

    SET @Definition = REPLACE(@Definition, @OldJoin, @NewJoin);

    DECLARE @CreatePosition INT = CHARINDEX(N'CREATE PROCEDURE', UPPER(@Definition));
    IF @CreatePosition = 0
        THROW 51012, N'Không xác định được CREATE PROCEDURE trong stored hiện hành.', 1;

    SET @Definition = STUFF(@Definition, @CreatePosition, LEN(N'CREATE'), N'ALTER');
    EXEC sys.sp_executesql @Definition;

    IF OBJECT_DEFINITION(OBJECT_ID(@ProcedureName, N'P')) NOT LIKE N'%LEFT JOIN dbo.MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan%'
        THROW 51013, N'Cập nhật nguồn đơn vị AM không thành công.', 1;

    DELETE FROM @Procedures WHERE ProcedureName = @ProcedureName;
    PRINT N'Đã cập nhật ' + @ProcedureName + N' lấy đơn vị AM từ Sys_Users.MaBoPhan.';
END
GO
