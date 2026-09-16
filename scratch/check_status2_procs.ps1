$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT ProcessID, ProcessName, StatusID, SortOrder FROM dbo.RM_DigitalSalesProcess WHERE StatusID = 2"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("Process {0}: {1} | StatusID: {2} | SortOrder: {3}" -f $r["ProcessID"], $r["ProcessName"], $r["StatusID"], $r["SortOrder"])
}
$r.Close()
$conn.Close()
