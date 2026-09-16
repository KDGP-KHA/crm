try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=5;'
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 5 TrackingID, DigitalSalesID, ProcessID, ProgressID, TaskName FROM RM_DigitalSalesTracking"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "TrackingID=$($reader['TrackingID']), DigitalSalesID=$($reader['DigitalSalesID']), ProcessID=$($reader['ProcessID']), ProgressID=$($reader['ProgressID']), TaskName=$($reader['TaskName'])"
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

