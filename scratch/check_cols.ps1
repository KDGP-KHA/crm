 = 'Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;'
 = New-Object System.Data.SqlClient.SqlConnection()
.Open()
 = .CreateCommand()
.CommandText = 'SELECT TOP 1 * FROM dbo.Sys_Messages'
 = .ExecuteReader()
for ( = 0;  -lt .FieldCount; ++) { Write-Host .GetName() }
.Close()
.Close()
