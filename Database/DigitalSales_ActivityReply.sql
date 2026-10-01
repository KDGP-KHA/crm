-- ========================================================
-- Cập nhật tính năng Phản hồi trao đổi (Discussion Reply)
-- Thêm cột ReplyToActivityID và cập nhật SPs
-- ========================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RM_DigitalSalesActivity' AND COLUMN_NAME = 'ReplyToActivityID'
)
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesActivity ADD ReplyToActivityID INT NULL;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesActivity_Add', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesActivity_Add;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesActivity_Add
    @DigitalSalesID INT,
    @ActivityType TINYINT = 1,
    @Content NVARCHAR(MAX),
    @Attachments NVARCHAR(MAX) = NULL,
    @MentionedUserIDs VARCHAR(500) = NULL,
    @MentionedNames NVARCHAR(1000) = NULL,
    @ReferenceID INT = NULL,
    @UserName VARCHAR(150),
    @ReplyToActivityID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActionByName NVARCHAR(250);
    SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
    IF @ActionByName IS NULL SET @ActionByName = @UserName;

    INSERT INTO dbo.RM_DigitalSalesActivity
    (
        DigitalSalesID, ActivityType, Content, Attachments, MentionedUserIDs, MentionedNames, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted, ReplyToActivityID
    )
    VALUES
    (
        @DigitalSalesID, @ActivityType, @Content, @Attachments, @MentionedUserIDs, @MentionedNames, @ReferenceID, GETDATE(), @UserName, @ActionByName, 0, @ReplyToActivityID
    );

    DECLARE @NewActivityID INT = SCOPE_IDENTITY();

    -- Luôn cập nhật ActionTime trên hồ sơ gốc khi có thảo luận / hoạt động
    UPDATE dbo.RM_DigitalSales
    SET ActionTime = GETDATE(), LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
    WHERE DigitalSalesID = @DigitalSalesID;

    SELECT @NewActivityID;
    RETURN @NewActivityID;
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesActivity_GetList', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesActivity_GetList;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesActivity_GetList
    @DigitalSalesID INT,
    @ActivityType TINYINT = NULL -- NULL: Tất cả trao đổi & trạng thái (1, 2), 1: Chỉ trao đổi, 2: Trạng thái, 99: Checklist (3, 4, 5), 255: Tất cả toàn bộ
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        a.ActivityID,
        a.DigitalSalesID,
        a.ActivityType,
        a.Content,
        a.Attachments,
        a.MentionedUserIDs,
        a.MentionedNames,
        a.ReferenceID,
        a.ActionDate,
        a.ActionBy,
        ISNULL(a.ActionByName, u.FullName) AS ActionByName,
        u.Avatar AS ActionByAvatar,
        ISNULL(bp.TenBoPhan, u.OfficeName) AS ActionByDepartment,
        a.IsDeleted,
        a.ReplyToActivityID,
        ISNULL(parent.ActionByName, pu.FullName) AS ReplyToActionByName,
        parent.Content AS ReplyToContent,
        parent.ActionDate AS ReplyToActionDate,
        parent.ActivityType AS ReplyToActivityType
    FROM dbo.RM_DigitalSalesActivity a
    LEFT JOIN dbo.Sys_Users u ON a.ActionBy = u.UserName
    LEFT JOIN dbo.MN_BoPhan bp ON u.MaBoPhan = bp.MaBoPhan
    LEFT JOIN dbo.RM_DigitalSalesActivity parent ON a.ReplyToActivityID = parent.ActivityID
    LEFT JOIN dbo.Sys_Users pu ON parent.ActionBy = pu.UserName
    WHERE a.DigitalSalesID = @DigitalSalesID 
      AND a.IsDeleted = 0
      AND (
          (@ActivityType IS NULL AND a.ActivityType NOT IN (3, 4, 5))
          OR (@ActivityType = 99 AND a.ActivityType IN (3, 4, 5))
          OR (@ActivityType = 255)
          OR (@ActivityType IS NOT NULL AND @ActivityType NOT IN (99, 255) AND a.ActivityType = @ActivityType)
      )
    ORDER BY a.ActionDate DESC, a.ActivityID DESC;
END
GO

-- Cập nhật Sys_Messages cho tính năng Phản hồi trao đổi
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LabelKey = 'DigitalSales_Discussion_ReplyNotificationTitle' AND LangCode = 'vi')
BEGIN
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
    VALUES ('vi', 'DigitalSales_Discussion_ReplyNotificationTitle', N'{0} đã phản hồi một trao đổi của bạn');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LabelKey = 'DigitalSales_Discussion_ReplyNotificationContent' AND LangCode = 'vi')
BEGIN
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
    VALUES ('vi', 'DigitalSales_Discussion_ReplyNotificationContent', N'{0}: {1}');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LabelKey = 'DigitalSales_Discussion_ReplyBtn' AND LangCode = 'vi')
BEGIN
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
    VALUES ('vi', 'DigitalSales_Discussion_ReplyBtn', N'Phản hồi');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LabelKey = 'DigitalSales_Discussion_ReplyingTo' AND LangCode = 'vi')
BEGIN
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
    VALUES ('vi', 'DigitalSales_Discussion_ReplyingTo', N'Đang phản hồi');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Messages WHERE LabelKey = 'DigitalSales_Discussion_CancelReply' AND LangCode = 'vi')
BEGIN
    INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
    VALUES ('vi', 'DigitalSales_Discussion_CancelReply', N'Hủy phản hồi');
END
GO
