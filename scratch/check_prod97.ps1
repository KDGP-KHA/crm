$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT SalesProductID, ProductServiceID, ExpectedRevenue, ActualRevenue, Quantity FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = 97"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host "ProdID: $($r[0]) | ExpectedRevenue: $($r[2]) | ActualRevenue: $($r[3])"
}
$r.Close()
$conn.Close()
