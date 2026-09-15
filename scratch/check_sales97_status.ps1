$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== TIMELINES OF SALES 97 ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TimelineID, DigitalSalesID, FromStatusID, ToStatusID, ActionDate, Note FROM dbo.RM_DigitalSalesTimeline WHERE DigitalSalesID = 97 ORDER BY TimelineID ASC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("TimelineID: {0} | From: {1} | To: {2} | Date: {3:yyyy-MM-dd HH:mm:ss} | Note: {4}" -f $r["TimelineID"], $r["FromStatusID"], $r["ToStatusID"], $r["ActionDate"], $r["Note"])
}
$r.Close()

Write-Host "`n=== TRACKING TASKS OF SALES 97 ==="
$cmd.CommandText = "SELECT TrackingID, TimelineID, ProcessID, TaskName, Status, CreatedDate FROM dbo.RM_DigitalSalesTracking WHERE DigitalSalesID = 97 ORDER BY TrackingID ASC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("TrackingID: {0} | TimelineID: {1} | ProcessID: {2} | Status: {3} | TaskName: {4}" -f $r["TrackingID"], $r["TimelineID"], $r["ProcessID"], $r["Status"], $r["TaskName"])
}
$r.Close()

$conn.Close()
