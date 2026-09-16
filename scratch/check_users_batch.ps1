try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 5 UserName, FullName, ReviewLevel FROM Sys_Users WHERE ReviewLevel IN (2, 3, 4)"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "User=$($reader['UserName']), Level=$($reader['ReviewLevel'])"
    }
    $reader.Close()

    $cmd2 = $conn.CreateCommand()
    $cmd2.CommandText = "SELECT TOP 5 ReviewBatchID, BatchName FROM RM_ReviewBatch WHERE IsDeleted = 0 ORDER BY ReviewBatchID DESC"
    $reader2 = $cmd2.ExecuteReader()
    while ($reader2.Read()) {
        Write-Host "BatchID=$($reader2['ReviewBatchID']), Name=$($reader2['BatchName'])"
    }
    $reader2.Close()

    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

