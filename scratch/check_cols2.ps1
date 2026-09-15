$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 1 * FROM dbo.Sys_Messages"
$r = $cmd.ExecuteReader()
for ($i = 0; $i -lt $r.FieldCount; $i++) {
    Write-Host $r.GetName($i)
}
$r.Close()

Write-Host "--- Messages ---"
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT MessageID, Message FROM dbo.Sys_Messages WHERE MessageID LIKE '%ExpectedRevenue%' OR MessageID LIKE '%Revenue%'"
$r2 = $cmd2.ExecuteReader()
while ($r2.Read()) {
    Write-Host "$($r2[0]) = $($r2[1])"
}
$r2.Close()

$conn.Close()
