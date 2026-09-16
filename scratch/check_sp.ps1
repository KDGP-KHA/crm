try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=5;'
    $conn.Open()
    Write-Host 'DB Connected successfully!'
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT PARAMETER_NAME, DATA_TYPE, ORDINAL_POSITION FROM INFORMATION_SCHEMA.PARAMETERS WHERE SPECIFIC_NAME = 'RM_DigitalSalesReview_GetList' ORDER BY ORDINAL_POSITION"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host ($reader['ORDINAL_POSITION'].ToString() + ' : ' + $reader['PARAMETER_NAME'] + ' (' + $reader['DATA_TYPE'] + ')')
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}
