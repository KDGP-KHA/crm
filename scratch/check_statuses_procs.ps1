$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== STATUSES ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT StatusID, StatusCode, StatusName FROM dbo.RM_DigitalSalesStatus ORDER BY SortOrder ASC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("StatusID: {0} | Code: {1} | Name: {2}" -f $r["StatusID"], $r["StatusCode"], $r["StatusName"])
}
$r.Close()

Write-Host "`n=== PROCESSES BY STATUS ==="
$cmd.CommandText = "SELECT ProcessID, ProcessName, StatusID, SortOrder, IsActive FROM dbo.RM_DigitalSalesProcess WHERE IsDeleted = 0 ORDER BY StatusID, SortOrder"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("ProcessID: {0} | StatusID: {1} | Name: {2} | Active: {3}" -f $r["ProcessID"], $r["StatusID"], $r["ProcessName"], $r["IsActive"])
}
$r.Close()

$conn.Close()
