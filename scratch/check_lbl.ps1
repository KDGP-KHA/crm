$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT LabelKey, ContentVN FROM dbo.Sys_Messages WHERE LabelKey LIKE '%ExpectedRevenue%'"
$r = $cmd.ExecuteReader()
while ($r.Read()) { Write-Host ($r[0] + " = " + $r[1]) }
$r.Close()
$conn.Close()
