import subprocess
import sys
sys.stdout.reconfigure(encoding='utf-8')

ps = r"""$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
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
"""

with open(r"d:\SVN\crm\scratch\check_cols3.ps1", "w", encoding="utf-8") as f:
    f.write(ps)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\check_cols3.ps1"])
print(out.decode("utf-8", errors="replace"))
