$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE dbo.RM_DigitalSalesTracking SET TimelineID = 250 WHERE TrackingID = 453 AND TimelineID IS NULL"
$n = $cmd.ExecuteNonQuery()
Write-Host "Rows updated: $n"
$conn.Close()
