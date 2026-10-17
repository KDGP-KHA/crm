/*
========================================================================================
    HỆ THỐNG CENIT TOC CRM - PHÂN HỆ KINH DOANH SPDV SỐ (RM_DIGITALSALES)
    DASHBOARD NÂNG CAO: COMBOBOX ĐƠN VỊ QUẢN LÝ & BIỂU ĐỒ CƠ CẤU THEO ĐƠN VỊ (US-18)
    - SP dbo.MN_BoPhan_GetManagedLevel1Departments (Lọc đơn vị theo quyền quản lý của User)
    - SP dbo.RM_DigitalSales_GetDashboardDepartmentStats (Thống kê số lượng & doanh thu theo Đơn vị)
========================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT N'>>> 1. TẠO STORED PROCEDURE dbo.MN_BoPhan_GetManagedLevel1Departments...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.MN_BoPhan_GetManagedLevel1Departments') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.MN_BoPhan_GetManagedLevel1Departments AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.MN_BoPhan_GetManagedLevel1Departments
    @UserName VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Nếu không truyền UserName hoặc không tồn tại trong Sys_UserBoPhan -> User không quản lý đơn vị nào (Rule 1)
    IF @UserName IS NULL OR NOT EXISTS (
        SELECT 1 FROM dbo.Sys_UserBoPhan subp 
        WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
    )
    BEGIN
        SELECT 
            CAST(0 AS INT) AS BoPhan_ID, 
            CAST('' AS NVARCHAR(250)) AS TenBoPhan, 
            CAST('' AS VARCHAR(50)) AS MaBoPhan
        WHERE 1 = 0;
        RETURN;
    END

    -- 2. Nếu được cấu hình quản lý đơn vị gốc 5741 (KHA000000 - Cấp Tỉnh / Ban Giám đốc)
    --    hoặc KDGP (100003, các phòng ban con của KDGP 239.603)
    -- -> Quản lý toàn bộ các đơn vị con Cấp 1 của Viễn thông Khánh Hòa
    IF EXISTS (
        SELECT 1 FROM dbo.Sys_UserBoPhan subp
        INNER JOIN dbo.MN_BoPhan bp ON subp.MaBoPhan = bp.MaBoPhan
        WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
          AND (
              bp.BoPhan_ID = 5741 
              OR bp.MaBoPhan = 'KHA000000'
              OR bp.BoPhan_ID = 100003 
              OR bp.BoPhanCha_ID = 100003
              OR bp.MaBoPhan LIKE '239.603%'
          )
    )
    BEGIN
        SELECT 
            BoPhan_ID, 
            TenBoPhan, 
            MaBoPhan
        FROM dbo.MN_BoPhan
        WHERE BoPhanCha_ID = 5741 
          AND Da_Xoa = 0
        ORDER BY TenBoPhan;
        RETURN;
    END

    -- 3. Quản lý cụ thể một hoặc một số đơn vị cấp 1 (hoặc các phòng ban/tổ đội con thuộc cấp 1)
    DECLARE @ManagedLevel1Depts TABLE (BoPhan_ID INT NOT NULL PRIMARY KEY);

    -- 3.1. Các đơn vị con Cấp 1 trực tiếp
    INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
    SELECT DISTINCT bp.BoPhan_ID
    FROM dbo.MN_BoPhan bp
    INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
    WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
      AND bp.BoPhanCha_ID = 5741 
      AND bp.Da_Xoa = 0;

    -- 3.2. Nếu phân quyền ở bộ phận con (cấp 2, 3), truy ngược cây danh mục lên đơn vị con Cấp 1 của 5741
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

    -- Trả về danh sách đơn vị con cấp 1 mà người dùng được phân quyền quản lý
    SELECT 
        bp.BoPhan_ID, 
        bp.TenBoPhan, 
        bp.MaBoPhan
    FROM dbo.MN_BoPhan bp
    INNER JOIN @ManagedLevel1Depts m ON bp.BoPhan_ID = m.BoPhan_ID
    WHERE bp.Da_Xoa = 0
    ORDER BY bp.TenBoPhan;
END
GO

PRINT N'>>> 2. TẠO STORED PROCEDURE dbo.RM_DigitalSales_GetDashboardDepartmentStats (US-18)...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSales_GetDashboardDepartmentStats') AND type = N'P')
    EXEC('CREATE PROCEDURE dbo.RM_DigitalSales_GetDashboardDepartmentStats AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.RM_DigitalSales_GetDashboardDepartmentStats
    @ApplyYear INT = NULL,
    @UserName VARCHAR(150) = NULL
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
        IF EXISTS (
            SELECT 1 FROM dbo.Sys_UserBoPhan subp
            INNER JOIN dbo.MN_BoPhan bp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND (
                  bp.BoPhan_ID = 5741 
                  OR bp.MaBoPhan = 'KHA000000'
                  OR bp.BoPhan_ID = 100003 
                  OR bp.BoPhanCha_ID = 100003
                  OR bp.MaBoPhan LIKE '239.603%'
              )
        )
        BEGIN
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT BoPhan_ID 
            FROM dbo.MN_BoPhan 
            WHERE BoPhanCha_ID = 5741 AND Da_Xoa = 0;
        END
        ELSE
        BEGIN
            INSERT INTO @ManagedLevel1Depts (BoPhan_ID)
            SELECT DISTINCT bp.BoPhan_ID
            FROM dbo.MN_BoPhan bp
            INNER JOIN dbo.Sys_UserBoPhan subp ON subp.MaBoPhan = bp.MaBoPhan
            WHERE REPLACE(subp.Email, '@vnpt.vn', '') = REPLACE(@UserName, '@vnpt.vn', '')
              AND bp.BoPhanCha_ID = 5741 AND bp.Da_Xoa = 0;

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

    -- Thống kê nhóm theo Đơn vị xuất phát (tuân thủ 3 Rules phân quyền)
    SELECT 
        ISNULL(ds.SourceDepartmentID, 0) AS BoPhan_ID,
        ISNULL(bp.TenBoPhan, N'Chưa chọn đơn vị xuất phát') AS TenBoPhan,
        COUNT(ds.DigitalSalesID) AS TotalCount,
        CAST(ISNULL(SUM(prod.ExpectedRevenue), 0) AS DECIMAL(18, 2)) AS TotalExpectedRevenue,
        CAST(ISNULL(SUM(contr.ActualRevenue), 0) AS DECIMAL(18, 2)) AS TotalActualRevenue
    FROM dbo.RM_DigitalSales ds
    LEFT JOIN dbo.MN_BoPhan bp ON ds.SourceDepartmentID = bp.BoPhan_ID
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
    WHERE ds.IsDeleted = 0
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
    GROUP BY ISNULL(ds.SourceDepartmentID, 0), ISNULL(bp.TenBoPhan, N'Chưa chọn đơn vị xuất phát')
    ORDER BY TotalCount DESC;
END
GO

PRINT N'>>> 3. CẬP NHẬT STORED PROCEDURE dbo.RM_DigitalSales_GetStaleActionTime...';
GO

ALTER PROCEDURE [dbo].[RM_DigitalSales_GetStaleActionTime]
    @ApplyYear INT = NULL,
    @Hours INT = 72,
    @EmployeeID INT = 0,
    @UserName VARCHAR(150) = NULL,
    @SourceDepartmentID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserTable TABLE (ID INT, UserName VARCHAR(150));
    INSERT INTO @UserTable (ID, UserName)
    EXEC dbo.Sys_User_ByManager @UserName;

    DECLARE @ManagedLevel1Depts TABLE (
        BoPhan_ID INT PRIMARY KEY,
        TenBoPhan NVARCHAR(250),
        MaBoPhan VARCHAR(50)
    );
    INSERT INTO @ManagedLevel1Depts (BoPhan_ID, TenBoPhan, MaBoPhan)
    EXEC dbo.MN_BoPhan_GetManagedLevel1Departments @UserName;

    DECLARE @HasManagedDepts BIT = 0;
    IF EXISTS (SELECT 1 FROM @ManagedLevel1Depts)
        SET @HasManagedDepts = 1;

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
        CAST(ISNULL((
            SELECT SUM(dsp.ExpectedRevenue) 
            FROM dbo.RM_DigitalSalesProduct dsp 
            WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
        ), 0) AS DECIMAL(18, 2)) AS TotalExpectedRevenue,
        CAST(COALESCE(
            (
                SELECT SUM(ISNULL(CASE WHEN ISNULL(ct.TotalAmount, 0) > 0 THEN ct.TotalAmount ELSE ct.ContractValue END, 0)) * 1000000
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
        ) AS DECIMAL(18, 2)) AS TotalActualRevenue,
        ds.AssignedEmployeeID,
        u.FullName AS AssignedEmployeeName,
        ds.DepartmentID,
        bp.TenBoPhan AS DepartmentName,
        ds.SourceDepartmentID,
        bpSource.TenBoPhan AS SourceDepartmentName,
        ds.ActionTime,
        ds.LastModifiedDate,
        ds.CreatedDate,
        DATEDIFF(hour, COALESCE(ds.ActionTime, ds.LastModifiedDate, ds.CreatedDate), GETDATE()) AS HoursSinceUpdate,
        ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) AS ApplyYear
    FROM dbo.RM_DigitalSales ds
    INNER JOIN dbo.RM_DigitalSalesStatus st ON ds.StatusID = st.StatusID
    LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
    LEFT JOIN dbo.Sys_Users u ON ds.AssignedEmployeeID = u.UserId
    LEFT JOIN dbo.MN_BoPhan bp ON ds.DepartmentID = bp.BoPhan_ID
    LEFT JOIN dbo.MN_BoPhan bpSource ON ds.SourceDepartmentID = bpSource.BoPhan_ID
    WHERE ds.IsDeleted = 0
      AND (
          ds.CreatedBy = @UserName
          OR ds.AssignedEmployeeID IN (SELECT ID FROM @UserTable)
          OR EXISTS (
              SELECT 1 FROM dbo.RM_DigitalSalesMember m
              WHERE m.DigitalSalesID = ds.DigitalSalesID
                AND ISNULL(m.IsActive, 0) = 1
                AND m.UserID IN (SELECT ID FROM @UserTable)
          )
          OR (
              @HasManagedDepts = 1
              AND ds.SourceDepartmentID IS NOT NULL
              AND ds.SourceDepartmentID IN (SELECT BoPhan_ID FROM @ManagedLevel1Depts)
          )
      )
      AND (st.StatusCode IS NULL OR st.StatusCode NOT IN ('COMPLETED', 'LOST'))
      AND (@ApplyYear IS NULL OR @ApplyYear = 0 OR ISNULL(ds.ApplyYear, YEAR(ISNULL(ds.CreatedDate, GETDATE()))) = @ApplyYear)
      AND (@EmployeeID = 0 OR ds.AssignedEmployeeID = @EmployeeID)
      AND (
          @SourceDepartmentID IS NULL
          OR @SourceDepartmentID = 0
          OR (@SourceDepartmentID = -1 AND ds.SourceDepartmentID IS NULL)
          OR (@SourceDepartmentID > 0 AND ds.SourceDepartmentID = @SourceDepartmentID)
      )
      AND DATEDIFF(hour, COALESCE(ds.ActionTime, ds.LastModifiedDate, ds.CreatedDate), GETDATE()) >= @Hours
    ORDER BY DATEDIFF(hour, COALESCE(ds.ActionTime, ds.LastModifiedDate, ds.CreatedDate), GETDATE()) DESC, ds.DigitalSalesID DESC;
END
GO

PRINT N'>>> Đã tạo xong các SP cho Dashboard đơn vị quản lý.';
GO
