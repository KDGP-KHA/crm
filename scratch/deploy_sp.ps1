try {
    $conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
    $conn.Open()
    Write-Host 'Connected to DB'

    $sqlFile = "d:\VNPT\CRM-GIT\crm\Database\DigitalSalesReview.sql"
    $content = [System.IO.File]::ReadAllText($sqlFile)

    # Extract the CREATE PROCEDURE dbo.RM_DigitalSalesReview_GetList part
    $pattern = '(?s)CREATE PROCEDURE dbo\.RM_DigitalSalesReview_GetList.*?END\s*\r?\nGO'
    if ($content -match $pattern) {
        $spSql = $matches[0]
        # Remove trailing GO
        $spSql = $spSql -replace '(?i)\r?\nGO\s*$', ''

        # Drop first if exists
        $dropCmd = $conn.CreateCommand()
        $dropCmd.CommandText = "IF OBJECT_ID('dbo.RM_DigitalSalesReview_GetList', 'P') IS NOT NULL DROP PROCEDURE dbo.RM_DigitalSalesReview_GetList;"
        $dropCmd.ExecuteNonQuery()
        Write-Host 'Dropped existing SP'

        $createCmd = $conn.CreateCommand()
        $createCmd.CommandText = $spSql
        $createCmd.ExecuteNonQuery()
        Write-Host 'Successfully deployed updated RM_DigitalSalesReview_GetList!'
    } else {
        Write-Host 'Pattern for SP not matched!'
    }

    # Verify parameters
    $verifyCmd = $conn.CreateCommand()
    $verifyCmd.CommandText = "SELECT PARAMETER_NAME, DATA_TYPE, ORDINAL_POSITION FROM INFORMATION_SCHEMA.PARAMETERS WHERE SPECIFIC_NAME = 'RM_DigitalSalesReview_GetList' ORDER BY ORDINAL_POSITION"
    $reader = $verifyCmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host ($reader['ORDINAL_POSITION'].ToString() + ' : ' + $reader['PARAMETER_NAME'] + ' (' + $reader['DATA_TYPE'] + ')')
    }
    $reader.Close()

    $conn.Close()
} catch {
    Write-Host 'Error:' $_.Exception.Message
}

