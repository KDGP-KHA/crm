$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TimelineID, DigitalSalesID, FromStatusID, ToStatusID, ActionDate, Note FROM dbo.RM_DigitalSalesTimeline WHERE DigitalSalesID = 97 ORDER BY ActionDate ASC, TimelineID ASC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("Timeline {0}: From {1} -> To {2} | Date: {3} | Note: {4}" -f $r["TimelineID"], $r["FromStatusID"], $r["ToStatusID"], $r["ActionDate"], $r["Note"])
}
$r.Close()

Write-Host "=== TRACKING TASKS IN DB ==="
$cmd.CommandText = "SELECT TrackingID, TaskName, StatusID, ProcessID, TimelineID, CreatedDate FROM dbo.RM_DigitalSalesTracking WHERE DigitalSalesID = 97 ORDER BY TrackingID ASC"
$r2 = $cmd.ExecuteReader()
while ($r2.Read()) {
    Write-Host ("Track {0}: {1} | StatusID: {2} | ProcID: {3} | TL: {4}" -f $r2["TrackingID"], $r2["TaskName"], $r2["StatusID"], $r2["ProcessID"], $r2["TimelineID"])
}
$r2.Close()

$conn.Close()
