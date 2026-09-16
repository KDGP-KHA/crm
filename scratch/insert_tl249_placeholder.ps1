$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# 1. Clean up test child tasks 455, 456
$cmd = $conn.CreateCommand()
$cmd.CommandText = "DELETE FROM dbo.RM_DigitalSalesActivity WHERE DigitalSalesID = 97 AND ReferenceID IN (455, 456); DELETE FROM dbo.RM_DigitalSalesTracking WHERE TrackingID IN (455, 456);"
$cmd.ExecuteNonQuery()

# 2. Insert placeholder for Timeline 249 (Status 2 - Đang tiếp cận)
$cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesTracking WHERE DigitalSalesID = 97 AND TimelineID = 249)
BEGIN
    INSERT INTO dbo.RM_DigitalSalesTracking
    (
        DigitalSalesID, ParentID, DurationDays, ProcessID, ProgressID, TaskName, AssignedUserID,
        StartDate, Deadline, Status, ResultNote, AttachmentFile, IsCustomTask, SortOrder,
        CreatedDate, CreatedBy, TrackingCode, TimelineID
    )
    VALUES
    (
        97, NULL, 3, 5, NULL, NULL, NULL,
        '2026-09-15', '2026-09-18', 1, NULL, NULL, 0, 0,
        '2026-09-15 20:32:16', 'tranduytan', NULL, 249
    );
END
"@
$cmd.ExecuteNonQuery()
Write-Host "Inserted placeholder for Timeline 249"

# 3. Test RM_DigitalSalesTracking_GetBySalesID
$cmd.CommandText = "EXEC dbo.RM_DigitalSalesTracking_GetBySalesID @DigitalSalesID = 97"
$r = $cmd.ExecuteReader()
Write-Host "=== TASKS RETURNED FOR SALES 97 ==="
while ($r.Read()) {
    Write-Host ("TL: {0} | Status: {1} ({2}) | Proc: {3} | Task: {4}" -f $r["TimelineID"], $r["StatusID"], $r["SalesStatusName"], $r["ProcessName"], $r["TaskName"])
}
$r.Close()
$conn.Close()
