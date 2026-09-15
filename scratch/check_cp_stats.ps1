$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "
SELECT 
    COUNT(*) AS TotalContacts,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS ActiveContacts,
    SUM(CASE WHEN Status = 0 OR Status IS NULL THEN 1 ELSE 0 END) AS InactiveContacts,
    COUNT(DISTINCT cc.CustomerID) AS TotalCustomers
FROM RM_ContactPersons cp
LEFT JOIN RM_CustomerContact cc ON cp.ContactPerson_ID = cc.ContactPersonID AND cc.IsDeleted = 0
WHERE cp.IsDeleted = 0
"
$da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
$da.Fill($dt) | Out-Null
$dt | Format-Table -AutoSize
$conn.Close()
