$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('dbo.RM_DigitalSalesTracking_UpdateStatus')"
$res = $cmd.ExecuteScalar()
Write-Host "=== RM_DigitalSalesTracking_UpdateStatus ==="
Write-Host $res

$cmd.CommandText = "SELECT definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('dbo.RM_DigitalSalesTracking_Save')"
$res2 = $cmd.ExecuteScalar()
Write-Host "=== RM_DigitalSalesTracking_Save ==="
Write-Host $res2
$conn.Close()
