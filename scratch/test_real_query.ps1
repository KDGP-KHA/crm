try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "EXEC dbo.RM_DigitalSalesReview_GetList @ReviewBatchID = 24, @BusinessType = 0, @DepartmentID = 0, @EmployeeID = 0, @StatusID = 0, @IsReviewed = NULL, @Search = NULL, @Order = '1', @OrderDir = 'ASC', @PageIndex = 0, @PageSize = 10, @UserName = 'bangpl.kha', @ProcessID = 0, @ProgressID = 0"
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) {
        $count++
        Write-Host "Title: $($reader['Title']), Code: $($reader['Code'])"
    }
    $reader.Close()
    Write-Host "Test (BatchID=24, User=bangpl.kha) returned $count rows."

    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

