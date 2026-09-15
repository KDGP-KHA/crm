try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "EXEC dbo.RM_DigitalSalesReview_GetList @ReviewBatchID = 0, @BusinessType = 0, @DepartmentID = 0, @EmployeeID = 0, @StatusID = 0, @IsReviewed = NULL, @Search = NULL, @Order = '1', @OrderDir = 'ASC', @PageIndex = 0, @PageSize = 10, @UserName = 'tan.td', @ProcessID = 0, @ProgressID = 0"
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) {
        $count++
    }
    $reader.Close()
    Write-Host "Test 1 (all 0) succeeded! Returned $count rows."

    # Test with processID = 5
    $cmd2 = $conn.CreateCommand()
    $cmd2.CommandText = "EXEC dbo.RM_DigitalSalesReview_GetList @ReviewBatchID = 0, @BusinessType = 0, @DepartmentID = 0, @EmployeeID = 0, @StatusID = 0, @IsReviewed = NULL, @Search = NULL, @Order = '1', @OrderDir = 'ASC', @PageIndex = 0, @PageSize = 10, @UserName = 'tan.td', @ProcessID = 5, @ProgressID = 0"
    $reader2 = $cmd2.ExecuteReader()
    $count2 = 0
    while ($reader2.Read()) {
        $count2++
    }
    $reader2.Close()
    Write-Host "Test 2 (ProcessID = 5) succeeded! Returned $count2 rows."

    # Test with processID = 5, progressID = 8
    $cmd3 = $conn.CreateCommand()
    $cmd3.CommandText = "EXEC dbo.RM_DigitalSalesReview_GetList @ReviewBatchID = 0, @BusinessType = 0, @DepartmentID = 0, @EmployeeID = 0, @StatusID = 0, @IsReviewed = NULL, @Search = NULL, @Order = '1', @OrderDir = 'ASC', @PageIndex = 0, @PageSize = 10, @UserName = 'tan.td', @ProcessID = 5, @ProgressID = 8"
    $reader3 = $cmd3.ExecuteReader()
    $count3 = 0
    while ($reader3.Read()) {
        $count3++
    }
    $reader3.Close()
    Write-Host "Test 3 (ProcessID = 5, ProgressID = 8) succeeded! Returned $count3 rows."

    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

