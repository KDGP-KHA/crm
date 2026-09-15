$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TrackingID, TaskName, ProcessID, TimelineID, CreatedDate, Status FROM dbo.RM_DigitalSalesTracking WHERE DigitalSalesID = 97 ORDER BY TrackingID ASC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("Track {0}: {1} | ProcID: {2} | TL: {3} | Status: {4} | Date: {5}" -f $r["TrackingID"], $r["TaskName"], $r["ProcessID"], $r["TimelineID"], $r["Status"], $r["CreatedDate"])
}
$r.Close()
$conn.Close()
