$connStr = 'Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()

Write-Host "=== SP RM_DigitalSalesTracking_Save ==="
$cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID('RM_DigitalSalesTracking_Save'))"
$def = $cmd.ExecuteScalar()
Write-Host $def

$conn.Close()
