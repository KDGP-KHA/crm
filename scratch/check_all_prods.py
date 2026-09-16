import subprocess
import sys
sys.stdout.reconfigure(encoding='utf-8')

ps = r"""$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 20 SalesProductID, DigitalSalesID, ExpectedRevenue, ActualRevenue FROM dbo.RM_DigitalSalesProduct ORDER BY SalesProductID DESC"
$r = $cmd.ExecuteReader()
while ($r.Read()) {
    Write-Host "ProdID: $($r[0]) | SalesID: $($r[1]) | Expected: $($r[2]) | Actual: $($r[3])"
}
$r.Close()
$conn.Close()
"""

with open(r"d:\SVN\crm\scratch\check_all_prods.ps1", "w", encoding="utf-8") as f:
    f.write(ps)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\check_all_prods.ps1"])
print(out.decode("utf-8", errors="replace"))
