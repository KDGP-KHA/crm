-- ========================================================
-- KỊCH BẢN KHỞI TẠO CƠ SỞ DỮ LIỆU ĐẦY ĐỦ
-- PHÂN HỆ: KINH DOANH SẢN PHẨM DỊCH VỤ SỐ (RM_DigitalSales)
-- ========================================================

-- 1. BỔ SUNG CỘT CHO RM_DigitalSalesProgress NẾU CHƯA CÓ
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RM_DigitalSalesProgress' AND COLUMN_NAME = 'DefaultDurationDays'
)
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesProgress 
    ADD DefaultDurationDays INT NOT NULL CONSTRAINT DF_RM_DigitalSalesProgress_DefaultDurationDays DEFAULT (3);
END
GO

-- 2. CẬP NHẬT DANH MỤC TRẠNG THÁI (RM_DigitalSalesStatus)
IF EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesStatus WHERE StatusID = 4)
BEGIN
    UPDATE dbo.RM_DigitalSalesStatus
    SET 
        StatusName = N'Giai đoạn Hình thành dự án',
        Description = N'Nếu dự án thử nghiệm thì nằm trong này, PoC'
    WHERE StatusID = 4;
END
GO

IF EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesStatus WHERE StatusID = 5)
BEGIN
    UPDATE dbo.RM_DigitalSalesStatus
    SET 
        StatusName = N'Triển khai dự án',
        Description = N'Triển khai dự án. Nhập checklist tiến trình dự án.'
    WHERE StatusID = 5;
END
GO

IF EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesStatus WHERE StatusID = 7)
BEGIN
    UPDATE dbo.RM_DigitalSalesStatus
    SET 
        StatusName = N'Đã ký hợp đồng (đang thực hiện)',
        Description = N'Đã ký hợp đồng chính thức, đang thực hiện'
    WHERE StatusID = 7;
END
GO

-- 3. TẠO BẢNG THỰC THỂ GỐC: RM_DigitalSales
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RM_DigitalSales')
BEGIN
    CREATE TABLE dbo.RM_DigitalSales
    (
        DigitalSalesID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code VARCHAR(50) NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        BusinessType TINYINT NOT NULL,          -- 1: Cơ hội, 2: Dự án
        StatusID INT NOT NULL,                  -- FK: RM_DigitalSalesStatus
        CustomerID INT NOT NULL,                -- FK: RM_Customer
        ContactPerson_ID INT NULL,              -- FK: RM_ContactPersons
        TotalExpectedRevenue DECIMAL(18,2) NULL,-- Tổng doanh thu dự kiến
        TotalActualRevenue DECIMAL(18,2) NULL,  -- Tổng doanh thu thực tế (sau ký HĐ)
        ClosingProbability DECIMAL(5,2) NULL,
        ExpectedDate DATETIME NULL,
        StartDate DATETIME NULL,
        EndDate DATETIME NULL,
        ContractID INT NULL,
        ContractNo NVARCHAR(100) NULL,
        ContractValue DECIMAL(18,2) NULL,
        ContractSignDate DATETIME NULL,
        AssignedEmployeeID INT NULL,            -- FK: Sys_Users (AM - Người chủ trì)
        DepartmentID INT NULL,                  -- FK: MN_BoPhan
        Note NVARCHAR(MAX) NULL,
        FileAttach NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSales_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSales_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL
    );

    CREATE INDEX IX_RM_DigitalSales_StatusID ON dbo.RM_DigitalSales(StatusID);
    CREATE INDEX IX_RM_DigitalSales_CustomerID ON dbo.RM_DigitalSales(CustomerID);
    CREATE INDEX IX_RM_DigitalSales_BusinessType ON dbo.RM_DigitalSales(BusinessType);
    CREATE INDEX IX_RM_DigitalSales_IsDeleted ON dbo.RM_DigitalSales(IsDeleted);
END
GO

-- 4. TẠO BẢNG SẢN PHẨM / DỊCH VỤ SỐ ĐI KÈM: RM_DigitalSalesProduct
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RM_DigitalSalesProduct')
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesProduct
    (
        SalesProductID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DigitalSalesID INT NOT NULL,            -- FK: RM_DigitalSales
        ProductServiceID INT NOT NULL,          -- FK: RM_ProductService
        ExpectedRevenue DECIMAL(18,2) NULL,     -- Doanh thu dự kiến khi chào bán
        ActualRevenue DECIMAL(18,2) NULL,       -- Doanh thu thực tế sau khi ký HĐ
        PackageName NVARCHAR(255) NULL,         -- Gói cước / Quy mô
        Quantity INT NULL CONSTRAINT DF_RM_DigitalSalesProduct_Quantity DEFAULT (1),
        StartDate DATETIME NULL,
        EndDate DATETIME NULL,
        Note NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesProduct_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesProduct_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL
    );

    CREATE INDEX IX_RM_DigitalSalesProduct_SalesID ON dbo.RM_DigitalSalesProduct(DigitalSalesID);
    CREATE INDEX IX_RM_DigitalSalesProduct_ProductServiceID ON dbo.RM_DigitalSalesProduct(ProductServiceID);
END
GO

-- 5. TẠO BẢNG THÀNH VIÊN THAM GIA: RM_DigitalSalesMember
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RM_DigitalSalesMember')
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesMember
    (
        MemberID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DigitalSalesID INT NOT NULL,            -- FK: RM_DigitalSales
        UserID INT NOT NULL,                    -- FK: Sys_Users
        RoleTitle NVARCHAR(150) NULL,           -- AM, Chuyên viên giải pháp, Kỹ thuật triển khai...
        IsAM BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesMember_IsAM DEFAULT (0),
        Note NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesMember_IsActive DEFAULT (1),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesMember_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL
    );

    CREATE INDEX IX_RM_DigitalSalesMember_SalesID ON dbo.RM_DigitalSalesMember(DigitalSalesID);
    CREATE INDEX IX_RM_DigitalSalesMember_UserID ON dbo.RM_DigitalSalesMember(UserID);
END
GO

-- 6. TẠO HOẶC CẬP NHẬT BẢNG CHECKLIST / TIẾN TRÌNH THỰC THI: RM_DigitalSalesTracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RM_DigitalSalesTracking')
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesTracking
    (
        TrackingID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DigitalSalesID INT NOT NULL,            -- FK: RM_DigitalSales
        ProcessID INT NULL,                     -- FK: RM_DigitalSalesProcess
        ProgressID INT NULL,                    -- FK: RM_DigitalSalesProgress
        TaskName NVARCHAR(500) NULL,            -- Tên việc trong Checklist
        AssignedUserID INT NULL,                -- FK: Sys_Users
        StartDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_StartDate DEFAULT (GETDATE()),
        Deadline DATETIME NULL,                 -- Hạn chót hoàn thành
        CompletedDate DATETIME NULL,            -- Ngày hoàn thành thực tế
        Status TINYINT NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_Status DEFAULT (1), -- 1: Chưa làm, 2: Đang làm, 3: Hoàn thành, 4: Quá hạn
        ResultNote NVARCHAR(MAX) NULL,          -- Biên bản / Kết quả
        AttachmentFile NVARCHAR(500) NULL,      -- File đính kèm
        IsCustomTask BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_IsCustomTask DEFAULT (0),
        SortOrder INT NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_SortOrder DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL
    );

    CREATE INDEX IX_RM_DigitalSalesTracking_SalesID ON dbo.RM_DigitalSalesTracking(DigitalSalesID);
END
ELSE
BEGIN
    -- Đảm bảo có các cột mới cho Checklist
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'TaskName')
    BEGIN
        ALTER TABLE dbo.RM_DigitalSalesTracking ADD TaskName NVARCHAR(500) NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'IsCustomTask')
    BEGIN
        ALTER TABLE dbo.RM_DigitalSalesTracking ADD IsCustomTask BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_IsCustomTask DEFAULT (0);
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'SortOrder')
    BEGIN
        ALTER TABLE dbo.RM_DigitalSalesTracking ADD SortOrder INT NOT NULL CONSTRAINT DF_RM_DigitalSalesTracking_SortOrder DEFAULT (0);
    END

    -- Cho phép NULL ProcessID & ProgressID để tự nhập checklist
    ALTER TABLE dbo.RM_DigitalSalesTracking ALTER COLUMN ProcessID INT NULL;
    ALTER TABLE dbo.RM_DigitalSalesTracking ALTER COLUMN ProgressID INT NULL;
END
GO

-- 7. TẠO BẢNG TIMELINE 360 ĐỘ: RM_DigitalSalesTimeline
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RM_DigitalSalesTimeline')
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesTimeline
    (
        TimelineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DigitalSalesID INT NOT NULL,
        FromStatusID INT NULL,
        ToStatusID INT NOT NULL,
        FromBusinessType TINYINT NULL,
        ToBusinessType TINYINT NOT NULL,
        ActionDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesTimeline_ActionDate DEFAULT (GETDATE()),
        ActionBy VARCHAR(150) NOT NULL,
        Note NVARCHAR(MAX) NULL,
        AttachmentPath NVARCHAR(500) NULL
    );

    CREATE INDEX IX_RM_DigitalSalesTimeline_SalesID ON dbo.RM_DigitalSalesTimeline(DigitalSalesID);
END
GO

-- 8. SEEDING QUY TRÌNH & TIẾN TRÌNH MẪU ĐỘNG (KỂ CẢ POC VÀ TRIỂN KHAI)
IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProcess WHERE IsDeleted = 0)
BEGIN
    -- Trạng thái 1: Chưa nắm bắt (Cơ hội)
    INSERT INTO dbo.RM_DigitalSalesProcess (StatusID, ProcessCode, ProcessName, Description, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (1, 'KHAOSAT_BAN_DAU', N'Khảo sát nhu cầu ban đầu', N'Giai đoạn tiếp nhận thông tin sơ bộ', 1, 1, 0, GETDATE(), 'system');
    DECLARE @Proc1 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.RM_DigitalSalesProgress (ProcessID, ProgressCode, ProgressName, DefaultDurationDays, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES 
    (@Proc1, 'TIEP_NHAN_TT', N'Tiếp nhận & xác minh nhu cầu khách hàng', 2, 1, 1, 0, GETDATE(), 'system'),
    (@Proc1, 'PHAN_CONG_NS', N'Phân công nhân sự đầu mối tiếp cận (AM)', 1, 2, 1, 0, GETDATE(), 'system');

    -- Trạng thái 2: Đang tiếp cận (Cơ hội)
    INSERT INTO dbo.RM_DigitalSalesProcess (StatusID, ProcessCode, ProcessName, Description, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (2, 'TIEP_CAN_DEMO', N'Tiếp xúc, demo & lập phương án giải pháp', N'Quy trình làm việc trực tiếp với khách hàng', 2, 1, 0, GETDATE(), 'system');
    DECLARE @Proc2 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.RM_DigitalSalesProgress (ProcessID, ProgressCode, ProgressName, DefaultDurationDays, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES 
    (@Proc2, 'GAP_GO_DON_VI', N'Gặp gỡ trao đổi với lãnh đạo / phụ trách CNTT của đơn vị', 3, 1, 1, 0, GETDATE(), 'system'),
    (@Proc2, 'DEMO_GIAIPHAP', N'Tổ chức demo giới thiệu tính năng sản phẩm dịch vụ số', 5, 2, 1, 0, GETDATE(), 'system'),
    (@Proc2, 'BAO_GIA_DU_KIEN', N'Lập và gửi báo giá sơ bộ / dự kiến kinh phí', 3, 3, 1, 0, GETDATE(), 'system');

    -- Trạng thái 4: Giai đoạn Hình thành dự án (PoC & Thử nghiệm)
    INSERT INTO dbo.RM_DigitalSalesProcess (StatusID, ProcessCode, ProcessName, Description, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (4, 'HINH_THANH_POC', N'Thử nghiệm giải pháp & Đánh giá PoC', N'Dự án thử nghiệm, chạy thử PoC', 3, 1, 0, GETDATE(), 'system');
    DECLARE @Proc4 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.RM_DigitalSalesProgress (ProcessID, ProgressCode, ProgressName, DefaultDurationDays, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES 
    (@Proc4, 'LAP_PHUONG_AN_POC', N'Lập kịch bản & phương án thử nghiệm kỹ thuật (PoC)', 4, 1, 1, 0, GETDATE(), 'system'),
    (@Proc4, 'KHOI_TAO_MOI_TRUONG', N'Cấu hình hạ tầng máy chủ / Cloud thử nghiệm', 3, 2, 1, 0, GETDATE(), 'system'),
    (@Proc4, 'HUONG_DAN_SU_DUNG', N'Đào tạo người dùng & chạy thử nghiệm diện hẹp', 7, 3, 1, 0, GETDATE(), 'system'),
    (@Proc4, 'DANH_GIA_KET_QUA_POC', N'Lập biên bản đánh giá kết quả thử nghiệm PoC với khách hàng', 3, 4, 1, 0, GETDATE(), 'system');

    -- Trạng thái 5: Triển khai dự án (Checklist)
    INSERT INTO dbo.RM_DigitalSalesProcess (StatusID, ProcessCode, ProcessName, Description, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (5, 'TRIEN_KHAI_CHECKLIST', N'Quy trình triển khai dự án', N'Thực hiện các bước đầu thầu và triển khai', 4, 1, 0, GETDATE(), 'system');
    DECLARE @Proc5 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.RM_DigitalSalesProgress (ProcessID, ProgressCode, ProgressName, DefaultDurationDays, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES 
    (@Proc5, 'KHAO_SAT_CHI_TIET', N'Khảo sát chi tiết hiện trạng hạ tầng & quy trình nghiệp vụ', 5, 1, 1, 0, GETDATE(), 'system'),
    (@Proc5, 'LAP_HO_SO_DU_THAU', N'Xây dựng hồ sơ dự thầu / Phương án kỹ thuật chi tiết', 7, 2, 1, 0, GETDATE(), 'system'),
    (@Proc5, 'THAM_DINH_PHE_DUYET', N'Trình thẩm định & phê duyệt chủ trương đầu tư', 10, 3, 1, 0, GETDATE(), 'system');

    -- Trạng thái 7: Đã ký hợp đồng (đang thực hiện)
    INSERT INTO dbo.RM_DigitalSalesProcess (StatusID, ProcessCode, ProcessName, Description, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (7, 'THUC_HIEN_HOP_DONG', N'Thực hiện hợp đồng kinh tế', N'Triển khai bàn giao theo các điều khoản HĐ', 5, 1, 0, GETDATE(), 'system');
    DECLARE @Proc7 INT = SCOPE_IDENTITY();
    INSERT INTO dbo.RM_DigitalSalesProgress (ProcessID, ProgressCode, ProgressName, DefaultDurationDays, SortOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES 
    (@Proc7, 'CHOT_DOANH_THU_TT', N'Xác nhận và chốt doanh thu thực tế các sản phẩm dịch vụ', 3, 1, 1, 0, GETDATE(), 'system'),
    (@Proc7, 'BAN_GIAO_SAN_PHAM', N'Cài đặt chính thức, chuyển giao bản quyền & dữ liệu', 15, 2, 1, 0, GETDATE(), 'system');
END
GO

-- ========================================================
-- 9. STORED PROCEDURE: RM_DigitalSales_GetList
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_GetList', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_GetList;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_GetList
    @Keyword NVARCHAR(250) = NULL,
    @BusinessType TINYINT = 0,          -- 0: Tất cả, 1: Cơ hội, 2: Dự án
    @StatusID INT = 0,                 -- 0: Tất cả
    @CustomerID INT = 0,
    @ProductServiceID INT = 0,
    @DepartmentID INT = 0,
    @EmployeeID INT = 0,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @UserName VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 20;
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    ;WITH Filtered AS
    (
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
            ds.TotalExpectedRevenue,
            ds.TotalActualRevenue,
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
            ds.Note,
            ds.FileAttach,
            ds.CreatedDate,
            ds.CreatedBy,
            ds.LastModifiedDate,
            ds.LastModifiedBy,
            -- Danh sách tên sản phẩm dịch vụ nối chuỗi
            STUFF((
                SELECT ', ' + ps.NameProduct
                FROM dbo.RM_DigitalSalesProduct dsp
                INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID
                WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0
                FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ProductServiceNames,
            -- Số lượng sản phẩm
            (SELECT COUNT(1) FROM dbo.RM_DigitalSalesProduct dsp WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.IsDeleted = 0) AS ProductCount,
            -- Số lượng thành viên
            (SELECT COUNT(1) FROM dbo.RM_DigitalSalesMember m WHERE m.DigitalSalesID = ds.DigitalSalesID AND m.IsActive = 1) AS MemberCount,
            -- Tiến độ hoàn thành checklist / tiến trình (%)
            CAST(
                CASE 
                    WHEN (SELECT COUNT(1) FROM dbo.RM_DigitalSalesTracking t WHERE t.DigitalSalesID = ds.DigitalSalesID) = 0 THEN 0
                    ELSE (CAST((SELECT COUNT(1) FROM dbo.RM_DigitalSalesTracking t WHERE t.DigitalSalesID = ds.DigitalSalesID AND t.Status = 3) AS FLOAT) /
                          CAST((SELECT COUNT(1) FROM dbo.RM_DigitalSalesTracking t WHERE t.DigitalSalesID = ds.DigitalSalesID) AS FLOAT)) * 100
                END AS INT
            ) AS ProgressPercentage,
            -- Có việc bị quá hạn hay không
            CASE 
                WHEN EXISTS (
                    SELECT 1 FROM dbo.RM_DigitalSalesTracking t 
                    WHERE t.DigitalSalesID = ds.DigitalSalesID AND t.Status <> 3 AND t.Deadline < GETDATE()
                ) THEN 1 ELSE 0 
            END AS HasOverdueTasks
        FROM dbo.RM_DigitalSales ds
        INNER JOIN dbo.RM_DigitalSalesStatus st ON ds.StatusID = st.StatusID
        LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
        LEFT JOIN dbo.RM_ContactPersons cp ON ds.ContactPerson_ID = cp.ContactPerson_ID
        LEFT JOIN dbo.Sys_Users u ON ds.AssignedEmployeeID = u.UserId
        LEFT JOIN dbo.MN_BoPhan bp ON ds.DepartmentID = bp.BoPhan_ID
        WHERE ds.IsDeleted = 0
          AND (@BusinessType = 0 OR ds.BusinessType = @BusinessType)
          AND (@StatusID = 0 OR ds.StatusID = @StatusID)
          AND (@CustomerID = 0 OR ds.CustomerID = @CustomerID)
          AND (@DepartmentID = 0 OR ds.DepartmentID = @DepartmentID)
          AND (@EmployeeID = 0 OR ds.AssignedEmployeeID = @EmployeeID)
          AND (@FromDate IS NULL OR ds.CreatedDate >= @FromDate)
          AND (@ToDate IS NULL OR ds.CreatedDate <= DATEADD(day, 1, @ToDate))
          AND (
                @ProductServiceID = 0 
                OR EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct dsp WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND dsp.ProductServiceID = @ProductServiceID AND dsp.IsDeleted = 0)
              )
          AND (
                @Keyword IS NULL 
                OR LTRIM(RTRIM(@Keyword)) = '' 
                OR ds.Code LIKE '%' + @Keyword + '%' 
                OR ds.Title LIKE N'%' + @Keyword + '%'
                OR c.CustomerName LIKE N'%' + @Keyword + '%'
                OR EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct dsp INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID WHERE dsp.DigitalSalesID = ds.DigitalSalesID AND ps.NameProduct LIKE N'%' + @Keyword + '%')
              )
    ),
    CountTotal AS
    (
        SELECT COUNT(1) AS TotalCount FROM Filtered
    )
    SELECT 
        f.*,
        ct.TotalCount
    FROM Filtered f
    CROSS JOIN CountTotal ct
    ORDER BY f.CreatedDate DESC, f.DigitalSalesID DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ========================================================
-- 10. STORED PROCEDURE: RM_DigitalSales_GetByID
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_GetByID', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_GetByID;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_GetByID
    @DigitalSalesID INT
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
        ds.TotalExpectedRevenue,
        ds.TotalActualRevenue,
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
        ds.Note,
        ds.FileAttach,
        ds.CreatedDate,
        ds.CreatedBy,
        ds.LastModifiedDate,
        ds.LastModifiedBy
    FROM dbo.RM_DigitalSales ds
    INNER JOIN dbo.RM_DigitalSalesStatus st ON ds.StatusID = st.StatusID
    LEFT JOIN dbo.RM_Customer c ON ds.CustomerID = c.CustomerID
    LEFT JOIN dbo.RM_ContactPersons cp ON ds.ContactPerson_ID = cp.ContactPerson_ID
    LEFT JOIN dbo.Sys_Users u ON ds.AssignedEmployeeID = u.UserId
    LEFT JOIN dbo.MN_BoPhan bp ON ds.DepartmentID = bp.BoPhan_ID
    WHERE ds.DigitalSalesID = @DigitalSalesID AND ds.IsDeleted = 0;
END
GO

-- ========================================================
-- 11. STORED PROCEDURE: RM_DigitalSales_Save
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_Save;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_Save
    @DigitalSalesID INT,
    @Code VARCHAR(50) = NULL,
    @Title NVARCHAR(255),
    @BusinessType TINYINT = 1,          -- Mặc định 1: Cơ hội
    @StatusID INT = 1,                  -- Mặc định 1: Chưa nắm bắt
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
    @UserName VARCHAR(150)
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

        -- Xác định AM nếu chưa truyền: Lấy UserID từ Sys_Users dựa vào @UserName
        IF @AssignedEmployeeID IS NULL OR @AssignedEmployeeID <= 0
        BEGIN
            SELECT TOP 1 @AssignedEmployeeID = UserId FROM dbo.Sys_Users WHERE UserName = @UserName;
        END

        IF @DigitalSalesID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @DigitalSalesID)
        BEGIN
            INSERT INTO dbo.RM_DigitalSales
            (
                Code, Title, BusinessType, StatusID, CustomerID, ContactPerson_ID,
                TotalExpectedRevenue, TotalActualRevenue, ClosingProbability,
                ExpectedDate, StartDate, EndDate,
                ContractID, ContractNo, ContractValue, ContractSignDate,
                AssignedEmployeeID, DepartmentID, Note, FileAttach,
                IsDeleted, CreatedDate, CreatedBy
            )
            VALUES
            (
                @Code, @Title, ISNULL(@BusinessType, 1), ISNULL(@StatusID, 1), @CustomerID, @ContactPerson_ID,
                0, 0, @ClosingProbability,
                @ExpectedDate, @StartDate, @EndDate,
                @ContractID, @ContractNo, @ContractValue, @ContractSignDate,
                @AssignedEmployeeID, @DepartmentID, @Note, @FileAttach,
                0, GETDATE(), @UserName
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

            -- TỰ ĐỘNG THÊM AM VÀO BẢNG THÀNH VIÊN (RM_DigitalSalesMember)
            IF @AssignedEmployeeID IS NOT NULL AND @AssignedEmployeeID > 0
            BEGIN
                INSERT INTO dbo.RM_DigitalSalesMember (DigitalSalesID, UserID, RoleTitle, IsAM, Note, IsActive, CreatedDate, CreatedBy)
                VALUES (@DigitalSalesID, @AssignedEmployeeID, N'AM (Chủ trì kinh doanh)', 1, N'Người tạo hồ sơ cơ hội', 1, GETDATE(), @UserName);
            END

            -- Tự động sinh tiến trình theo quy trình của trạng thái ban đầu nếu có
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
                ContractSignDate = @ContractSignDate,
                AssignedEmployeeID = @AssignedEmployeeID,
                DepartmentID = @DepartmentID,
                Note = @Note,
                FileAttach = @FileAttach,
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

-- ========================================================
-- 12. STORED PROCEDURES: QUẢN LÝ SẢN PHẨM & DOANH THU (RM_DigitalSalesProduct)
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSalesProduct_GetBySalesID', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesProduct_GetBySalesID;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesProduct_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        dsp.SalesProductID,
        dsp.DigitalSalesID,
        dsp.ProductServiceID,
        ps.NameProduct AS ProductServiceName,
        ps.CodeProduct AS ProductServiceCode,
        dsp.ExpectedRevenue,
        dsp.ActualRevenue,
        dsp.PackageName,
        dsp.Quantity,
        dsp.StartDate,
        dsp.EndDate,
        dsp.Note,
        dsp.CreatedDate,
        dsp.CreatedBy
    FROM dbo.RM_DigitalSalesProduct dsp
    INNER JOIN dbo.RM_ProductService ps ON dsp.ProductServiceID = ps.ProductServiceID
    WHERE dsp.DigitalSalesID = @DigitalSalesID
      AND dsp.IsDeleted = 0
    ORDER BY dsp.SalesProductID ASC;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesProduct_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesProduct_Save;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesProduct_Save
    @SalesProductID INT,
    @DigitalSalesID INT,
    @ProductServiceID INT,
    @ExpectedRevenue DECIMAL(18,2) = NULL,
    @ActualRevenue DECIMAL(18,2) = NULL,
    @PackageName NVARCHAR(255) = NULL,
    @Quantity INT = 1,
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @Note NVARCHAR(MAX) = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF @SalesProductID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct WHERE SalesProductID = @SalesProductID)
        BEGIN
            INSERT INTO dbo.RM_DigitalSalesProduct
            (
                DigitalSalesID, ProductServiceID, ExpectedRevenue, ActualRevenue,
                PackageName, Quantity, StartDate, EndDate, Note,
                IsDeleted, CreatedDate, CreatedBy
            )
            VALUES
            (
                @DigitalSalesID, @ProductServiceID, @ExpectedRevenue, @ActualRevenue,
                @PackageName, CASE WHEN ISNULL(@Quantity, 0) <= 0 THEN 1 ELSE @Quantity END, @StartDate, @EndDate, @Note,
                0, GETDATE(), @UserName
            );
            SET @SalesProductID = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE dbo.RM_DigitalSalesProduct
            SET
                ProductServiceID = @ProductServiceID,
                ExpectedRevenue = @ExpectedRevenue,
                ActualRevenue = @ActualRevenue,
                PackageName = @PackageName,
                Quantity = CASE WHEN ISNULL(@Quantity, 0) <= 0 THEN 1 ELSE @Quantity END,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Note = @Note,
                LastModifiedDate = GETDATE(),
                LastModifiedBy = @UserName
            WHERE SalesProductID = @SalesProductID;
        END

        -- Tự động tổng hợp lại doanh thu cho RM_DigitalSales gốc
        UPDATE dbo.RM_DigitalSales
        SET
            TotalExpectedRevenue = (SELECT ISNULL(SUM(ExpectedRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            TotalActualRevenue = (SELECT ISNULL(SUM(ActualRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE DigitalSalesID = @DigitalSalesID;

        COMMIT TRANSACTION;
        SELECT @SalesProductID;
        RETURN @SalesProductID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesProduct_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesProduct_Delete;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesProduct_Delete
    @SalesProductID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DigitalSalesID INT;
    SELECT @DigitalSalesID = DigitalSalesID FROM dbo.RM_DigitalSalesProduct WHERE SalesProductID = @SalesProductID;

    IF @DigitalSalesID IS NULL 
    BEGIN
        SELECT 0;
        RETURN 0;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.RM_DigitalSalesProduct
        SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID;

        -- Tính lại tổng doanh thu
        UPDATE dbo.RM_DigitalSales
        SET
            TotalExpectedRevenue = (SELECT ISNULL(SUM(ExpectedRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            TotalActualRevenue = (SELECT ISNULL(SUM(ActualRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE DigitalSalesID = @DigitalSalesID;

        COMMIT TRANSACTION;
        SELECT 1;
        RETURN 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH
END
GO

-- ========================================================
-- 13. STORED PROCEDURES: THÀNH VIÊN THAM GIA (RM_DigitalSalesMember)
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSalesMember_GetBySalesID', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesMember_GetBySalesID;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesMember_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        m.MemberID,
        m.DigitalSalesID,
        m.UserID,
        u.FullName,
        u.UserName,
        u.Email,
        u.Phone,
        m.RoleTitle,
        m.IsAM,
        m.Note,
        m.IsActive,
        m.CreatedDate,
        m.CreatedBy
    FROM dbo.RM_DigitalSalesMember m
    INNER JOIN dbo.Sys_Users u ON m.UserID = u.UserId
    WHERE m.DigitalSalesID = @DigitalSalesID
      AND m.IsActive = 1
    ORDER BY m.IsAM DESC, m.MemberID ASC;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesMember_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesMember_Save;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesMember_Save
    @MemberID INT,
    @DigitalSalesID INT,
    @UserID INT,
    @RoleTitle NVARCHAR(150) = NULL,
    @IsAM BIT = 0,
    @Note NVARCHAR(500) = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF @MemberID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesMember WHERE MemberID = @MemberID)
    BEGIN
        IF @IsAM = 1
        BEGIN
            UPDATE dbo.RM_DigitalSalesMember SET IsAM = 0 WHERE DigitalSalesID = @DigitalSalesID;
            UPDATE dbo.RM_DigitalSales SET AssignedEmployeeID = @UserID WHERE DigitalSalesID = @DigitalSalesID;
        END

        INSERT INTO dbo.RM_DigitalSalesMember
        (DigitalSalesID, UserID, RoleTitle, IsAM, Note, IsActive, CreatedDate, CreatedBy)
        VALUES
        (@DigitalSalesID, @UserID, @RoleTitle, ISNULL(@IsAM, 0), @Note, 1, GETDATE(), @UserName);

        DECLARE @NewMemberID INT = SCOPE_IDENTITY();
        SELECT @NewMemberID;
        RETURN @NewMemberID;
    END
    ELSE
    BEGIN
        IF @IsAM = 1
        BEGIN
            UPDATE dbo.RM_DigitalSalesMember SET IsAM = 0 WHERE DigitalSalesID = @DigitalSalesID;
            UPDATE dbo.RM_DigitalSales SET AssignedEmployeeID = @UserID WHERE DigitalSalesID = @DigitalSalesID;
        END

        UPDATE dbo.RM_DigitalSalesMember
        SET 
            UserID = @UserID,
            RoleTitle = @RoleTitle,
            IsAM = ISNULL(@IsAM, 0),
            Note = @Note,
            IsActive = 1
        WHERE MemberID = @MemberID;

        SELECT @MemberID;
        RETURN @MemberID;
    END
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesMember_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesMember_Delete;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesMember_Delete
    @MemberID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RM_DigitalSalesMember
    SET IsActive = 0
    WHERE MemberID = @MemberID;

    SELECT @@ROWCOUNT;
    RETURN @@ROWCOUNT;
END
GO

-- ========================================================
-- 14. STORED PROCEDURE: RM_DigitalSales_ChangeStatus
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_ChangeStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_ChangeStatus;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_ChangeStatus
    @DigitalSalesID INT,
    @NewStatusID INT,
    @Note NVARCHAR(MAX) = NULL,
    @AttachmentPath NVARCHAR(500) = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatusID INT;
    DECLARE @CurrentBusinessType TINYINT;
    DECLARE @NewBusinessType TINYINT;
    DECLARE @NewStatusName NVARCHAR(250);
    DECLARE @AssignedEmployeeID INT;

    SELECT 
        @CurrentStatusID = StatusID,
        @CurrentBusinessType = BusinessType,
        @AssignedEmployeeID = AssignedEmployeeID
    FROM dbo.RM_DigitalSales
    WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0;

    IF @CurrentStatusID IS NULL 
    BEGIN
        SELECT -1;
        RETURN -1;
    END

    SELECT 
        @NewBusinessType = BusinessType,
        @NewStatusName = StatusName
    FROM dbo.RM_DigitalSalesStatus
    WHERE StatusID = @NewStatusID AND IsActive = 1 AND IsDeleted = 0;

    IF @NewBusinessType IS NULL 
    BEGIN
        SELECT -2;
        RETURN -2;
    END

    -- ========================================================
    -- KIỂM TRA RÀNG BUỘC KHI CHUYỂN SANG DỰ ÁN (BusinessType = 2)
    -- ========================================================
    IF @NewBusinessType = 2
    BEGIN
        -- 1. Bắt buộc có ít nhất 1 sản phẩm dịch vụ số
        IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0)
        BEGIN
            SELECT -3;
            RETURN -3; -- Thiếu thông tin sản phẩm dịch vụ
        END

        -- 2. Bắt buộc có thành viên tham gia
        IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesMember WHERE DigitalSalesID = @DigitalSalesID AND IsActive = 1)
        BEGIN
            SELECT -4;
            RETURN -4; -- Thiếu danh sách thành viên tham gia
        END
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Cập nhật trạng thái và loại hình
        UPDATE dbo.RM_DigitalSales
        SET
            StatusID = @NewStatusID,
            BusinessType = @NewBusinessType,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName,
            EndDate = CASE WHEN @NewStatusID = 8 THEN GETDATE() ELSE EndDate END
        WHERE DigitalSalesID = @DigitalSalesID;

        -- Ghi nhận dòng thời gian (Timeline)
        DECLARE @ActionDesc NVARCHAR(MAX);
        IF @CurrentBusinessType = 1 AND @NewBusinessType = 2
        BEGIN
            SET @ActionDesc = N'Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới: ' + @NewStatusName;
        END
        ELSE
        BEGIN
            SET @ActionDesc = N'Chuyển trạng thái sang: ' + @NewStatusName;
        END

        IF @Note IS NOT NULL AND LTRIM(RTRIM(@Note)) <> ''
        BEGIN
            SET @ActionDesc = @ActionDesc + N' | Ghi chú: ' + @Note;
        END

        INSERT INTO dbo.RM_DigitalSalesTimeline
        (
            DigitalSalesID, FromStatusID, ToStatusID, FromBusinessType, ToBusinessType, ActionDate, ActionBy, Note, AttachmentPath
        )
        VALUES
        (
            @DigitalSalesID, @CurrentStatusID, @NewStatusID, @CurrentBusinessType, @NewBusinessType, GETDATE(), @UserName, @ActionDesc, @AttachmentPath
        );

        DECLARE @NewTimelineID INT = SCOPE_IDENTITY();

        -- Tự động ghi nhận Activity Stream (ActivityType = 2: Chuyển trạng thái)
        DECLARE @ActionByName NVARCHAR(250);
        SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
        IF @ActionByName IS NULL SET @ActionByName = @UserName;

        INSERT INTO dbo.RM_DigitalSalesActivity
        (
            DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
        )
        VALUES
        (
            @DigitalSalesID, 2, @ActionDesc, @AttachmentPath, @NewTimelineID, GETDATE(), @UserName, @ActionByName, 0
        );

        -- Tự động sinh các tiến trình theo quy trình của trạng thái mới nếu chưa có
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
        WHERE p.StatusID = @NewStatusID 
          AND p.IsActive = 1 
          AND p.IsDeleted = 0 
          AND pg.IsActive = 1 
          AND pg.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 FROM dbo.RM_DigitalSalesTracking t 
              WHERE t.DigitalSalesID = @DigitalSalesID AND t.ProgressID = pg.ProgressID
          );

        COMMIT TRANSACTION;
        SELECT 1;
        RETURN 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH
END
GO

-- ========================================================
-- 15. STORED PROCEDURES: QUẢN LÝ CHECKLIST & TIẾN TRÌNH DỰ ÁN (RM_DigitalSalesTracking)
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSalesTracking_GetBySalesID', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TrackingID,
        t.DigitalSalesID,
        t.ProcessID,
        ISNULL(p.ProcessName, N'Checklist tiến trình dự án') AS ProcessName,
        p.StatusID,
        st.StatusName AS SalesStatusName,
        t.ProgressID,
        ISNULL(t.TaskName, ISNULL(pg.ProgressName, N'Công việc')) AS TaskName,
        ISNULL(pg.ProgressName, t.TaskName) AS ProgressName,
        ISNULL(pg.DefaultDurationDays, 3) AS DefaultDurationDays,
        t.AssignedUserID,
        u.FullName AS AssignedUserName,
        t.StartDate,
        t.Deadline,
        t.CompletedDate,
        t.Status,
        CASE t.Status 
            WHEN 1 THEN N'Chưa thực hiện' 
            WHEN 2 THEN N'Đang thực hiện' 
            WHEN 3 THEN N'Hoàn thành' 
            WHEN 4 THEN N'Quá hạn' 
            ELSE N'Khác' 
        END AS TaskStatusName,
        CASE 
            WHEN t.Status <> 3 AND t.Deadline < GETDATE() THEN 1 
            ELSE 0 
        END AS IsOverdue,
        t.ResultNote,
        t.AttachmentFile,
        t.IsCustomTask,
        t.SortOrder,
        t.CreatedDate
    FROM dbo.RM_DigitalSalesTracking t
    LEFT JOIN dbo.RM_DigitalSalesProgress pg ON t.ProgressID = pg.ProgressID
    LEFT JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID
    LEFT JOIN dbo.RM_DigitalSalesStatus st ON p.StatusID = st.StatusID
    LEFT JOIN dbo.Sys_Users u ON t.AssignedUserID = u.UserId
    WHERE t.DigitalSalesID = @DigitalSalesID
    ORDER BY ISNULL(p.SortOrder, 999) ASC, ISNULL(pg.SortOrder, 999) ASC, t.SortOrder ASC, t.TrackingID ASC;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesTracking_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesTracking_Save;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_Save
    @TrackingID INT,
    @DigitalSalesID INT,
    @ProcessID INT = NULL,
    @ProgressID INT = NULL,
    @TaskName NVARCHAR(500),
    @AssignedUserID INT = NULL,
    @StartDate DATETIME = NULL,
    @Deadline DATETIME = NULL,
    @Status TINYINT = 1,
    @ResultNote NVARCHAR(MAX) = NULL,
    @AttachmentFile NVARCHAR(500) = NULL,
    @IsCustomTask BIT = 1,
    @SortOrder INT = 0,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TrackingID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID)
    BEGIN
        INSERT INTO dbo.RM_DigitalSalesTracking
        (
            DigitalSalesID, ProcessID, ProgressID, TaskName, AssignedUserID,
            StartDate, Deadline, Status, ResultNote, AttachmentFile, IsCustomTask, SortOrder,
            CreatedDate, CreatedBy
        )
        VALUES
        (
            @DigitalSalesID, @ProcessID, @ProgressID, @TaskName, @AssignedUserID,
            ISNULL(@StartDate, GETDATE()), @Deadline, ISNULL(@Status, 1), @ResultNote, @AttachmentFile, ISNULL(@IsCustomTask, 1), ISNULL(@SortOrder, 0),
            GETDATE(), @UserName
        );
        DECLARE @NewTrackingID INT = SCOPE_IDENTITY();
        SELECT @NewTrackingID;
        RETURN @NewTrackingID;
    END
    ELSE
    BEGIN
        UPDATE dbo.RM_DigitalSalesTracking
        SET
            TaskName = ISNULL(@TaskName, TaskName),
            AssignedUserID = ISNULL(@AssignedUserID, AssignedUserID),
            StartDate = ISNULL(@StartDate, StartDate),
            Deadline = ISNULL(@Deadline, Deadline),
            Status = ISNULL(@Status, Status),
            CompletedDate = CASE WHEN @Status = 3 THEN GETDATE() ELSE CompletedDate END,
            ResultNote = ISNULL(@ResultNote, ResultNote),
            AttachmentFile = ISNULL(@AttachmentFile, AttachmentFile),
            SortOrder = ISNULL(@SortOrder, SortOrder),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE TrackingID = @TrackingID;

        SELECT @TrackingID;
        RETURN @TrackingID;
    END
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesTracking_UpdateStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesTracking_UpdateStatus;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_UpdateStatus
    @TrackingID INT,
    @Status TINYINT,                     -- 1: Chưa làm, 2: Đang làm, 3: Hoàn thành
    @ResultNote NVARCHAR(MAX) = NULL,
    @AttachmentFile NVARCHAR(500) = NULL,
    @AssignedUserID INT = NULL,
    @Deadline DATETIME = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DigitalSalesID INT;
    DECLARE @TaskName NVARCHAR(500);
    DECLARE @OldStatus TINYINT;
    DECLARE @ProcessID INT;

    SELECT 
        @DigitalSalesID = DigitalSalesID,
        @TaskName = TaskName,
        @OldStatus = Status,
        @ProcessID = ProcessID
    FROM dbo.RM_DigitalSalesTracking
    WHERE TrackingID = @TrackingID;

    IF @DigitalSalesID IS NULL
    BEGIN
        SELECT 0;
        RETURN 0;
    END

    UPDATE dbo.RM_DigitalSalesTracking
    SET
        Status = @Status,
        CompletedDate = CASE WHEN @Status = 3 THEN GETDATE() ELSE CompletedDate END,
        ResultNote = ISNULL(@ResultNote, ResultNote),
        AttachmentFile = ISNULL(@AttachmentFile, AttachmentFile),
        AssignedUserID = ISNULL(@AssignedUserID, AssignedUserID),
        Deadline = ISNULL(@Deadline, Deadline),
        LastModifiedDate = GETDATE(),
        LastModifiedBy = @UserName
    WHERE TrackingID = @TrackingID;

    DECLARE @ActionByName NVARCHAR(250);
    SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
    IF @ActionByName IS NULL SET @ActionByName = @UserName;

    -- Ghi Activity Log nếu có sự kiện đáng chú ý
    IF @Status = 3 AND @OldStatus <> 3
    BEGIN
        -- ActivityType = 3: Hoàn thành checklist
        DECLARE @CompleteContent NVARCHAR(MAX) = N'Đã hoàn thành công việc checklist: ' + @TaskName;
        IF @ResultNote IS NOT NULL AND LTRIM(RTRIM(@ResultNote)) <> ''
        BEGIN
            SET @CompleteContent = @CompleteContent + N' | Kết quả: ' + @ResultNote;
        END

        INSERT INTO dbo.RM_DigitalSalesActivity
        (
            DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
        )
        VALUES
        (
            @DigitalSalesID, 3, @CompleteContent, @AttachmentFile, @TrackingID, GETDATE(), @UserName, @ActionByName, 0
        );

        -- Kiểm tra nếu toàn bộ task trong ProcessID này đã hoàn thành thì ghi log ActivityType = 4 (Hoàn thành quy trình)
        IF @ProcessID IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.RM_DigitalSalesTracking 
            WHERE DigitalSalesID = @DigitalSalesID AND ProcessID = @ProcessID AND Status <> 3
        )
        BEGIN
            DECLARE @ProcName NVARCHAR(250);
            SELECT @ProcName = ProcessName FROM dbo.RM_DigitalSalesProcess WHERE ProcessID = @ProcessID;
            IF @ProcName IS NOT NULL
            BEGIN
                INSERT INTO dbo.RM_DigitalSalesActivity
                (
                    DigitalSalesID, ActivityType, Content, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
                )
                VALUES
                (
                    @DigitalSalesID, 4, N'Đã hoàn thành 100% các công việc trong quy trình: ' + @ProcName, @ProcessID, GETDATE(), @UserName, @ActionByName, 0
                );
            END
        END
    END
    ELSE IF @ResultNote IS NOT NULL AND LTRIM(RTRIM(@ResultNote)) <> '' AND (@OldStatus = @Status OR @Status = 2)
    BEGIN
        -- ActivityType = 5: Cập nhật tiến độ / ghi chú trong checklist
        INSERT INTO dbo.RM_DigitalSalesActivity
        (
            DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
        )
        VALUES
        (
            @DigitalSalesID, 5, N'Cập nhật tiến độ công việc [' + @TaskName + N']: ' + @ResultNote, @AttachmentFile, @TrackingID, GETDATE(), @UserName, @ActionByName, 0
        );
    END

    SELECT 1;
    RETURN 1;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesTracking_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesTracking_Delete;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_Delete
    @TrackingID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID;
    SELECT @@ROWCOUNT;
    RETURN @@ROWCOUNT;
END
GO

-- ========================================================
-- 16. STORED PROCEDURE: RM_DigitalSales_GetTimeline
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_GetTimeline', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_GetTimeline;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_GetTimeline
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        tl.TimelineID,
        tl.DigitalSalesID,
        tl.FromStatusID,
        sFrom.StatusName AS FromStatusName,
        tl.ToStatusID,
        sTo.StatusName AS ToStatusName,
        tl.FromBusinessType,
        CASE tl.FromBusinessType WHEN 1 THEN N'Cơ hội' WHEN 2 THEN N'Dự án' ELSE NULL END AS FromBusinessTypeName,
        tl.ToBusinessType,
        CASE tl.ToBusinessType WHEN 1 THEN N'Cơ hội' WHEN 2 THEN N'Dự án' ELSE N'Khác' END AS ToBusinessTypeName,
        tl.ActionDate,
        tl.ActionBy,
        u.FullName AS ActionByName,
        tl.Note,
        tl.AttachmentPath
    FROM dbo.RM_DigitalSalesTimeline tl
    LEFT JOIN dbo.RM_DigitalSalesStatus sFrom ON tl.FromStatusID = sFrom.StatusID
    INNER JOIN dbo.RM_DigitalSalesStatus sTo ON tl.ToStatusID = sTo.StatusID
    LEFT JOIN dbo.Sys_Users u ON tl.ActionBy = u.UserName
    WHERE tl.DigitalSalesID = @DigitalSalesID
    ORDER BY tl.ActionDate ASC, tl.TimelineID ASC;
END
GO

-- ========================================================
-- 17. STORED PROCEDURE: RM_DigitalSales_Delete
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_Delete;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_Delete
    @DigitalSalesID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RM_DigitalSales
    SET
        IsDeleted = 1,
        LastModifiedDate = GETDATE(),
        LastModifiedBy = @UserName
    WHERE DigitalSalesID = @DigitalSalesID;

    SELECT @@ROWCOUNT;
    RETURN @@ROWCOUNT;
END
GO

-- ========================================================
-- 18. STORED PROCEDURE: RM_DigitalSalesStatus_GetAll
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSalesStatus_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesStatus_GetAll;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesStatus_GetAll
    @BusinessType TINYINT = NULL        -- NULL: Tất cả, 1: Cơ hội, 2: Dự án
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        StatusID,
        StatusCode,
        StatusName,
        BusinessType,
        CASE BusinessType WHEN 1 THEN N'Cơ hội' WHEN 2 THEN N'Dự án' ELSE N'Khác' END AS BusinessTypeName,
        SortOrder,
        IsDefault,
        Description
    FROM dbo.RM_DigitalSalesStatus
    WHERE IsActive = 1 AND IsDeleted = 0
      AND (@BusinessType IS NULL OR BusinessType = @BusinessType)
    ORDER BY BusinessType ASC, SortOrder ASC, StatusID ASC;
END
GO

-- ========================================================
-- 19. STORED PROCEDURE: RM_DigitalSales_ToggleKeyProject
-- ========================================================
IF OBJECT_ID('dbo.RM_DigitalSales_ToggleKeyProject', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSales_ToggleKeyProject;
GO

CREATE PROCEDURE dbo.RM_DigitalSales_ToggleKeyProject
    @DigitalSalesID INT,
    @IsKeyProject BIT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0)
    BEGIN
        SELECT 0 AS Result;
        RETURN 0;
    END

    UPDATE dbo.RM_DigitalSales
    SET IsKeyProject = @IsKeyProject,
        LastModifiedDate = GETDATE(),
        LastModifiedBy = @UserName
    WHERE DigitalSalesID = @DigitalSalesID;

    DECLARE @ActionText NVARCHAR(500);
    IF @IsKeyProject = 1
        SET @ActionText = N'Đánh dấu là Dự án trọng điểm';
    ELSE
        SET @ActionText = N'Bỏ đánh dấu Dự án trọng điểm';

    INSERT INTO dbo.RM_DigitalSalesTimeline
    (
        DigitalSalesID, FromStatusID, ToStatusID, FromBusinessType, ToBusinessType, ActionDate, ActionBy, Note
    )
    SELECT
        @DigitalSalesID, ds.StatusID, ds.StatusID, ds.BusinessType, ds.BusinessType, GETDATE(), @UserName,
        @ActionText
    FROM dbo.RM_DigitalSales ds
    WHERE ds.DigitalSalesID = @DigitalSalesID;

    SELECT 1 AS Result;
    RETURN 1;
END
GO

