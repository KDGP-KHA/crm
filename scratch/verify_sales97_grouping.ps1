$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "=== VERIFY SALES 97 GROUPING SIMULATION ==="
$cmd = $conn.CreateCommand()
$cmd.CommandText = "EXEC dbo.RM_DigitalSalesTracking_GetBySalesID @DigitalSalesID = 97"
$da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$da.Fill($dt) | Out-Null
$conn.Close()

Write-Host ("Total tracking records: {0}" -f $dt.Rows.Count)

$groups = $dt.Rows | Group-Object { 
    $tl = $_["TimelineID"]
    if ($tl -is [System.DBNull] -or $null -eq $tl) { $tl = $_["TrackingID"] }
    "$tl|$($_['StatusID'])|$($_['SalesStatusName'])"
}

foreach ($g in $groups) {
    $parts = $g.Name.Split('|')
    $tlId = $parts[0]
    $stId = $parts[1]
    $stName = $parts[2]
    $realTasks = $g.Group | Where-Object { -not [string]::IsNullOrWhiteSpace($_["TaskName"]) }
    Write-Host ("`n>>> STATUS BLOCK: [{0}] (TimelineID: {1}, StatusID: {2}) - Total Tasks: {3}" -f $stName, $tlId, $stId, $realTasks.Count)
    foreach ($r in $g.Group) {
        Write-Host ("   - TrackingID: {0} | Process: {1} | TaskName: '{2}' | Status: {3}" -f $r["TrackingID"], $r["ProcessName"], $r["TaskName"], $r["Status"])
    }
}
