import base64
import subprocess

sql = """ALTER PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TrackingID,
        t.DigitalSalesID,
        t.ParentID,
        t.ProcessID,
        ISNULL(p.ProcessName, N'Checklist tiến trình dự án') AS ProcessName,
        p.StatusID,
        st.StatusName AS SalesStatusName,
        (SELECT COUNT(1) FROM dbo.RM_DigitalSalesProcess pr WHERE pr.StatusID = p.StatusID AND pr.IsActive = 1 AND pr.IsDeleted = 0) AS ProcessCountOfStatus,
        t.ProgressID,
        ISNULL(t.TaskName, pg.ProgressName) AS TaskName,
        ISNULL(pg.ProgressName, t.TaskName) AS ProgressName,
        ISNULL(t.DurationDays, ISNULL(pg.DefaultDurationDays, 3)) AS DurationDays,
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
        t.CreatedDate,
        t.CreatedBy,
        ISNULL(uCreated.FullName, t.CreatedBy) AS CreatedByName,
        t.LastModifiedDate,
        t.LastModifiedBy,
        ISNULL(uModified.FullName, t.LastModifiedBy) AS LastModifiedByName,
        t.TrackingCode,
        t.TimelineID,
        tl.ActionDate AS TimelineDate
    FROM dbo.RM_DigitalSalesTracking t
    LEFT JOIN dbo.RM_DigitalSalesProgress pg ON t.ProgressID = pg.ProgressID
    LEFT JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID
    LEFT JOIN dbo.RM_DigitalSalesStatus st ON p.StatusID = st.StatusID
    LEFT JOIN dbo.Sys_Users u ON t.AssignedUserID = u.UserId
    LEFT JOIN dbo.Sys_Users uCreated ON t.CreatedBy = uCreated.UserName
    LEFT JOIN dbo.Sys_Users uModified ON t.LastModifiedBy = uModified.UserName
    LEFT JOIN dbo.RM_DigitalSalesTimeline tl ON t.TimelineID = tl.TimelineID
    WHERE t.DigitalSalesID = @DigitalSalesID
    ORDER BY ISNULL(t.TimelineID, t.TrackingID) ASC, ISNULL(p.SortOrder, 999) ASC, ISNULL(pg.SortOrder, 999) ASC, ISNULL(t.ParentID, 0) ASC, t.SortOrder ASC, t.TrackingID ASC;
END"""

b64 = base64.b64encode(sql.encode('utf-8')).decode('ascii')

ps1_content = f'''$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$b64 = "{b64}"
$bytes = [System.Convert]::FromBase64String($b64)
$sql = [System.Text.Encoding]::UTF8.GetString($bytes)

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$cmd.ExecuteNonQuery()
Write-Host "ALTER PROCEDURE RM_DigitalSalesTracking_GetBySalesID with Creator info succeeded!"

$conn.Close()
'''

with open(r"d:\SVN\crm\scratch\run_fix_sp_creator.ps1", "w", encoding="utf-8-sig") as f:
    f.write(ps1_content)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\run_fix_sp_creator.ps1"])
print(out.decode("utf-8", errors="replace"))
