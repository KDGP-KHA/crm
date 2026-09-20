SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_UserGuideConfig
    (
        GuideConfigID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sys_UserGuideConfig PRIMARY KEY,
        ScreenCode VARCHAR(150) NOT NULL,
        StepOrder INT NOT NULL CONSTRAINT DF_Sys_UserGuideConfig_StepOrder DEFAULT(1),
        Selector VARCHAR(250) NOT NULL,
        Title NVARCHAR(250) NOT NULL,
        GuideText NVARCHAR(MAX) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Sys_UserGuideConfig_IsActive DEFAULT(1),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_Sys_UserGuideConfig_CreatedDate DEFAULT(GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        UpdatedDate DATETIME NULL,
        UpdatedBy VARCHAR(150) NULL
    );
    CREATE INDEX IX_Sys_UserGuideConfig_Screen ON dbo.Sys_UserGuideConfig(ScreenCode, StepOrder);
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig_GetByScreenCode', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideConfig_GetByScreenCode;
GO
CREATE PROCEDURE dbo.Sys_UserGuideConfig_GetByScreenCode
    @ScreenCode VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        GuideConfigID,
        ScreenCode,
        StepOrder,
        Selector,
        Title,
        GuideText,
        IsActive
    FROM dbo.Sys_UserGuideConfig
    WHERE ScreenCode = @ScreenCode AND IsActive = 1
    ORDER BY StepOrder ASC, GuideConfigID ASC;
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig_GetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideConfig_GetAll;
GO
CREATE PROCEDURE dbo.Sys_UserGuideConfig_GetAll
    @ScreenPrefix VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        GuideConfigID,
        ScreenCode,
        StepOrder,
        Selector,
        Title,
        GuideText,
        IsActive
    FROM dbo.Sys_UserGuideConfig
    WHERE (@ScreenPrefix IS NULL OR ScreenCode LIKE @ScreenPrefix + '%')
    ORDER BY ScreenCode ASC, StepOrder ASC, GuideConfigID ASC;
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig_SaveStep', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideConfig_SaveStep;
GO
CREATE PROCEDURE dbo.Sys_UserGuideConfig_SaveStep
    @GuideConfigID INT = 0,
    @ScreenCode VARCHAR(150),
    @StepOrder INT,
    @Selector VARCHAR(250),
    @Title NVARCHAR(250),
    @GuideText NVARCHAR(MAX),
    @IsActive BIT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    IF @GuideConfigID > 0 AND EXISTS (SELECT 1 FROM dbo.Sys_UserGuideConfig WHERE GuideConfigID = @GuideConfigID)
    BEGIN
        UPDATE dbo.Sys_UserGuideConfig
        SET ScreenCode = @ScreenCode,
            StepOrder = @StepOrder,
            Selector = @Selector,
            Title = @Title,
            GuideText = @GuideText,
            IsActive = @IsActive,
            UpdatedDate = GETDATE(),
            UpdatedBy = @UserName
        WHERE GuideConfigID = @GuideConfigID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy)
        VALUES (@ScreenCode, @StepOrder, @Selector, @Title, @GuideText, @IsActive, GETDATE(), @UserName);
        SET @GuideConfigID = SCOPE_IDENTITY();
    END
    RETURN 1;
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig_DeleteStep', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideConfig_DeleteStep;
GO
CREATE PROCEDURE dbo.Sys_UserGuideConfig_DeleteStep
    @GuideConfigID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Sys_UserGuideConfig WHERE GuideConfigID = @GuideConfigID;
    RETURN 1;
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideConfig_DeleteByScreenCode', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideConfig_DeleteByScreenCode;
GO
CREATE PROCEDURE dbo.Sys_UserGuideConfig_DeleteByScreenCode
    @ScreenCode VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Sys_UserGuideConfig WHERE ScreenCode = @ScreenCode;
    RETURN 1;
END
GO

-- SEED DATA NẾU CHƯA CÓ DỮ LIỆU
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_UserGuideConfig WHERE ScreenCode LIKE 'Cate.DigitalSales.Detail%')
BEGIN
    -- 1. Toàn trang (Page Tour)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail', 1, '.ds-header-title-container', N'Thông tin hồ sơ', N'Hiển thị mã hồ sơ, tên khách hàng, mã số thuế, loại dịch vụ và nhân viên phụ trách chính của hồ sơ chuyển đổi số.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail', 2, '.ds-header-action-bar', N'Thao tác hồ sơ', N'Các nút chức năng: Chuyển trạng thái quy trình, Chỉnh sửa thông tin hồ sơ, Làm mới dữ liệu và Mở hướng dẫn sử dụng.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail', 3, '#containerMetrics', N'Chỉ số tổng quan', N'Thẻ chỉ số tổng quan gồm: Tổng giá trị hợp đồng, Số sản phẩm dịch vụ, Tiến độ thực hiện nhiệm vụ và Số lượng trao đổi/hoạt động.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail', 4, '.nav-tabs', N'Các khu vực thông tin', N'Thanh điều hướng chuyển đổi giữa 5 tab: Thông tin tổng quan, Sản phẩm & Doanh thu, Tiến trình & Checklist, Trao đổi và Lịch sử rà soát.', 1, GETDATE(), 'system');

    -- 2. Tab 1: Tổng quan (Overview)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail.Overview', 1, '#tab-overview .col-md-6:first-child .card', N'Thông tin hồ sơ', N'Chi tiết các thông tin pháp lý của khách hàng, người liên hệ, cơ hội kinh doanh và nguồn gốc hồ sơ.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Overview', 2, '#tab-overview .col-md-6:nth-child(2) .card', N'Phân công và quản lý', N'Thông tin nhân sự phụ trách AM, cán bộ hỗ trợ giải pháp, quy trình và tiến trình hiện tại của hồ sơ.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Overview', 3, '#sectionMembers', N'Thành viên tham gia', N'Danh sách các cán bộ, chuyên viên thuộc đội ngũ phụ trách hồ sơ, hỗ trợ thêm/xóa thành viên tham gia.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Overview', 4, '#tab-overview .ds-html-note-view', N'Mô tả nhu cầu', N'Nội dung chi tiết về nhu cầu chuyển đổi số của khách hàng, phạm vi yêu cầu và ghi chú quan trọng.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Overview', 5, '#sectionAttachments', N'Tệp đính kèm', N'Khu vực lưu trữ hồ sơ, tài liệu đề xuất, hợp đồng scan và biên bản liên quan đến chuyển đổi số.', 1, GETDATE(), 'system');

    -- 3. Tab 2: Sản phẩm & Doanh thu (Products)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail.Products', 1, '#tab-products h6', N'Danh sách sản phẩm', N'Thống kê tổng số lượng sản phẩm/dịch vụ số và tổng giá trị hợp đồng của hồ sơ.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Products', 2, '#tab-products .btn-purple', N'Thêm sản phẩm', N'Nhấn nút này để thêm mới sản phẩm/dịch vụ số, cấu hình số lượng, đơn giá và giá trị hợp đồng.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Products', 3, '#tab-products .card.bcard', N'Thông tin sản phẩm và hợp đồng', N'Danh sách các dịch vụ số đang tư vấn, trạng thái hợp đồng, ngày bắt đầu và kết thúc sử dụng.', 1, GETDATE(), 'system');

    -- 4. Tab 3: Tiến trình & Checklist (Tracking)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail.Tracking', 1, '#tab-tracking > .d-flex', N'Tổng quan checklist', N'Thanh tiến độ tổng thể phản ánh tỷ lệ hoàn thành các nhiệm vụ và tiến trình theo quy trình chuẩn.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Tracking', 2, '#tblTracking', N'Danh sách tiến trình', N'Bảng phân rã từng bước triển khai, người thực hiện, thời hạn hoàn thành và kết quả thực hiện.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Tracking', 3, '#treeTrackingBody .tree-status-row', N'Trạng thái và quy trình', N'Cây phân cấp các giai đoạn quy trình bán hàng giải pháp số và các tiến trình tương ứng.', 1, GETDATE(), 'system');

    -- 5. Tab 4: Trao đổi chung & Hoạt động (Discussions)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail.Discussions', 1, '#tab-discussions .ds-composer-card', N'Tạo trao đổi', N'Khung soạn thảo phản hồi, trao đổi nhanh giữa các thành viên phụ trách hồ sơ chuyển đổi số.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Discussions', 2, '#frmPostDiscussion', N'Nội dung và tệp đính kèm', N'Nhập nội dung thảo luận, hỗ trợ định dạng văn bản và đính kèm tài liệu trao đổi.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.Discussions', 3, '#tab-discussions .ds-activity-timeline', N'Lịch sử hoạt động', N'Dòng thời gian ghi nhận toàn bộ lịch sử cập nhật, trao đổi, thay đổi trạng thái của hồ sơ.', 1, GETDATE(), 'system');

    -- 6. Tab 5: Lịch sử rà soát (ReviewHistory)
    INSERT INTO dbo.Sys_UserGuideConfig (ScreenCode, StepOrder, Selector, Title, GuideText, IsActive, CreatedDate, CreatedBy) VALUES
    ('Cate.DigitalSales.Detail.ReviewHistory', 1, '#tab-review-history .review-history-timeline', N'Lịch sử rà soát', N'Trục thời gian ghi nhận các đợt rà soát định kỳ đánh giá tính khả thi và tiến độ hồ sơ.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.ReviewHistory', 2, '#tab-review-history .review-history-batch', N'Đợt rà soát', N'Thông tin đợt rà soát, người thực hiện rà soát và thời điểm thực hiện.', 1, GETDATE(), 'system'),
    ('Cate.DigitalSales.Detail.ReviewHistory', 3, '#tab-review-history .review-history-entry', N'Chi tiết kết quả rà soát', N'Kết quả đánh giá hồ sơ trong đợt rà soát: đạt, cần khắc phục hoặc các ghi chú cảnh báo rủi ro.', 1, GETDATE(), 'system');
END
GO

-- KHAI BÁO BẢNG SYS_MESSAGES
DECLARE @Messages TABLE (LabelKey VARCHAR(150), Message NVARCHAR(MAX));
INSERT INTO @Messages (LabelKey, Message) VALUES
('DigitalSales_Guide_Button', N'Hướng dẫn'),
('DigitalSales_Guide_ConfigButton', N'Cấu hình hướng dẫn'),
('DigitalSales_Guide_ModalTitle', N'Cấu hình hướng dẫn sử dụng'),
('DigitalSales_Guide_SaveSuccess', N'Cập nhật cấu hình hướng dẫn thành công'),
('DigitalSales_Guide_SaveError', N'Lỗi khi cập nhật cấu hình hướng dẫn'),
('DigitalSales_Guide_ResetSuccess', N'Đã khôi phục hướng dẫn mặc định'),
('DigitalSales_Guide_Tab_Page', N'Toàn trang'),
('DigitalSales_Guide_Tab_Overview', N'1. Tổng quan'),
('DigitalSales_Guide_Tab_Products', N'2. Sản phẩm & Doanh thu'),
('DigitalSales_Guide_Tab_Tracking', N'3. Tiến trình & Checklist'),
('DigitalSales_Guide_Tab_Discussions', N'4. Trao đổi chung'),
('DigitalSales_Guide_Tab_ReviewHistory', N'5. Lịch sử rà soát'),
('DigitalSales_Guide_Col_Order', N'Thứ tự'),
('DigitalSales_Guide_Col_Selector', N'Bộ chọn CSS (Selector)'),
('DigitalSales_Guide_Col_Title', N'Tiêu đề bước'),
('DigitalSales_Guide_Col_Content', N'Nội dung hướng dẫn'),
('DigitalSales_Guide_Col_Status', N'Kích hoạt'),
('DigitalSales_Guide_Col_Action', N'Thao tác'),
('DigitalSales_Guide_Btn_AddStep', N'Thêm bước'),
('DigitalSales_Guide_Btn_ResetDefault', N'Khôi phục mặc định'),
('DigitalSales_Guide_Btn_Save', N'Lưu cấu hình'),
('DigitalSales_Guide_Btn_Preview', N'Xem thử'),
('DigitalSales_Guide_MoveUp', N'Di chuyển lên'),
('DigitalSales_Guide_MoveDown', N'Di chuyển xuống'),
('DigitalSales_Guide_DeleteStep', N'Xóa bước');

MERGE dbo.Sys_Messages AS target
USING @Messages AS source
ON target.LangCode = 'vi-VN' AND target.LabelKey = source.LabelKey
WHEN MATCHED THEN
    UPDATE SET target.Message = source.Message
WHEN NOT MATCHED THEN
    INSERT (LangCode, LabelKey, Message) VALUES ('vi-VN', source.LabelKey, source.Message);
GO

