$conn = New-Object System.Data.SqlClient.SqlConnection('Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;')
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID('RM_ContactPersons_Get'))"
$def = $cmd.ExecuteScalar()
[System.IO.File]::WriteAllText('d:\MyProject\crm\scratch\sp_RM_ContactPersons_Get.sql', $def, [System.Text.Encoding]::UTF8)
$conn.Close()
Write-Output "Done dumping RM_ContactPersons_Get"
