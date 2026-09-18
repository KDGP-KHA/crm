SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Sys_UserGuideState', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_UserGuideState
    (
        UserGuideStateID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sys_UserGuideState PRIMARY KEY,
        UserName VARCHAR(150) NOT NULL,
        ScreenCode VARCHAR(150) NOT NULL,
        ViewedDate DATETIME NOT NULL CONSTRAINT DF_Sys_UserGuideState_ViewedDate DEFAULT(GETDATE())
    );
    CREATE UNIQUE INDEX UX_Sys_UserGuideState_User_Screen ON dbo.Sys_UserGuideState(UserName, ScreenCode);
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideState_IsViewed', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideState_IsViewed;
GO
CREATE PROCEDURE dbo.Sys_UserGuideState_IsViewed
    @UserName VARCHAR(150),
    @ScreenCode VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS
    (
        SELECT 1 FROM dbo.Sys_UserGuideState
        WHERE UserName = @UserName AND ScreenCode = @ScreenCode
    ) THEN 1 ELSE 0 END AS INT);
END
GO

IF OBJECT_ID('dbo.Sys_UserGuideState_MarkViewed', 'P') IS NOT NULL
    DROP PROCEDURE dbo.Sys_UserGuideState_MarkViewed;
GO
CREATE PROCEDURE dbo.Sys_UserGuideState_MarkViewed
    @UserName VARCHAR(150),
    @ScreenCode VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    IF NULLIF(LTRIM(RTRIM(@UserName)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@ScreenCode)), '') IS NULL
        RETURN -1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Sys_UserGuideState WHERE UserName = @UserName AND ScreenCode = @ScreenCode)
        INSERT INTO dbo.Sys_UserGuideState(UserName, ScreenCode) VALUES (@UserName, @ScreenCode);

    RETURN 1;
END
GO
