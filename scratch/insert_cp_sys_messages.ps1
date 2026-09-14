$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$messages = @(
    @{ Key = "ContactPersons_Message_StatusActive"; Vi = "Đang hoạt động"; En = "Active" },
    @{ Key = "ContactPersons_Message_StatusInactive"; Vi = "Ngừng hoạt động"; En = "Inactive" },
    @{ Key = "ContactPersons_KPI_Total"; Vi = "Tổng người liên hệ"; En = "Total Contacts" },
    @{ Key = "ContactPersons_KPI_Active"; Vi = "Đang hoạt động"; En = "Active Contacts" },
    @{ Key = "ContactPersons_KPI_Inactive"; Vi = "Ngừng hoạt động"; En = "Inactive Contacts" },
    @{ Key = "ContactPersons_KPI_Customers"; Vi = "Khách hàng liên kết"; En = "Linked Customers" },
    @{ Key = "ContactPersons_Search_Customer_Option"; Vi = "-- Chọn khách hàng --"; En = "-- Select Customer --" },
    @{ Key = "ContactPersons_Search_Gender_Option"; Vi = "-- Chọn giới tính --"; En = "-- Select Gender --" },
    @{ Key = "ContactPersons_Search_Status_Option"; Vi = "-- Chọn trạng thái --"; En = "-- Select Status --" },
    @{ Key = "ContactPersons_Search_Reset"; Vi = "Đặt lại"; En = "Reset" }
)

foreach ($item in $messages) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM Sys_Messages WHERE LabelKey = @Key AND LangCode = 'vi-VN')
BEGIN
    INSERT INTO Sys_Messages (LangCode, LabelKey, Message) VALUES ('vi-VN', @Key, @Vi)
END
ELSE
BEGIN
    UPDATE Sys_Messages SET Message = @Vi WHERE LabelKey = @Key AND LangCode = 'vi-VN'
END

IF NOT EXISTS (SELECT 1 FROM Sys_Messages WHERE LabelKey = @Key AND LangCode = 'en-US')
BEGIN
    INSERT INTO Sys_Messages (LangCode, LabelKey, Message) VALUES ('en-US', @Key, @En)
END
ELSE
BEGIN
    UPDATE Sys_Messages SET Message = @En WHERE LabelKey = @Key AND LangCode = 'en-US'
END
"@
    $cmd.Parameters.AddWithValue("@Key", $item.Key) | Out-Null
    $cmd.Parameters.AddWithValue("@Vi", $item.Vi) | Out-Null
    $cmd.Parameters.AddWithValue("@En", $item.En) | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Output "Processed key: $($item.Key)"
}

$conn.Close()
Write-Output "Done updating Sys_Messages for ContactPersons!"
