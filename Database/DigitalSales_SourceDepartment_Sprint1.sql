﻿﻿/*
========================================================================================
    HỆ THỐNG CENIT TOC CRM - PHÂN HỆ KINH DOANH SPDV SỐ (RM_DIGITALSALES)
    SPRINT 1: NỀN TẢNG CƠ SỞ DỮ LIỆU, SYS_MESSAGES & PHÂN QUYỀN 3 RULES
    - US-21: Khai báo Resource Text vào bảng Sys_Messages
    - US-01: Bổ sung cột SourceDepartmentID, FK và Index vào RM_DigitalSales
    - US-02: Stored Procedure dbo.MN_BoPhan_GetLevel1Departments (28 đơn vị con cấp 1 VTKH)
    - US-03: Xử lý an toàn dữ liệu hiện có (SourceDepartmentID = NULL)
    - US-14: Tái cấu trúc SP dbo.RM_DigitalSales_GetList (3 Rules phân quyền & lọc @SourceDepartmentID)
    - US-16: Cập nhật SP dbo.RM_DigitalSales_GetDashboardStatusStats (3 Rules phân quyền)
    - Cập nhật SP dbo.RM_DigitalSales_GetByID & dbo.RM_DigitalSales_Save
    - SP dbo.RM_DigitalSales_QuickAssignSourceDepartment (Cập nhật nhanh 1-Click)
========================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT N'>>> 1. KHỞI TẠO RESOURCE TEXT TRONG SYS_MESSAGES (US-21)...';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_SourceDepartment', N'Cơ hội xuất phát từ');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Cơ hội xuất phát từ' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_Placeholder')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_SourceDepartment_Placeholder', N'-- Chọn đơn vị xuất phát --');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'-- Chọn đơn vị xuất phát --' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_Placeholder';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_Help')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_SourceDepartment_Help', N'Chọn đơn vị địa bàn / bộ phận khởi nguồn cơ hội kinh doanh');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Chọn đơn vị địa bàn / bộ phận khởi nguồn cơ hội kinh doanh' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_Help';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_Label')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSalesSearch_SourceDepartment_Label', N'Đơn vị xuất phát');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Đơn vị xuất phát' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_Label';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_AllOption')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSalesSearch_SourceDepartment_AllOption', N'-- Tất cả đơn vị xuất phát --');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'-- Tất cả đơn vị xuất phát --' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_AllOption';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_Unassigned')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSalesSearch_SourceDepartment_Unassigned', N'⚠️ -- Chưa chọn đơn vị xuất phát --');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'⚠️ -- Chưa chọn đơn vị xuất phát --' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_SourceDepartment_Unassigned';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_MemberDepartment_Label')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSalesSearch_MemberDepartment_Label', N'Đơn vị thành viên thực hiện');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Đơn vị thành viên thực hiện' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSalesSearch_MemberDepartment_Label';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_Col_SourceDepartment')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_Col_SourceDepartment', N'Đơn vị xuất phát');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Đơn vị xuất phát' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_Col_SourceDepartment';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_Validation_SourceDepartment_Required')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_Validation_SourceDepartment_Required', N'Vui lòng chọn đơn vị xuất phát cơ hội');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Vui lòng chọn đơn vị xuất phát cơ hội' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_Validation_SourceDepartment_Required';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_Title')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_QuickAssign_Title', N'Cập nhật Đơn vị xuất phát');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Cập nhật Đơn vị xuất phát' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_Title';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_Success')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_QuickAssign_Success', N'Cập nhật đơn vị xuất phát thành công!');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Cập nhật đơn vị xuất phát thành công!' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_Success';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_UnassignedBadge')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_QuickAssign_UnassignedBadge', N'Chưa chọn đơn vị');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Chưa chọn đơn vị' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_QuickAssign_UnassignedBadge';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_SourcePrefix')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'DigitalSales_SourceDepartment_SourcePrefix', N'Nguồn:');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Nguồn:' WHERE LangCode = 'vi-VN' AND LabelKey = 'DigitalSales_SourceDepartment_SourcePrefix';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'Dashboard_Filter_SourceDepartment')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'Dashboard_Filter_SourceDepartment', N'Đơn vị xuất phát');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Đơn vị xuất phát' WHERE LangCode = 'vi-VN' AND LabelKey = 'Dashboard_Filter_SourceDepartment';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey = 'Dashboard_Chart_SourceDepartment_Title')
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', 'Dashboard_Chart_SourceDepartment_Title', N'Cơ cấu Cơ hội theo Đơn vị xuất phát');
ELSE
    UPDATE dbo.Sys_Messages SET Message = N'Cơ cấu Cơ hội theo Đơn vị xuất phát' WHERE LangCode = 'vi-VN' AND LabelKey = 'Dashboard_Chart_SourceDepartment_Title';
GO

PRINT N'>>> Đã cập nhật xong Resource Text trong Sys_Messages.';
GO

PRINT N'>>> 2. BỔ SUNG CỘT SOURCEDEPARTMENTID, FK VÀ INDEX (US-01, US-03)...';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RM_DigitalSales' AND COLUMN_NAME = 'SourceDepartmentID'
)
BEGIN
    ALTER TABLE dbo.RM_DigitalSales ADD SourceDepartmentID INT NULL;
    PRINT N'Đã thêm cột SourceDepartmentID vào bảng RM_DigitalSales.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_RM_DigitalSales_SourceDepartment'
)
BEGIN
    ALTER TABLE dbo.RM_DigitalSales 
    ADD CONSTRAINT FK_RM_DigitalSales_SourceDepartment 
    FOREIGN KEY (SourceDepartmentID) REFERENCES dbo.MN_BoPhan(BoPhan_ID);
    PRINT N'Đã tạo khóa ngoại FK_RM_DigitalSales_SourceDepartment.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_RM_DigitalSales_SourceDepartmentID' AND object_id = OBJECT_ID('dbo.RM_DigitalSales')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RM_DigitalSales_SourceDepartmentID 
    ON dbo.RM_DigitalSales(SourceDepartmentID, IsDeleted, ApplyYear) 
    INCLUDE (DigitalSalesID, Title, Code, CustomerID, AssignedEmployeeID, StatusID);
    PRINT N'Đã tạo Index IX_RM_DigitalSales_SourceDepartmentID.';
END
GO

PRINT N'>>> 3. TẠO STORED PROCEDURE LẤY 28 ĐƠN VỊ CẤP 1 VTKH (US-02)...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.MN_BoPhan_GetLevel1Departments') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.MN_BoPhan_GetLevel1Departments AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.MN_BoPhan_GetLevel1Departments
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Chỉ lấy các đơn vị con trực tiếp của Viễn thông Khánh Hòa (BoPhanCha_ID = 5741)
    -- Tuyệt đối không lấy các đơn vị con của những đơn vị này
    SELECT 
        bp.BoPhan_ID AS BoPhanID,
        bp.MaBoPhan,
        bp.TenBoPhan
    FROM dbo.MN_BoPhan bp
    WHERE bp.Da_Xoa = 0 
      AND bp.BoPhanCha_ID = 5741
    ORDER BY bp.MaBoPhan, bp.TenBoPhan;
END
GO

PRINT N'>>> 4. TẠO STORED PROCEDURE CẬP NHẬT NHANH ĐƠN VỊ XUẤT PHÁT (QUICK ASSIGN)...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_QuickAssignSourceDepartment') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_QuickAssignSourceDepartment AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.RM_DigitalSales_QuickAssignSourceDepartment
    @DigitalSalesID INT,
    @SourceDepartmentID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @DigitalSalesID <= 0 OR @SourceDepartmentID <= 0
    BEGIN
        SELECT -1 AS Result;
        RETURN -1;
    END

    -- Xác thực đơn vị có thuộc 28 đơn vị con cấp 1 của VTKH
    IF NOT EXISTS (
        SELECT 1 FROM dbo.MN_BoPhan 
        WHERE BoPhan_ID = @SourceDepartmentID AND Da_Xoa = 0 AND BoPhanCha_ID = 5741
    )
    BEGIN
        SELECT -2 AS Result; -- Đơn vị không hợp lệ
        RETURN -2;
    END

    UPDATE dbo.RM_DigitalSales
    SET 
        SourceDepartmentID = @SourceDepartmentID,
        LastModifiedDate = GETDATE(),
        LastModifiedBy = @UserName,
        ActionTime = GETDATE()
    WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0;

    SELECT 1 AS Result;
    RETURN 1;
END
GO

PRINT N'>>> 5. CẬP NHẬT STORED PROCEDURE RM_DIGITALSALES_GETBYID...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_GetByID') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_GetByID AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.RM_DigitalSales_GetByID
    @DigitalSalesID INT,
    @UserName VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ds.DigitalSalesID,
        ds.Code,
        ds.Title,
        ds.BusinessType,
        CASE ds.BusinessType WHEN 1 THEN N'Cơ hội' WHEN 2 THEN N'Dự án' ELSE N'Khác' END AS BusinessTypeName,
        ds.StatusID,
        st.StatusCode,
        st.StatusName,
        ds.CustomerID,
        c.CustomerName,
        ds.ContactPerson_ID,
        cp.FullName AS ContactPersonName,
        cp.Phone AS ContactPersonPhone,
        cp.Email AS ContactPersonEmail,
        CAST(ISNULL((
            SELECT SUM(dsp.ExpectedRevenue) 
            FROM dbo.RM_DigitalSalesProduct dsp 
            WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
        ), 0) AS DECIMAL(18, 2)) AS TotalExpectedRevenue,
        CAST(COALESCE(
            (
                SELECT SUM(ISNULL(CASE WHEN ISNULL(c.TotalAmount, 0) > 0 THEN c.TotalAmount ELSE c.ContractValue END, 0)) * 1000000
                FROM dbo.RM_DigitalSalesProductContract pc
                INNER JOIN dbo.RM_DigitalSalesProduct dsp ON dsp.SalesProductID = pc.SalesProductID
                INNER JOIN dbo.RM_Contracts c ON c.ContractID = pc.ContractID
                WHERE dsp.DigitalSalesID = ds.DigitalSalesID
                  AND pc.IsDeleted = 0
                  AND ISNULL(c.IsDeleted, 0) = 0
                  AND dsp.IsDeleted = 0
            ),
            ds.ContractValue,
            0
        ) AS DECIMAL(18, 2)) AS TotalActualRevenue,
        ds.ClosingProbability,
        ds.ExpectedDate,
        ds.StartDate,
        ds.EndDate,
        ds.ContractID,
        ds.ContractNo,
        ds.ContractValue,
        ds.ContractSignDate,
        ds.AssignedEmployeeID,
        u.FullName AS AssignedEmployeeName,
        ds.DepartmentID,
        bp.TenBoPhan AS DepartmentName,
        ds.SourceDepartmentID,
        sbp.TenBoPhan AS SourceDepartmentName,
        ds.Note,
        ds.FileAttach,
        ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) AS ApplyYear,
        ISNULL(ds.IsKeyProject, 0) AS IsKeyProject,
        CASE 
            WHEN @UserName IS NOT NULL AND EXISTS (
                SELECT 1 FROM dbo.RM_DigitalSalesFollow f 
                WHERE f.DigitalSalesID = ds.DigitalSalesID AND f.UserName = @UserName
            ) THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS IsFollowed,
        ds.CreatedDate,
        ds.CreatedBy,
        uCreate.FullName AS CreatedByName,
        ds.LastModifiedDate,
        ds.LastModifiedBy,
        ds.ActionTime
    FROM dbo.RM_DigitalSales ds
    INNER JOIN dbo.RM_DigitalSalesStatus st ON ds.StatusID = st.StatusID
    LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
    LEFT JOIN dbo.RM_ContactPersons cp ON ds.ContactPerson_ID = cp.ContactPerson_ID
    LEFT JOIN dbo.Sys_Users u ON ds.AssignedEmployeeID = u.UserId
    LEFT JOIN dbo.Sys_Users uCreate ON ds.CreatedBy = uCreate.UserName
    OUTER APPLY (
        SELECT TOP 1 b.TenBoPhan 
        FROM dbo.MN_BoPhan b 
        WHERE b.MaBoPhan = u.MaBoPhan AND b.Da_Xoa = 0 
        ORDER BY b.BoPhan_ID DESC
    ) bp
    LEFT JOIN dbo.MN_BoPhan sbp ON ds.SourceDepartmentID = sbp.BoPhan_ID
    WHERE ds.DigitalSalesID = @DigitalSalesID AND ds.IsDeleted = 0;
END
GO

PRINT N'>>> 6. CẬP NHẬT STORED PROCEDURE RM_DIGITALSALES_SAVE...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_Save') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_Save AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.RM_DigitalSales_Save
    @DigitalSalesID INT,
    @Code VARCHAR(50) = NULL,
    @Title NVARCHAR(255),
    @BusinessType TINYINT = 1,          -- Mặc định 1: Cơ hội
    @StatusID INT = 1,                  -- Mặc định 1: Chưa tiếp cận
    @CustomerID INT,
    @ContactPerson_ID INT = NULL,
    @ClosingProbability DECIMAL(5,2) = NULL,
    @ExpectedDate DATETIME = NULL,
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @ContractID INT = NULL,
    @ContractNo NVARCHAR(100) = NULL,
    @ContractValue DECIMAL(18,2) = NULL,
    @ContractSignDate DATETIME = NULL,
    @AssignedEmployeeID INT = NULL,     -- AM
    @DepartmentID INT = NULL,
    @Note NVARCHAR(MAX) = NULL,
    @FileAttach NVARCHAR(MAX) = NULL,
    @UserName VARCHAR(150),
    @ApplyYear INT = NULL,              -- Đặt ở gần cuối để tương thích ngược
    @SourceDepartmentID INT = NULL      -- Cơ hội xuất phát từ (Đơn vị con cấp 1 của VTKH)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Tự động sinh mã nếu chưa có
        IF @Code IS NULL OR LTRIM(RTRIM(@Code)) = ''
        BEGIN
            DECLARE @CurrentYear VARCHAR(4) = CAST(YEAR(GETDATE()) AS VARCHAR(4));
            DECLARE @NextSeq INT;
            SELECT @NextSeq = ISNULL(MAX(DigitalSalesID), 0) + 1 FROM dbo.RM_DigitalSales;
            SET @Code = 'SPDV-' + @CurrentYear + '-' + RIGHT('0000' + CAST(@NextSeq AS VARCHAR(10)), 4);
        END

        -- Xác định AM nếu chưa truyền
        IF @AssignedEmployeeID IS NULL OR @AssignedEmployeeID <= 0
        BEGIN
            SELECT TOP 1 @AssignedEmployeeID = UserId FROM dbo.Sys_Users WHERE UserName = @UserName;
        END

        IF @ApplyYear IS NULL OR @ApplyYear <= 0
        BEGIN
            SET @ApplyYear = YEAR(GETDATE());
        END

        IF @DigitalSalesID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @DigitalSalesID)
        BEGIN
            INSERT INTO dbo.RM_DigitalSales
            (
                Code, Title, BusinessType, StatusID, CustomerID, ContactPerson_ID,
                TotalExpectedRevenue, TotalActualRevenue, ClosingProbability,
                ExpectedDate, StartDate, EndDate,
                ContractID, ContractNo, ContractValue, ContractSignDate,
                AssignedEmployeeID, DepartmentID, SourceDepartmentID, Note, FileAttach, ApplyYear,
                IsDeleted, CreatedDate, CreatedBy, ActionTime, LastModifiedDate, LastModifiedBy
            )
            VALUES
            (
                @Code, @Title, ISNULL(@BusinessType, 1), ISNULL(@StatusID, 1), @CustomerID, @ContactPerson_ID,
                0, ISNULL(@ContractValue, 0), @ClosingProbability,
                @ExpectedDate, @StartDate, @EndDate,
                @ContractID, @ContractNo, @ContractValue, @ContractSignDate,
                @AssignedEmployeeID, @DepartmentID, @SourceDepartmentID, @Note, @FileAttach, @ApplyYear,
                0, GETDATE(), @UserName, GETDATE(), GETDATE(), @UserName
            );

            SET @DigitalSalesID = SCOPE_IDENTITY();

            -- Ghi nhận dòng thời gian khởi tạo
            INSERT INTO dbo.RM_DigitalSalesTimeline
            (
                DigitalSalesID, FromStatusID, ToStatusID, FromBusinessType, ToBusinessType, ActionDate, ActionBy, Note
            )
            VALUES
            (
                @DigitalSalesID, NULL, ISNULL(@StatusID, 1), NULL, ISNULL(@BusinessType, 1), GETDATE(), @UserName,
                N'Khởi tạo hồ sơ kinh doanh sản phẩm dịch vụ số. AM chủ trì: ' + ISNULL(@UserName, '')
            );

            -- Tự động thêm AM vào bảng thành viên
            IF @AssignedEmployeeID IS NOT NULL AND @AssignedEmployeeID > 0
            BEGIN
                INSERT INTO dbo.RM_DigitalSalesMember (DigitalSalesID, UserID, RoleTitle, IsAM, Note, IsActive, CreatedDate, CreatedBy)
                VALUES (@DigitalSalesID, @AssignedEmployeeID, N'AM (Chủ trì kinh doanh)', 1, N'Người tạo hồ sơ cơ hội', 1, GETDATE(), @UserName);
            END

            -- Tự động sinh tiến trình theo quy trình ban đầu
            INSERT INTO dbo.RM_DigitalSalesTracking (DigitalSalesID, ProcessID, ProgressID, TaskName, AssignedUserID, StartDate, Deadline, Status, IsCustomTask, SortOrder, CreatedDate, CreatedBy)
            SELECT 
                @DigitalSalesID,
                p.ProcessID,
                pg.ProgressID,
                pg.ProgressName,
                @AssignedEmployeeID,
                GETDATE(),
                DATEADD(day, ISNULL(pg.DefaultDurationDays, 3), GETDATE()),
                1, -- Chưa làm
                0,
                pg.SortOrder,
                GETDATE(),
                @UserName
            FROM dbo.RM_DigitalSalesProcess p
            INNER JOIN dbo.RM_DigitalSalesProgress pg ON p.ProcessID = pg.ProcessID
            WHERE p.StatusID = ISNULL(@StatusID, 1) 
              AND p.IsActive = 1 
              AND p.IsDeleted = 0 
              AND pg.IsActive = 1 
              AND pg.IsDeleted = 0;
        END
        ELSE
        BEGIN
            UPDATE dbo.RM_DigitalSales
            SET
                Title = @Title,
                CustomerID = @CustomerID,
                ContactPerson_ID = @ContactPerson_ID,
                ClosingProbability = @ClosingProbability,
                ExpectedDate = @ExpectedDate,
                StartDate = @StartDate,
                EndDate = @EndDate,
                ContractID = @ContractID,
                ContractNo = @ContractNo,
                ContractValue = @ContractValue,
                TotalActualRevenue = ISNULL(@ContractValue, 0),
                ContractSignDate = @ContractSignDate,
                AssignedEmployeeID = @AssignedEmployeeID,
                DepartmentID = @DepartmentID,
                SourceDepartmentID = @SourceDepartmentID,
                Note = @Note,
                FileAttach = @FileAttach,
                ApplyYear = ISNULL(@ApplyYear, ApplyYear),
                ActionTime = GETDATE(),
                LastModifiedDate = GETDATE(),
                LastModifiedBy = @UserName
            WHERE DigitalSalesID = @DigitalSalesID;
        END

        COMMIT TRANSACTION;
        SELECT @DigitalSalesID;
        RETURN @DigitalSalesID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH
END
GO

PRINT N'>>> 7. CẬP NHẬT STORED PROCEDURE RM_DIGITALSALES_GETDASHBOARDSTATUSSTATS (3 RULES)...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_GetDashboardStatusStats') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_GetDashboardStatusStats AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.RM_DigitalSales_GetDashboardStatusStats
    @ApplyYear INT = NULL, 
    @UserName VARCHAR(150) = NULL,
    @SourceDepartmentID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @ApplyYear IS NULL OR @ApplyYear <= 0
        SET @ApplyYear = YEAR(GETDATE());

    -- 1. Xác định User có được phân quyền quản lý đơn vị không
    DECLARE @HasManagedDepts BIT = 0;
    IF EXISTS (
        SELECT 1 FROM dbo.Sys_UserBoPhan subp 
        WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
    )
    BEGIN
        SET @HasManagedDepts = 1;
    END

    -- 2. Bảng nhân sự được phép xem (Từ Sys_User_ByManager)
    DECLARE @UserTable TABLE (ID INT, UserName VARCHAR(150));
    INSERT INTO @UserTable (ID, UserName)
    EXEC dbo.Sys_User_ByManager @UserName;

    -- 3. Bảng chứa danh sách Đơn vị Cấp 1 mà User quản lý
    DECLARE @ManagedLevel1Depts TABLE (BoPhan_ID INT NOT NULL PRIMARY KEY);
    IF @HasManagedDepts = 1
    BEGIN
        -- Nếu quản lý đơn vị gốc 5741 (KHA000000) -> Quản lý tất cả 28 đơn vị cấp 1
        IF EXISTS (
            SELECT 1 FROM dbo.Sys_UserBoPhan subp
            INNER JOIN dbo.MN_BoPhan bp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND (bp.BoPhan_ID = 5741 OR bp.MaBoPhan = 'KHA000000')
        )
        BEGIN
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT BoPhan_ID 
            FROM dbo.MN_BoPhan 
            WHERE BoPhanCha_ID = 5741 AND Da_Xoa = 0;
        END
        ELSE
        BEGIN
            -- Trực tiếp là đơn vị Cấp 1
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT DISTINCT bp.BoPhan_ID
            FROM dbo.MN_BoPhan bp
            INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND bp.BoPhanCha_ID = 5741 AND bp.Da_Xoa = 0;

            -- Nếu được phân quyền ở bộ phận con, truy ngược lên đơn vị cấp 1
            ;WITH ParentTree AS (
                SELECT bp.BoPhan_ID, bp.BoPhanCha_ID
                FROM dbo.MN_BoPhan bp
                INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
                WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
                  AND bp.Da_Xoa = 0
                UNION ALL
                SELECT p.BoPhan_ID, p.BoPhanCha_ID
                FROM dbo.MN_BoPhan p
                INNER JOIN ParentTree c ON p.BoPhan_ID = c.BoPhanCha_ID
                WHERE p.Da_Xoa = 0 AND p.BoPhan_ID <> 5741
            )
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT DISTINCT BoPhan_ID
            FROM ParentTree
            WHERE BoPhanCha_ID = 5741
              AND BoPhan_ID NOT IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts);
        END
    END

    SELECT 
        st.StatusID,
        st.StatusCode,
        st.StatusName,
        st.BusinessType,
        st.SortOrder,
        COUNT(ds.DigitalSalesID) AS TotalCount,
        CAST(ISNULL(SUM(prod.ExpectedRevenue), 0) AS DECIMAL(18, 2)) AS TotalExpectedRevenue,
        CAST(ISNULL(SUM(contr.ActualRevenue), 0) AS DECIMAL(18, 2)) AS TotalActualRevenue
    FROM dbo.RM_DigitalSalesStatus st
    LEFT JOIN dbo.RM_DigitalSales ds ON ds.StatusID = st.StatusID 
        AND ds.IsDeleted = 0 
        AND ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) = @ApplyYear 
        --------------------------------------------------------
        -- ÁP DỤNG ĐÚNG 3 RULES PHÂN QUYỀN
        --------------------------------------------------------
        AND (
            -- Rule 1 & Rule 3: Người tạo hoặc có thành viên thuộc @UserTable tham gia
            ds.CreatedBy = @UserName 
            OR ds.AssignedEmployeeID IN (SELECT ID FROM @UserTable) 
            OR EXISTS (
                SELECT 1 FROM dbo.RM_DigitalSalesMember m 
                WHERE m.DigitalSalesID = ds.DigitalSalesID 
                  AND ISNULL(m.IsActive, 0) = 1 
                  AND m.UserID IN (SELECT ID FROM @UserTable)
            )
            -- Rule 2: Có phân quyền quản lý -> Xuất phát từ đơn vị mình quản lý
            OR (
                @HasManagedDepts = 1 
                AND ds.SourceDepartmentID IS NOT NULL 
                AND ds.SourceDepartmentID IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts)
            )
        )
        --------------------------------------------------------
        -- LỌC THEO ĐƠN VỊ XUẤT PHÁT (NẾU CÓ TRUYỀN)
        --------------------------------------------------------
        AND (
            @SourceDepartmentID = 0
            OR (@SourceDepartmentID = -1 AND ds.SourceDepartmentID IS NULL)
            OR (@SourceDepartmentID > 0 AND ds.SourceDepartmentID = @SourceDepartmentID)
        )
    LEFT JOIN (
        SELECT dsp.DigitalSalesID, SUM(dsp.ExpectedRevenue) AS ExpectedRevenue
        FROM dbo.RM_DigitalSalesProduct dsp
        WHERE dsp.IsDeleted = 0
        GROUP BY dsp.DigitalSalesID
    ) prod ON prod.DigitalSalesID = ds.DigitalSalesID
    LEFT JOIN (
        SELECT dsp.DigitalSalesID, 
               SUM(ISNULL(CASE WHEN ISNULL(c.TotalAmount, 0) > 0 THEN c.TotalAmount ELSE c.ContractValue END, 0)) * 1000000 AS ActualRevenue
        FROM dbo.RM_DigitalSalesProductContract pc
        INNER JOIN dbo.RM_DigitalSalesProduct dsp ON dsp.SalesProductID = pc.SalesProductID
        INNER JOIN dbo.RM_Contracts c ON c.ContractID = pc.ContractID
        WHERE pc.IsDeleted = 0 AND ISNULL(c.IsDeleted, 0) = 0 AND dsp.IsDeleted = 0
        GROUP BY dsp.DigitalSalesID
    ) contr ON contr.DigitalSalesID = ds.DigitalSalesID
    WHERE ISNULL(st.IsDeleted, 0) = 0 AND ISNULL(st.IsActive, 1) = 1
    GROUP BY st.StatusID, st.StatusCode, st.StatusName, st.BusinessType, st.SortOrder
    ORDER BY st.BusinessType, st.SortOrder;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_GetList') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_GetList AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE [dbo].[RM_DigitalSales_GetList]
    @Keyword NVARCHAR(255) = NULL,
    @BusinessType TINYINT = 0,
    @StatusID INT = 0,
    @CustomerID INT = 0,
    @ProductServiceID INT = 0,
    @DepartmentID INT = 0,
    @EmployeeID INT = 0,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @UserName VARCHAR(150) = NULL,
    @IsKeyProject BIT = NULL,
    @IsFollowed BIT = NULL,
    @StatusIDs VARCHAR(500) = NULL,
    @ApplyYear INT = NULL,
    @MemberRole NVARCHAR(100) = NULL,
    @SourceDepartmentID INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    ------------------------------------------------------------
    -- 1. Phạm vi nhân sự được phép xem
    ------------------------------------------------------------
    DECLARE @UserTable TABLE
    (
        ID INT,
        UserName VARCHAR(150)
    );
    INSERT INTO @UserTable (ID, UserName)
    EXEC dbo.Sys_User_ByManager @UserName;

    ------------------------------------------------------------
    -- 1b. Danh sách Đơn vị Cấp 1 mà User quản lý (theo Sys_UserBoPhan)
    ------------------------------------------------------------
    DECLARE @HasManagedDepts BIT = 0;
    IF EXISTS (
        SELECT 1 FROM dbo.Sys_UserBoPhan subp 
        WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
    )
    BEGIN
        SET @HasManagedDepts = 1;
    END

    DECLARE @ManagedLevel1Depts TABLE (BoPhan_ID INT NOT NULL PRIMARY KEY);
    IF @HasManagedDepts = 1
    BEGIN
        -- Nếu quản lý đơn vị gốc 5741 (KHA000000) -> Quản lý tất cả 28 đơn vị cấp 1
        IF EXISTS (
            SELECT 1 FROM dbo.Sys_UserBoPhan subp
            INNER JOIN dbo.MN_BoPhan bp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND (bp.BoPhan_ID = 5741 OR bp.MaBoPhan = 'KHA000000')
        )
        BEGIN
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT BoPhan_ID 
            FROM dbo.MN_BoPhan 
            WHERE BoPhanCha_ID = 5741 AND Da_Xoa = 0;
        END
        ELSE
        BEGIN
            -- Trực tiếp là đơn vị Cấp 1
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT DISTINCT bp.BoPhan_ID
            FROM dbo.MN_BoPhan bp
            INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND bp.BoPhanCha_ID = 5741 AND bp.Da_Xoa = 0;

            -- Nếu được phân quyền ở bộ phận con, truy ngược lên đơn vị cấp 1
            ;WITH ParentTree AS (
                SELECT bp.BoPhan_ID, bp.BoPhanCha_ID
                FROM dbo.MN_BoPhan bp
                INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
                WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
                  AND bp.Da_Xoa = 0
                UNION ALL
                SELECT p.BoPhan_ID, p.BoPhanCha_ID
                FROM dbo.MN_BoPhan p
                INNER JOIN ParentTree c ON p.BoPhan_ID = c.BoPhanCha_ID
                WHERE p.Da_Xoa = 0 AND p.BoPhan_ID <> 5741
            )
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT DISTINCT BoPhan_ID
            FROM ParentTree
            WHERE BoPhanCha_ID = 5741
              AND BoPhan_ID NOT IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts);
        END
    END
    ------------------------------------------------------------
    -- 2. Cây phòng ban
    ------------------------------------------------------------
    DECLARE @DepartmentTree TABLE
    (
        BoPhan_ID INT NOT NULL PRIMARY KEY,
        MaBoPhan NVARCHAR(255) NULL
    );
    IF ISNULL(@DepartmentID, 0) <> 0
    BEGIN
        ;WITH DepartmentTree AS
        (
            SELECT bp.BoPhan_ID, bp.MaBoPhan
            FROM dbo.MN_BoPhan bp
            WHERE bp.BoPhan_ID = @DepartmentID
            UNION ALL
            SELECT child.BoPhan_ID, child.MaBoPhan
            FROM dbo.MN_BoPhan child
            INNER JOIN DepartmentTree parent ON child.BoPhanCha_ID = parent.BoPhan_ID
        )
        INSERT INTO @DepartmentTree
        (
            BoPhan_ID,
            MaBoPhan
        )
        SELECT BoPhan_ID, MaBoPhan
        FROM DepartmentTree;
    END;
    ------------------------------------------------------------
    -- 3. Chuẩn hóa tham số
    ------------------------------------------------------------
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    SET @Keyword = NULLIF(LTRIM(RTRIM(@Keyword)), '');
    SET @MemberRole = NULLIF(LTRIM(RTRIM(@MemberRole)), '');
    DECLARE @TotalCount INT = 0;
    ------------------------------------------------------------
    -- 4. Tổng số bản ghi
    ------------------------------------------------------------
    SELECT @TotalCount = COUNT(1)
    FROM dbo.RM_DigitalSales ds
    LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
    WHERE ds.IsDeleted = 0
      ----------------------------------------------------------
      -- Phân quyền dữ liệu theo 3 Rules
      ----------------------------------------------------------
      AND
      (
            -- Rule 1 & Rule 3: Người tạo hoặc có thành viên thuộc @UserTable tham gia
            ds.CreatedBy = @UserName
            OR ds.AssignedEmployeeID IN (SELECT ID FROM @UserTable)
            OR EXISTS
            (
                SELECT 1
                FROM dbo.RM_DigitalSalesMember member
                WHERE member.DigitalSalesID = ds.DigitalSalesID
                  AND ISNULL(member.IsActive, 0) = 1
                  AND member.UserID IN (SELECT ID FROM @UserTable)
            )
            -- Rule 2: Có phân quyền quản lý -> Xuất phát từ đơn vị mình quản lý
            OR (
                @HasManagedDepts = 1
                AND ds.SourceDepartmentID IS NOT NULL
                AND ds.SourceDepartmentID IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts)
            )
      )
      ----------------------------------------------------------
      -- Lọc theo Đơn vị xuất phát
      ----------------------------------------------------------
      AND
      (
            @SourceDepartmentID = 0
            OR (@SourceDepartmentID = -1 AND ds.SourceDepartmentID IS NULL)
            OR (@SourceDepartmentID > 0 AND ds.SourceDepartmentID = @SourceDepartmentID)
      )
      ----------------------------------------------------------
      -- Loại nghiệp vụ
      ----------------------------------------------------------
      AND
      (
          @BusinessType = 0
          OR ds.BusinessType = @BusinessType
      )
      ----------------------------------------------------------
      -- Trạng thái
      ----------------------------------------------------------
      AND
      (
            (
                @StatusIDs IS NULL
                AND
                (
                    @StatusID = 0
                    OR ds.StatusID = @StatusID
                )
            )
            OR
            (
                @StatusIDs IS NOT NULL
                AND
                (
                    ',' + @StatusIDs + ','
                    LIKE '%,' + CAST(ds.StatusID AS VARCHAR(20)) + ',%'
                )
            )
      )
      ----------------------------------------------------------
      -- Khách hàng
      ----------------------------------------------------------
      AND
      (
          @CustomerID = 0
          OR ds.CustomerID = @CustomerID
      )
      ----------------------------------------------------------
      -- Phòng ban
      ----------------------------------------------------------
      AND
      (
            @DepartmentID = 0
            OR EXISTS
            (
                SELECT 1
                FROM dbo.RM_DigitalSalesMember member
                INNER JOIN dbo.Sys_Users userDepartment ON userDepartment.UserId = member.UserID
                WHERE member.DigitalSalesID = ds.DigitalSalesID
                  AND ISNULL(member.IsActive, 0) = 1
                  AND userDepartment.MaBoPhan IN
                  (
                      SELECT MaBoPhan
                      FROM @DepartmentTree
                  )
            )
      )
      ----------------------------------------------------------
      -- Nhân viên phụ trách
      ----------------------------------------------------------
            AND
      (
          (@EmployeeID = 0 AND @MemberRole IS NULL)
          OR (@EmployeeID > 0 AND @MemberRole IS NULL AND
              (
                  ds.AssignedEmployeeID = @EmployeeID
                  OR EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND member.UserID = @EmployeeID
                        AND ISNULL(member.IsActive, 0) = 1
                  )
              )
          )
          OR (@EmployeeID > 0 AND @MemberRole IS NOT NULL AND
              (
                  EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND member.UserID = @EmployeeID
                        AND ISNULL(member.IsActive, 0) = 1
                        AND (member.RoleTitle = @MemberRole OR member.RoleTitle LIKE N'%' + @MemberRole + N'%' OR @MemberRole LIKE N'%' + member.RoleTitle + N'%')
                  )
                  OR
                  (
                      ds.AssignedEmployeeID = @EmployeeID
                      AND (@MemberRole LIKE N'%AM%' OR @MemberRole LIKE N'%Account Manager%' OR @MemberRole LIKE N'%Ch' + NCHAR(0x1EE7) + N' tr' + NCHAR(0x00EC) + N'%')
                  )
              )
          )
          OR (@EmployeeID = 0 AND @MemberRole IS NOT NULL AND
              (
                  EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND ISNULL(member.IsActive, 0) = 1
                        AND (member.RoleTitle = @MemberRole OR member.RoleTitle LIKE N'%' + @MemberRole + N'%' OR @MemberRole LIKE N'%' + member.RoleTitle + N'%')
                  )
                  OR
                  (
                      ISNULL(ds.AssignedEmployeeID, 0) > 0
                      AND (@MemberRole LIKE N'%AM%' OR @MemberRole LIKE N'%Account Manager%' OR @MemberRole LIKE N'%Ch' + NCHAR(0x1EE7) + N' tr' + NCHAR(0x00EC) + N'%')
                  )
              )
          )
      )
      ----------------------------------------------------------
      -- Khoảng ngày
      ----------------------------------------------------------
      AND
      (
          @FromDate IS NULL
          OR ds.CreatedDate >= @FromDate
      )
      AND
      (
          @ToDate IS NULL
   OR ds.CreatedDate <= DATEADD(DAY, 1, @ToDate)
      )
      ----------------------------------------------------------
      -- Dự án trọng điểm
      ----------------------------------------------------------
      AND
      (
          @IsKeyProject IS NULL
          OR @IsKeyProject = 0
          OR ds.IsKeyProject = 1
      )
      ----------------------------------------------------------
      -- Đang theo dõi
      ----------------------------------------------------------
      AND
      (
          @IsFollowed IS NULL
          OR @IsFollowed = 0
          OR
          (
                (
                    @UserName IS NOT NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM dbo.RM_DigitalSalesFollow f
                        WHERE f.DigitalSalesID = ds.DigitalSalesID AND f.UserName = @UserName
                    )
                )
                OR
                (
                    @UserName IS NULL
                    AND EXISTS
                    (
                        SELECT 1
                        FROM dbo.RM_DigitalSalesFollow f
                        WHERE f.DigitalSalesID = ds.DigitalSalesID
                    )
                )
          )
      )
      ----------------------------------------------------------
      -- Sản phẩm dịch vụ
      ----------------------------------------------------------
      AND
      (
          @ProductServiceID = 0
          OR EXISTS
          (
              SELECT 1
              FROM dbo.RM_DigitalSalesProduct dsp
              WHERE dsp.DigitalSalesID = ds.DigitalSalesID
                AND dsp.ProductServiceID = @ProductServiceID
                AND dsp.IsDeleted = 0
          )
      )
      ----------------------------------------------------------
      -- Năm áp dụng
      ----------------------------------------------------------
      AND
      (
          @ApplyYear IS NULL
          OR @ApplyYear = 0
          OR ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) = @ApplyYear
      )
      ----------------------------------------------------------
      -- Từ khóa
      ----------------------------------------------------------
      AND
      (
            @Keyword IS NULL
            OR ds.Code LIKE '%' + @Keyword + '%'
            OR ds.Title LIKE N'%' + @Keyword + '%'
            OR c.CustomerName LIKE N'%' + @Keyword + '%'
            OR EXISTS
            (
                SELECT 1
                FROM dbo.RM_DigitalSalesProduct dsp
                INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID
                WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND ps.NameProduct LIKE N'%' + @Keyword + '%'
            )
      );
    ------------------------------------------------------------
    -- 5. Lấy ID theo trang
    --
    -- QUAN TRỌNG:
    -- TotalExpectedRevenue được SUM trực tiếp từ
    -- RM_DigitalSalesProduct.ExpectedRevenue.
    --
    -- ORDER BY chính giá trị này.
    ------------------------------------------------------------
    ;WITH PagedIDs AS
    (
        SELECT ds.DigitalSalesID
        FROM dbo.RM_DigitalSales ds
        LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
        --------------------------------------------------------
        -- Tính tổng doanh thu dự kiến đúng theo dữ liệu hiển thị
        --------------------------------------------------------
        OUTER APPLY
        (
            SELECT
                CAST(
                    ISNULL(SUM(dsp.ExpectedRevenue), 0)
                    AS DECIMAL(18, 2)
                ) AS TotalExpectedRevenue
            FROM dbo.RM_DigitalSalesProduct dsp
            WHERE dsp.DigitalSalesID = ds.DigitalSalesID
              AND dsp.IsDeleted = 0
        ) Revenue
        WHERE ds.IsDeleted = 0
          ------------------------------------------------------
          -- Phân quyền dữ liệu theo 3 Rules
          ------------------------------------------------------
          AND
          (
                -- Rule 1 & Rule 3: Người tạo hoặc có thành viên thuộc @UserTable tham gia
                ds.CreatedBy = @UserName
                OR ds.AssignedEmployeeID IN (SELECT ID FROM @UserTable)
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesMember member
                    WHERE member.DigitalSalesID = ds.DigitalSalesID
                      AND ISNULL(member.IsActive, 0) = 1
                      AND member.UserID IN (SELECT ID FROM @UserTable)
                )
                -- Rule 2: Có phân quyền quản lý -> Xuất phát từ đơn vị mình quản lý
                OR (
                    @HasManagedDepts = 1
                    AND ds.SourceDepartmentID IS NOT NULL
                    AND ds.SourceDepartmentID IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts)
                )
          )
          ------------------------------------------------------
          -- Lọc theo Đơn vị xuất phát
          ------------------------------------------------------
          AND
          (
                @SourceDepartmentID = 0
                OR (@SourceDepartmentID = -1 AND ds.SourceDepartmentID IS NULL)
                OR (@SourceDepartmentID > 0 AND ds.SourceDepartmentID = @SourceDepartmentID)
          )
          ------------------------------------------------------
          -- Loại nghiệp vụ
          ------------------------------------------------------
          AND
          (
              @BusinessType = 0
              OR ds.BusinessType = @BusinessType
          )
          ------------------------------------------------------
          -- Trạng thái
          ------------------------------------------------------
          AND
          (
                (
                    @StatusIDs IS NULL
                    AND
                    (
                        @StatusID = 0
                        OR ds.StatusID = @StatusID
                    )
                )
                OR
                (
                    @StatusIDs IS NOT NULL
                    AND
                    (
                        ',' + @StatusIDs + ','
                        LIKE '%,' + CAST(ds.StatusID AS VARCHAR(20)) + ',%'
                    )
                )
          )
          ------------------------------------------------------
          -- Khách hàng
          ------------------------------------------------------
          AND
          (
              @CustomerID = 0
              OR ds.CustomerID = @CustomerID
          )
          ------------------------------------------------------
          -- Phòng ban
          ------------------------------------------------------
          AND
          (
                @DepartmentID = 0
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesMember member
                    INNER JOIN dbo.Sys_Users userDepartment ON userDepartment.UserId = member.UserID
                    WHERE member.DigitalSalesID = ds.DigitalSalesID
                      AND ISNULL(member.IsActive, 0) = 1
                      AND userDepartment.MaBoPhan IN (SELECT MaBoPhan FROM @DepartmentTree)
                )
          )
          ------------------------------------------------------
          -- Nhân viên
          ------------------------------------------------------
                AND
      (
          (@EmployeeID = 0 AND @MemberRole IS NULL)
          OR (@EmployeeID > 0 AND @MemberRole IS NULL AND
              (
                  ds.AssignedEmployeeID = @EmployeeID
                  OR EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND member.UserID = @EmployeeID
                        AND ISNULL(member.IsActive, 0) = 1
                  )
              )
          )
          OR (@EmployeeID > 0 AND @MemberRole IS NOT NULL AND
              (
                  EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND member.UserID = @EmployeeID
                        AND ISNULL(member.IsActive, 0) = 1
                        AND (member.RoleTitle = @MemberRole OR member.RoleTitle LIKE N'%' + @MemberRole + N'%' OR @MemberRole LIKE N'%' + member.RoleTitle + N'%')
                  )
                  OR
                  (
                      ds.AssignedEmployeeID = @EmployeeID
                      AND (@MemberRole LIKE N'%AM%' OR @MemberRole LIKE N'%Account Manager%' OR @MemberRole LIKE N'%Ch' + NCHAR(0x1EE7) + N' tr' + NCHAR(0x00EC) + N'%')
                  )
              )
          )
          OR (@EmployeeID = 0 AND @MemberRole IS NOT NULL AND
              (
                  EXISTS
                  (
                      SELECT 1
                      FROM dbo.RM_DigitalSalesMember member
                      WHERE member.DigitalSalesID = ds.DigitalSalesID
                        AND ISNULL(member.IsActive, 0) = 1
                        AND (member.RoleTitle = @MemberRole OR member.RoleTitle LIKE N'%' + @MemberRole + N'%' OR @MemberRole LIKE N'%' + member.RoleTitle + N'%')
                  )
                  OR
                  (
                      ISNULL(ds.AssignedEmployeeID, 0) > 0
                      AND (@MemberRole LIKE N'%AM%' OR @MemberRole LIKE N'%Account Manager%' OR @MemberRole LIKE N'%Ch' + NCHAR(0x1EE7) + N' tr' + NCHAR(0x00EC) + N'%')
                  )
              )
          )
      )
          ------------------------------------------------------
          -- Khoảng ngày
          ------------------------------------------------------
          AND
          (
              @FromDate IS NULL
              OR ds.CreatedDate >= @FromDate
          )
          AND
          (
              @ToDate IS NULL
              OR ds.CreatedDate <= DATEADD(DAY, 1, @ToDate)
          )
          ------------------------------------------------------
          -- Dự án trọng điểm
          ------------------------------------------------------
          AND
          (
              @IsKeyProject IS NULL
              OR @IsKeyProject = 0
              OR ds.IsKeyProject = 1
          )
          ------------------------------------------------------
          -- Theo dõi
          ------------------------------------------------------
          AND
          (
              @IsFollowed IS NULL
              OR @IsFollowed = 0
              OR
              (
                    (
                        @UserName IS NOT NULL
                        AND EXISTS
                        (
                            SELECT 1
                            FROM dbo.RM_DigitalSalesFollow f
                            WHERE f.DigitalSalesID = ds.DigitalSalesID AND f.UserName = @UserName
                        )
                    )
                    OR
                    (
                        @UserName IS NULL
                        AND EXISTS
                        (
                            SELECT 1
                            FROM dbo.RM_DigitalSalesFollow f
                            WHERE f.DigitalSalesID = ds.DigitalSalesID
                        )
                    )
              )
          )
          ------------------------------------------------------
          -- Sản phẩm
          ------------------------------------------------------
          AND
          (
              @ProductServiceID = 0

              OR EXISTS
              (
                  SELECT 1
                  FROM dbo.RM_DigitalSalesProduct dsp
                  WHERE dsp.DigitalSalesID = ds.DigitalSalesID
                    AND dsp.ProductServiceID = @ProductServiceID
                    AND dsp.IsDeleted = 0
              )
          )
          ------------------------------------------------------
          -- Năm
          ------------------------------------------------------
          AND
          (
              @ApplyYear IS NULL
              OR @ApplyYear = 0
              OR ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) = @ApplyYear
          )
          ------------------------------------------------------
          -- Từ khóa
          ------------------------------------------------------
          AND
          (
                @Keyword IS NULL
                OR ds.Code LIKE '%' + @Keyword + '%'
                OR ds.Title LIKE N'%' + @Keyword + '%'
                OR c.CustomerName LIKE N'%' + @Keyword + '%'
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesProduct dsp
                    INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID
                    WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND ps.NameProduct LIKE N'%' + @Keyword + '%'
                )
          )
        --------------------------------------------------------
        -- SẮP XẾP THEO TỔNG DOANH THU DỰ KIẾN GIẢM DẦN
        --------------------------------------------------------
        ORDER BY
            Revenue.TotalExpectedRevenue DESC,
            ISNULL(
                ds.ActionTime,
                ISNULL(ds.LastModifiedDate, ds.CreatedDate)
            ) DESC,
            ds.DigitalSalesID DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY
    )
    ------------------------------------------------------------
    -- 6. Lấy dữ liệu chi tiết
    ------------------------------------------------------------
    SELECT
        ds.DigitalSalesID,
        ds.Code,
        ds.Title,
        ds.BusinessType,
        CASE ds.BusinessType
            WHEN 1 THEN N'Cơ hội'
            WHEN 2 THEN N'Dự án'
            ELSE N'Khác'
        END AS BusinessTypeName,
        ds.StatusID,
        st.StatusCode,
        st.StatusName,
        ds.CustomerID,
        c.CustomerName,
        ds.ContactPerson_ID,
        cp.FullName AS ContactPersonName,
        Revenue.TotalExpectedRevenue,
        --------------------------------------------------------
        -- Tổng doanh thu thực tế
        --------------------------------------------------------
        CAST(
            COALESCE
            (
                (
                    SELECT
                        SUM
                        (
                            ISNULL
                            (
                                CASE
                                    WHEN ISNULL(ct.TotalAmount, 0) > 0
                                        THEN ct.TotalAmount
                                    ELSE ct.ContractValue
                                END,
                                0
                            )
                        ) * 1000000
                    FROM dbo.RM_DigitalSalesProductContract pc
                    INNER JOIN dbo.RM_DigitalSalesProduct dsp ON dsp.SalesProductID = pc.SalesProductID
                    INNER JOIN dbo.RM_Contracts ct ON ct.ContractID = pc.ContractID
                    WHERE dsp.DigitalSalesID = ds.DigitalSalesID
                      AND pc.IsDeleted = 0
                      AND ISNULL(ct.IsDeleted, 0) = 0
                      AND dsp.IsDeleted = 0
                ),
                ds.ContractValue,
                0
            )
            AS DECIMAL(18, 2)
        ) AS TotalActualRevenue,
        ds.ClosingProbability,
        ds.ExpectedDate,
        ds.StartDate,
        ds.EndDate,
        ds.ContractID,
        ds.ContractNo,
        ds.ContractValue,
        ds.ContractSignDate,
        ds.AssignedEmployeeID,
        u.FullName AS AssignedEmployeeName,
        ds.DepartmentID,
        bp.TenBoPhan AS DepartmentName,
        ds.SourceDepartmentID,
        sbp.TenBoPhan AS SourceDepartmentName,
        ds.Note,
        ds.FileAttach,
        ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) AS ApplyYear,
        ISNULL(ds.IsKeyProject, 0) AS IsKeyProject,
        --------------------------------------------------------
        -- Theo dõi
        --------------------------------------------------------
        CASE
            WHEN @UserName IS NOT NULL
                 AND EXISTS
                 (
              SELECT 1
                     FROM dbo.RM_DigitalSalesFollow f
                     WHERE f.DigitalSalesID = ds.DigitalSalesID AND f.UserName = @UserName
                 )
            THEN CAST(1 AS BIT)

            ELSE CAST(0 AS BIT)
        END AS IsFollowed,
        ds.CreatedDate,
        ds.CreatedBy,
        ds.LastModifiedDate,
        ds.LastModifiedBy,
        ds.ActionTime,
        --------------------------------------------------------
        -- Danh sách sản phẩm
        --------------------------------------------------------
        STUFF
        (
            (
                SELECT  ', ' + ps.NameProduct
                FROM dbo.RM_DigitalSalesProduct dsp
                INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID
                WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'),
            1,
            2,
            ''
        ) AS ProductServiceNames,
        --------------------------------------------------------
        -- Số sản phẩm
        --------------------------------------------------------
        (
            SELECT COUNT(1)
            FROM dbo.RM_DigitalSalesProduct dsp
            WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
        ) AS ProductCount,
        --------------------------------------------------------
        -- Số thành viên
        --------------------------------------------------------
        (
            SELECT COUNT(1)
            FROM dbo.RM_DigitalSalesMember m
            WHERE m.DigitalSalesID = ds.DigitalSalesID AND m.IsActive = 1
        ) AS MemberCount,
        --------------------------------------------------------
        -- Tiến độ
        --------------------------------------------------------
        CAST
        (
            CASE
                WHEN
                (
                    SELECT COUNT(1)
                    FROM dbo.RM_DigitalSalesTracking t
                    WHERE t.DigitalSalesID = ds.DigitalSalesID
                ) = 0
                THEN 0
                ELSE
                (
                    CAST
                    (
                        (
                            SELECT COUNT(1)
                            FROM dbo.RM_DigitalSalesTracking t
                            WHERE t.DigitalSalesID = ds.DigitalSalesID AND t.Status = 3
                        ) AS FLOAT
                    )
                    /
                    CAST
                    (
                        (
                            SELECT COUNT(1)
                            FROM dbo.RM_DigitalSalesTracking t
                            WHERE t.DigitalSalesID = ds.DigitalSalesID
                        ) AS FLOAT
                    )
                ) * 100
            END
            AS INT
        ) AS ProgressPercentage,
        --------------------------------------------------------
        -- Có công việc quá hạn
        --------------------------------------------------------
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.RM_DigitalSalesTracking t
                WHERE t.DigitalSalesID = ds.DigitalSalesID
                  AND t.Status <> 3
                  AND t.Deadline < GETDATE()
            )
            THEN 1
            ELSE 0
        END AS HasOverdueTasks,
        --------------------------------------------------------
        -- Tổng số bản ghi
        --------------------------------------------------------
        @TotalCount AS TotalCount
    FROM PagedIDs p
    INNER JOIN dbo.RM_DigitalSales ds ON p.DigitalSalesID = ds.DigitalSalesID
    INNER JOIN dbo.RM_DigitalSalesStatus st ON ds.StatusID = st.StatusID
    LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
    LEFT JOIN dbo.RM_ContactPersons cp ON ds.ContactPerson_ID = cp.ContactPerson_ID
    LEFT JOIN dbo.Sys_Users u ON ds.AssignedEmployeeID = u.UserId
    OUTER APPLY (
        SELECT TOP 1 b.TenBoPhan 
        FROM dbo.MN_BoPhan b 
        WHERE b.MaBoPhan = u.MaBoPhan AND b.Da_Xoa = 0 
        ORDER BY b.BoPhan_ID DESC
    ) bp
    LEFT JOIN dbo.MN_BoPhan sbp ON ds.SourceDepartmentID = sbp.BoPhan_ID
    ------------------------------------------------------------
    -- Tính lại đúng cùng công thức dùng khi phân trang
    ------------------------------------------------------------
    OUTER APPLY
    (
        SELECT
            CAST(
                ISNULL(SUM(dsp.ExpectedRevenue), 0)
                AS DECIMAL(18, 2)
            ) AS TotalExpectedRevenue
        FROM dbo.RM_DigitalSalesProduct dsp
        WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
    ) Revenue
    ------------------------------------------------------------
    -- SẮP XẾP KẾT QUẢ CUỐI
    ------------------------------------------------------------
    ORDER BY
        Revenue.TotalExpectedRevenue DESC,
        ISNULL(
            ds.ActionTime,
            ISNULL(ds.LastModifiedDate, ds.CreatedDate)
        ) DESC,
        ds.DigitalSalesID DESC;
END
GO
