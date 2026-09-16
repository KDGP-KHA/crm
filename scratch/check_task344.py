import subprocess
import sys
sys.stdout.reconfigure(encoding='utf-8')

ps = r"""$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
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
"""

with open(r"d:\SVN\crm\scratch\check_task344.ps1", "w", encoding="utf-8") as f:
    f.write(ps)

out = subprocess.check_output(["powershell", "-ExecutionPolicy", "Bypass", "-File", r"d:\SVN\crm\scratch\check_task344.ps1"])
print(out.decode("utf-8", errors="replace"))
