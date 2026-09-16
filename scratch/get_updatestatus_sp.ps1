$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== RM_DigitalSalesTracking_UpdateStatus ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "sp_helptext RM_DigitalSalesTracking_UpdateStatus"
$r = $cmd.ExecuteReader()
while ($r.Read()) { Write-Host -NoNewline $r[0] }
$r.Close()

$conn.Close()
