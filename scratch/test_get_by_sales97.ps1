$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== RM_DigitalSalesTracking_GetBySalesID 97 ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "EXEC dbo.RM_DigitalSalesTracking_GetBySalesID @DigitalSalesID = 97"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("TrackingID: {0} | TimelineID: {1} | StatusID: {2} | StatusName: {3} | ProcessID: {4} | ProcessName: {5} | TaskName: '{6}'" -f $r["TrackingID"], $r["TimelineID"], $r["StatusID"], $r["SalesStatusName"], $r["ProcessID"], $r["ProcessName"], $r["TaskName"])
}
$r.Close()
$conn.Close()
