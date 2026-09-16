$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$c = New-Object System.Data.SqlClient.SqlConnection($connStr)
$c.Open()
$cmd = $c.CreateCommand()
$cmd.CommandText = "SELECT TOP 10 ActivityID, DigitalSalesID, ActivityType, Content, ReferenceID, ActionDate, ActionByName FROM RM_DigitalSalesActivity WHERE DigitalSalesID = 97 ORDER BY ActionDate DESC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host ("[{0}] Ref:{1} | {2} | {3}" -f $r["ActivityType"], $r["ReferenceID"], $r["ActionByName"], $r["Content"])
}
$c.Close()
