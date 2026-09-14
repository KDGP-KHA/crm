$connStr = 'Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = 'SELECT OBJECT_DEFINITION(OBJECT_ID(''RM_DigitalSalesActivity_UpdateLatestStatusChangeAttachments''))'
$res = $cmd.ExecuteScalar()
if ($res) {
    Write-Host 'SP RM_DigitalSalesActivity_UpdateLatestStatusChangeAttachments exists! Length:' $res.Length
} else {
    Write-Host 'SP RM_DigitalSalesActivity_UpdateLatestStatusChangeAttachments DOES NOT EXIST!'
}

$cmd.CommandText = 'SELECT OBJECT_DEFINITION(OBJECT_ID(''RM_DigitalSales_ChangeStatus''))'
$res2 = $cmd.ExecuteScalar()
if ($res2) {
    Write-Host 'RM_DigitalSales_ChangeStatus exists! Length:' $res2.Length
    Write-Host "Contains AttachmentPath:" ($res2.Contains('@AttachmentPath'))
}

$conn.Close()
