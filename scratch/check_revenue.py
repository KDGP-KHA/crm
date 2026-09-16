import subprocess

ps = """
$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== RM_DigitalSales ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT DigitalSalesID, Code, Title, TotalExpectedRevenue, TotalActualRevenue FROM dbo.RM_DigitalSales WHERE Code = 'SPDV-2026-0070'"
$r = $cmd.ExecuteReader()
$salesId = 0
while ($r.Read()) {
    $salesId = $r[0]
    Write-Host "SalesID: $($r[0]) | Code: $($r[1]) | Expected: $($r[3]) | Actual: $($r[4])"
}
$r.Close()

if ($salesId -gt 0) {
    Write-Host "`n=== RM_DigitalSalesProduct ==="
    $cmd2 = $conn.CreateCommand()
    $cmd2.CommandText = "SELECT SalesProductID, ProductServiceID, ProductName, ExpectedRevenue, ActualRevenue, ExpectedRevenueYearly, UnitPrice, Quantity FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = $salesId"
    $r2 = $cmd2.ExecuteReader()
    while ($r2.Read()) {
        Write-Host "ProdID: $($r2[0]) | Name: $($r2[2]) | Expected: $($r2[3]) | Actual: $($r2[4]) | Yearly: $($r2[5]) | UnitPrice: $($r2[6]) | Qty: $($r2[7])"
    }
    $r2.Close()

    Write-Host "`n=== RM_DigitalSalesProductRevenue ==="
    $cmd3 = $conn.CreateCommand()
    $cmd3.CommandText = "SELECT RevenueID, SalesProductID, YearIndex, ExpectedRevenue, ActualRevenue FROM dbo.RM_DigitalSalesProductRevenue WHERE SalesProductID IN (SELECT SalesProductID FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = $salesId)"
    $r3 = $cmd3.ExecuteReader()
    while ($r3.Read()) {
        Write-Host "RevID: $($r3[0]) | SalesProdID: $($r3[1]) | Year: $($r3[2]) | Expected: $($r3[3]) | Actual: $($r3[4])"
    }
    $r3.Close()
}

$conn.Close()
"""

with open(r"d:\SVN\crm\scratch\check_rev.ps1", "w", encoding="utf-8") as f:
    f.write(ps)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\check_rev.ps1"])
print(out.decode("utf-8", errors="replace"))
