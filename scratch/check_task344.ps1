$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TrackingID, TrackingCode, TaskName, AssignedUserID, StartDate, Deadline, Status, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy FROM dbo.RM_DigitalSalesTracking WHERE TrackingCode = 'PR2609000344'"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host "ID: $($r[0]) | Code: $($r[1]) | Task: $($r[2]) | AssignedUser: $($r[3]) | Start: $($r[4]) | Deadline: $($r[5]) | Status: $($r[6]) | CreatedDate: $($r[7]) | CreatedBy: $($r[8]) | ModifiedDate: $($r[9]) | ModifiedBy: $($r[10])"
}
$r.Close()
$conn.Close()
