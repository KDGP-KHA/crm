$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "EXEC dbo.RM_DigitalSalesTracking_GetBySalesID @DigitalSalesID = 97"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host "Task: $($r['TaskName']) | Assigned: $($r['AssignedUserName']) | CreatedBy: $($r['CreatedByName']) | ModBy: $($r['LastModifiedByName'])"
}
$r.Close()
$conn.Close()
