try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT ds.DigitalSalesID, ds.Code, ds.StatusID, t.TrackingID, t.ProcessID, t.ProgressID FROM RM_DigitalSales ds LEFT JOIN RM_DigitalSalesTracking t ON t.DigitalSalesID = ds.DigitalSalesID WHERE ds.Code IN ('SPDV-2026-0069', 'SPDV-2026-0023', 'SPDV-2026-0068', 'SPDV-2026-0070')"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "Code=$($reader['Code']), StatusID=$($reader['StatusID']), ProcessID=$($reader['ProcessID']), ProgressID=$($reader['ProgressID'])"
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

