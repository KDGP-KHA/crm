$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$cmd = $conn.CreateCommand()

# Cleanup any orphaned 454 if exists
$cmd.CommandText = "DELETE FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = 454"
$cmd.ExecuteNonQuery()

# 1. Tạo 1 tiến trình cha test
$cmd.CommandText = @"
EXEC dbo.RM_DigitalSalesTracking_Save
    @TrackingID = 0,
    @DigitalSalesID = 97,
    @ProcessID = 6,
    @TaskName = N'Tiến trình Test Cascade Completion',
    @StartDate = '2026-09-15',
    @Deadline = '2026-09-20',
    @Status = 2,
    @UserName = 'tranduytan';
"@
$parentId = [int]$cmd.ExecuteScalar()
Write-Host "Created Parent Tracking ID: $parentId"

# 2. Tạo công việc con 1 (Status = 1)
$cmd.CommandText = @"
EXEC dbo.RM_DigitalSalesTracking_Save
    @TrackingID = 0,
    @DigitalSalesID = 97,
    @ProcessID = 6,
    @ParentID = $parentId,
    @TaskName = N'Công việc con 1 (Chưa làm)',
    @StartDate = '2026-09-15',
    @Deadline = '2026-09-18',
    @Status = 1,
    @UserName = 'tranduytan';
"@
$c1 = [int]$cmd.ExecuteScalar()

# Tạo công việc con 2 (Status = 2)
$cmd.CommandText = @"
EXEC dbo.RM_DigitalSalesTracking_Save
    @TrackingID = 0,
    @DigitalSalesID = 97,
    @ProcessID = 6,
    @ParentID = $parentId,
    @TaskName = N'Công việc con 2 (Đang làm)',
    @StartDate = '2026-09-15',
    @Deadline = '2026-09-19',
    @Status = 2,
    @UserName = 'tranduytan';
"@
$c2 = [int]$cmd.ExecuteScalar()
Write-Host "Created Child 1 ID: $c1, Child 2 ID: $c2"

# 3. Cập nhật Tiến trình cha sang Hoàn thành (Status = 3) qua UpdateTrackingStatus
$cmd.CommandText = @"
EXEC dbo.RM_DigitalSalesTracking_UpdateStatus
    @TrackingID = $parentId,
    @Status = 3,
    @ResultNote = N'Đã hoàn thành toàn bộ phương án kỹ thuật',
    @UserName = 'tranduytan';
"@
$cmd.ExecuteNonQuery()
Write-Host "Called RM_DigitalSalesTracking_UpdateStatus with Status = 3 on Parent: $parentId"

# 4. Kiểm tra trạng thái của 2 công việc con
$cmd.CommandText = @"
SELECT TrackingID, TaskName, Status, CompletedDate 
FROM dbo.RM_DigitalSalesTracking 
WHERE TrackingID IN ($c1, $c2);
"@
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("Child: {0} ({1}) | Status: {2} | CompletedDate: {3}" -f $r["TaskName"], $r["TrackingID"], $r["Status"], $r["CompletedDate"])
}
$r.Close()

# 5. Kiểm tra Activity Log được sinh ra
$cmd.CommandText = @"
SELECT ActivityID, ActivityType, Content, ReferenceID, ActionDate, ActionByName
FROM dbo.RM_DigitalSalesActivity
WHERE DigitalSalesID = 97 AND ReferenceID IN ($parentId, $c1, $c2)
ORDER BY ActionDate DESC;
"@
$r2 = $cmd.ExecuteReader()
Write-Host "=== ACTIVITIES GENERATED ==="
while ($r2.Read()) {
    Write-Host ("[{0}] Ref:{1} | {2} | {3}" -f $r2["ActivityType"], $r2["ReferenceID"], $r2["ActionByName"], $r2["Content"])
}
$r2.Close()

# 6. Dọn dẹp dữ liệu test
$cmd.CommandText = @"
DELETE FROM dbo.RM_DigitalSalesActivity WHERE DigitalSalesID = 97 AND ReferenceID IN ($parentId, $c1, $c2);
DELETE FROM dbo.RM_DigitalSalesTracking WHERE TrackingID IN ($parentId, $c1, $c2);
"@
$cmd.ExecuteNonQuery()
Write-Host "Cleaned up test data."

$conn.Close()
