[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=5;'
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT LabelKey, CAST(Message AS NVARCHAR(MAX)) AS Msg FROM Sys_Messages WHERE LabelKey IN ('DigitalSalesTracking_Process_Label', 'DigitalSalesTracking_Progress_Label', 'DigitalSales_ProcessName', 'DigitalSales_ProgressName')"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host ($reader['LabelKey'].ToString() + ' = ' + $reader['Msg'].ToString())
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

