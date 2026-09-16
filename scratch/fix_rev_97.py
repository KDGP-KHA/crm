import subprocess
import sys
sys.stdout.reconfigure(encoding='utf-8')

ps = r"""$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# 1. Update Product 125 and 126
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE dbo.RM_DigitalSalesProduct SET ExpectedRevenue = 15000000 WHERE SalesProductID = 125"
$cmd.ExecuteNonQuery()

$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "UPDATE dbo.RM_DigitalSalesProduct SET ExpectedRevenue = 50486000 WHERE SalesProductID = 126"
$cmd2.ExecuteNonQuery()

# 2. Update TotalExpectedRevenue in RM_DigitalSales
$cmd3 = $conn.CreateCommand()
$cmd3.CommandText = "UPDATE dbo.RM_DigitalSales SET TotalExpectedRevenue = 65486000 WHERE DigitalSalesID = 97"
$cmd3.ExecuteNonQuery()

Write-Host "Updated SalesID 97 revenues successfully!"

# 3. Check any other records in RM_DigitalSales with absurdly large numbers (> 1,000,000,000,000)
$cmd4 = $conn.CreateCommand()
$cmd4.CommandText = "SELECT DigitalSalesID, Code, TotalExpectedRevenue FROM dbo.RM_DigitalSales WHERE TotalExpectedRevenue > 1000000000000"
$r4 = $cmd4.ExecuteReader()
while ($r4.Read()) {
    Write-Host "Other huge record: SalesID $($r4[0]) | Code: $($r4[1]) | Revenue: $($r4[2])"
}
$r4.Close()

$conn.Close()
"""

with open(r"d:\SVN\crm\scratch\fix_rev_97.ps1", "w", encoding="utf-8") as f:
    f.write(ps)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\fix_rev_97.ps1"])
print(out.decode("utf-8", errors="replace"))
