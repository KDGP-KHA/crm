$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT LabelKey, Message FROM dbo.Sys_Messages WHERE LabelKey LIKE '%ExpectedRevenue%' OR LabelKey LIKE '%Product%Revenue%'"
$r2 = $cmd2.ExecuteReader()
while ($r2.Read()) {
    Write-Host "$($r2[0]) = $($r2[1])"
}
$r2.Close()
$conn.Close()
