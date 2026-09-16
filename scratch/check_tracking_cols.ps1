$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' ORDER BY ORDINAL_POSITION"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("{0} ({1})" -f $r["COLUMN_NAME"], $r["DATA_TYPE"])
}
$r.Close()
$conn.Close()
