# Extreme Inputs and Boundary Stress Test Suite for Milestone 1 Database Procedures
$ErrorActionPreference = "Stop"
$connStr = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " TEST SUITE 3: EXTREME INPUTS & BOUNDARY VALUE STRESS" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

function From-B64([string]$b64) {
    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($b64))
}

function Execute-SqlNonQuery([string]$sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        return $cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

function Execute-SqlScalar([string]$sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        return $cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }
}

$testSuite3Results = @()

function Record-Test($testName, $passed, $details) {
    $status = if ($passed) { "PASS" } else { "FAIL" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host ("[" + $status + "] " + $testName + " : " + $details) -ForegroundColor $color
    $script:testSuite3Results += [PSCustomObject]@{
        TestName = $testName
        Passed = $passed
        Details = $details
    }
}

function Call-InsertDoc($catId, $docName, $desc, $fileName, $origName, $path, $size, $ext, $user) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandText = "dbo.Sys_SharedDocument_Insert"
        $cmd.Parameters.AddWithValue("@CategoryId", $catId) | Out-Null
        $cmd.Parameters.AddWithValue("@DocumentName", $docName) | Out-Null
        if ($desc -ne $null) { $cmd.Parameters.AddWithValue("@Description", $desc) | Out-Null }
        $cmd.Parameters.AddWithValue("@FileName", $fileName) | Out-Null
        $cmd.Parameters.AddWithValue("@OriginalFileName", $origName) | Out-Null
        $cmd.Parameters.AddWithValue("@FilePath", $path) | Out-Null
        $cmd.Parameters.AddWithValue("@FileSize", $size) | Out-Null
        $cmd.Parameters.AddWithValue("@FileExtension", $ext) | Out-Null
        $cmd.Parameters.AddWithValue("@CreatedBy", $user) | Out-Null

        $docId = $cmd.ExecuteScalar()
        return @{ Success = $true; DocumentId = [int]$docId; Error = $null }
    }
    catch {
        return @{ Success = $false; DocumentId = -1; Error = $_.Exception.Message }
    }
    finally {
        $conn.Close()
    }
}

function Call-UpdateDoc($docId, $catId, $docName, $desc, $fileName, $origName, $path, $size, $ext, $user) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandText = "dbo.Sys_SharedDocument_Update"
        $cmd.Parameters.AddWithValue("@DocumentId", $docId) | Out-Null
        $cmd.Parameters.AddWithValue("@CategoryId", $catId) | Out-Null
        $cmd.Parameters.AddWithValue("@DocumentName", $docName) | Out-Null
        if ($desc -ne $null) { $cmd.Parameters.AddWithValue("@Description", $desc) | Out-Null }
        if ($fileName -ne $null) { $cmd.Parameters.AddWithValue("@FileName", $fileName) | Out-Null }
        if ($origName -ne $null) { $cmd.Parameters.AddWithValue("@OriginalFileName", $origName) | Out-Null }
        if ($path -ne $null) { $cmd.Parameters.AddWithValue("@FilePath", $path) | Out-Null }
        if ($size -ne $null) { $cmd.Parameters.AddWithValue("@FileSize", $size) | Out-Null }
        if ($ext -ne $null) { $cmd.Parameters.AddWithValue("@FileExtension", $ext) | Out-Null }
        $cmd.Parameters.AddWithValue("@UpdatedBy", $user) | Out-Null

        $res = $cmd.ExecuteScalar()
        return @{ Success = $true; Result = [int]$res; Error = $null }
    }
    catch {
        return @{ Success = $false; Result = -1; Error = $_.Exception.Message }
    }
    finally {
        $conn.Close()
    }
}

# --- CLEANUP PRIOR TO TESTS ---
Execute-SqlNonQuery "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy = 'extreme_tester';"

Write-Host ""
Write-Host "--- 1. String Boundary & Truncation Tests ---" -ForegroundColor Yellow

# E3.1: DocumentName exact 250 characters
$docName250 = ("A" * 240) + "1234567890"
$r1 = Call-InsertDoc 1 $docName250 "250 chars test" "name_250.pdf" "name_250.pdf" "/path/250.pdf" 1024 ".pdf" "extreme_tester"
Record-Test "E3.1: DocumentName Boundary (Exact 250 chars)" $r1.Success "Created DocId: $($r1.DocumentId)"

# E3.2: DocumentName overflow (251 characters via SP parameter vs direct INSERT)
# SP parameter truncates silently to 250 chars per T-SQL parameter specification
$docName251 = ("A" * 241) + "1234567890"
$r2 = Call-InsertDoc 1 $docName251 "251 chars test" "name_251.pdf" "name_251.pdf" "/path/251.pdf" 1024 ".pdf" "extreme_tester"
$r2Len = [int](Execute-SqlScalar "SELECT LEN(DocumentName) FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r2.DocumentId);")
# Direct INSERT on table throws truncation exception
$directTruncationCaught = $false
try {
    Execute-SqlNonQuery "INSERT INTO dbo.Sys_SharedDocument (CategoryId, DocumentName, FileName, OriginalFileName, FilePath, FileExtension, CreatedBy) VALUES (1, REPLICATE('B', 251), 'f.pdf', 'f.pdf', '/path/f.pdf', '.pdf', 'extreme_tester');"
} catch {
    $directTruncationCaught = $_.Exception.Message -like "*truncated*"
}
$p2 = ($r2Len -eq 250) -and $directTruncationCaught
Record-Test "E3.2: DocumentName Overflow (SP parameter truncated to 250; Table direct INSERT rejects >250)" $p2 "SP Stored Len: $r2Len (Expected: 250), Table Direct Truncation Caught: $directTruncationCaught"

# E3.3: FileName exact 255 characters
$fileName255 = ("F" * 251) + ".pdf"
$r3 = Call-InsertDoc 1 "FileName 255 chars test" "desc" $fileName255 $fileName255 "/path/f255.pdf" 1024 ".pdf" "extreme_tester"
Record-Test "E3.3: FileName Boundary (Exact 255 chars)" $r3.Success "Created DocId: $($r3.DocumentId)"

# E3.4: FileName overflow (256 characters)
$fileName256 = ("F" * 252) + ".pdf"
$r4 = Call-InsertDoc 1 "FileName 256 chars test" "desc" $fileName256 $fileName256 "/path/f256.pdf" 1024 ".pdf" "extreme_tester"
$r4Len = [int](Execute-SqlScalar "SELECT LEN(FileName) FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r4.DocumentId);")
$fileDirectTruncCaught = $false
try {
    Execute-SqlNonQuery "INSERT INTO dbo.Sys_SharedDocument (CategoryId, DocumentName, FileName, OriginalFileName, FilePath, FileExtension, CreatedBy) VALUES (1, 'doc', REPLICATE('F', 256), 'f.pdf', '/path/f.pdf', '.pdf', 'extreme_tester');"
} catch {
    $fileDirectTruncCaught = $_.Exception.Message -like "*truncated*"
}
$p4 = ($r4Len -eq 255) -and $fileDirectTruncCaught
Record-Test "E3.4: FileName Overflow (SP parameter truncated to 255; Table direct INSERT rejects >255)" $p4 "SP Stored Len: $r4Len (Expected: 255), Table Direct Truncation Caught: $fileDirectTruncCaught"

# E3.5: FilePath exact 500 characters
$path500 = "/Contents/Uploads/" + ("P" * 478) + ".pdf"
$r5 = Call-InsertDoc 1 "FilePath 500 chars test" "desc" "f.pdf" "f.pdf" $path500 1024 ".pdf" "extreme_tester"
Record-Test "E3.5: FilePath Boundary (Exact 500 chars)" $r5.Success "Created DocId: $($r5.DocumentId)"

# E3.6: FilePath overflow (501 characters)
$path501 = "/Contents/Uploads/" + ("P" * 479) + ".pdf"
$r6 = Call-InsertDoc 1 "FilePath 501 chars test" "desc" "f.pdf" "f.pdf" $path501 1024 ".pdf" "extreme_tester"
$r6Len = [int](Execute-SqlScalar "SELECT LEN(FilePath) FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r6.DocumentId);")
$pathDirectTruncCaught = $false
try {
    Execute-SqlNonQuery "INSERT INTO dbo.Sys_SharedDocument (CategoryId, DocumentName, FileName, OriginalFileName, FilePath, FileExtension, CreatedBy) VALUES (1, 'doc', 'f.pdf', 'f.pdf', REPLICATE('/p', 251), '.pdf', 'extreme_tester');"
} catch {
    $pathDirectTruncCaught = $_.Exception.Message -like "*truncated*"
}
$p6 = ($r6Len -eq 500) -and $pathDirectTruncCaught
Record-Test "E3.6: FilePath Overflow (SP parameter truncated to 500; Table direct INSERT rejects >500)" $p6 "SP Stored Len: $r6Len (Expected: 500), Table Direct Truncation Caught: $pathDirectTruncCaught"

# E3.7: Description with 20,000 characters (NVARCHAR(MAX))
$desc20k = "Đoạn văn bản mô tả tài liệu dài 20,000 ký tự. " * 440
$r7 = Call-InsertDoc 1 "Large Description 20K" $desc20k "desc20k.pdf" "desc20k.pdf" "/path/desc20k.pdf" 1024 ".pdf" "extreme_tester"
$retDescLen = 0
if ($r7.Success) {
    $retDescLen = [int](Execute-SqlScalar "SELECT LEN(Description) FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r7.DocumentId);")
}
Record-Test "E3.7: Large Description (20,000 chars, NVARCHAR(MAX))" ($r7.Success -and ($retDescLen -ge 20000)) "Stored and verified: $retDescLen chars"

# E3.8: Description with 100,000 characters (NVARCHAR(MAX))
$desc100k = "Tập đoàn Bưu chính Viễn thông Việt Nam VNPT. " * 2230
$r8 = Call-InsertDoc 1 "Large Description 100K" $desc100k "desc100k.pdf" "desc100k.pdf" "/path/desc100k.pdf" 1024 ".pdf" "extreme_tester"
$retDesc100Len = 0
if ($r8.Success) {
    $retDesc100Len = [int](Execute-SqlScalar "SELECT LEN(Description) FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r8.DocumentId);")
}
Record-Test "E3.8: Very Large Description (100,000 chars, NVARCHAR(MAX))" ($r8.Success -and ($retDesc100Len -ge 100000)) "Stored and verified: $retDesc100Len chars"

Write-Host ""
Write-Host "--- 2. Special Characters & SQL Injection Payloads ---" -ForegroundColor Yellow

# E3.9: SQL Injection Strings in DocumentName and Description
$sqliDocName = "'; DROP TABLE Sys_SharedDocument; -- ' OR '1'='1"
$sqliDesc = "'); EXEC xp_cmdshell 'dir'; -- /* comment */ <script>alert('xss')</script>"
$r9 = Call-InsertDoc 1 $sqliDocName $sqliDesc "sqli.pdf" "sqli.pdf" "/path/sqli.pdf" 1024 ".pdf" "extreme_tester"
$tablePreserved = [int](Execute-SqlScalar "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Sys_SharedDocument';")
$sqliStoredName = ""
if ($r9.Success) {
    $sqliStoredName = [string](Execute-SqlScalar "SELECT DocumentName FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r9.DocumentId);")
}
$p9 = $r9.Success -and ($sqliStoredName -eq $sqliDocName) -and ($tablePreserved -eq 1)
Record-Test "E3.9: SQL Injection Strings Stored Safely as Literal Text" $p9 "DocId: $($r9.DocumentId), Verified exact literal preservation"

# E3.10: Complex Symbols, Brackets, JSON, Escapes
$symName = From-B64 "fiFAIyQlXiYqKClfK3t9fDoiPD4/W11cOycsLi8="
$symDesc = From-B64 "eyAianNvbiI6IHRydWUsICJzeW1ib2xzIjogIn4hQCMkJV4mKigpIiwgInBhdGgiOiAiQzpcXFdpbmRvd3NcXFRlbXBcXGZpbGUudG1wIiB9"
$r10 = Call-InsertDoc 1 $symName $symDesc "sym.pdf" "sym.pdf" "/path/sym.pdf" 1024 ".pdf" "extreme_tester"
Record-Test "E3.10: Special Symbols, Brackets, Quotes, and JSON Escapes" $r10.Success "DocId: $($r10.DocumentId)"

Write-Host ""
Write-Host "--- 3. Complex Vietnamese Unicode & Multi-Byte Characters ---" -ForegroundColor Yellow

# E3.11: Complex Vietnamese Unicode Diacritics
$vnDocName = From-B64 "Q+G7mW5nIGjDsmEgWMOjIGjhu5lpIENo4bunIG5naMSpYSBWaeG7h3QgTmFtIC0gxJDhu5ljIGzhuq1wIC0gVOG7sSBkbyAtIEjhuqFuaCBwaMO6YyAoSOG7o3AgxJHhu5NuZyBt4bqrdSBz4buRIDIwMjYvQ0VOSVQvSMSQLVZUKQ=="
$vnDesc = From-B64 "VOG6pXQgY+G6oyBk4bqldSB0aGFuaDogaHV54buBbiAow6AsIMOoLCDDrCwgw7IsIMO5KSwgc+G6r2MgKMOhLCDDqSwgw60sIMOzLCDDuiksIGjhu49pICjhuqMsIOG6uywg4buJLCDhu48sIOG7pyksIG5nw6MgKMOjLCDhur0sIMSpLCDDtSwgxakpLCBu4bq3bmcgKOG6oSwg4bq5LCDhu4ssIOG7jSwg4bulKS4gQ2jhu68gY8OzIG3Ds2MvbcWpOiDEgywgw6IsIMSRLCDDqiwgw7QsIMahLCDGsCwg4buzLCDhu7csIOG7uSwg4bu1LCDhu4EsIOG7gywg4buFLCDhu4csIOG7kywg4buVLCDhu5csIOG7mSwg4burLCDhu60sIOG7rywg4buxLg=="
$r11 = Call-InsertDoc 1 $vnDocName $vnDesc "vn_test.pdf" "HopDong_2026.pdf" "/path/vn_test.pdf" 1024 ".pdf" "extreme_tester"
$storedVnName = ""
if ($r11.Success) {
    $storedVnName = [string](Execute-SqlScalar "SELECT DocumentName FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r11.DocumentId);")
}
$p11 = $r11.Success -and ($storedVnName -eq $vnDocName)
Record-Test "E3.11: Vietnamese Unicode Fidelity (Full diacritic preservation)" $p11 "DocId: $($r11.DocumentId), Unicode Match: $($storedVnName -eq $vnDocName)"

# E3.12: Emoji and 4-Byte UTF-16 Surrogates
$emojiName = From-B64 "VMOgaSBsaeG7h3UgYmnhu4N1IG3huqt1IPCfk4Twn5OBIHbDoCBnaeG6o2kgcGjDoXAg8J+agCAoQ2VuSVQgVE9DIPCfh7vwn4ezKQ=="
$r12 = Call-InsertDoc 1 $emojiName "Emoji test" "emoji.pdf" "emoji.pdf" "/path/emoji.pdf" 1024 ".pdf" "extreme_tester"
$storedEmojiName = ""
if ($r12.Success) {
    $storedEmojiName = [string](Execute-SqlScalar "SELECT DocumentName FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r12.DocumentId);")
}
Record-Test "E3.12: Emoji Characters in NVARCHAR" $r12.Success "DocId: $($r12.DocumentId), Stored: $storedEmojiName"

Write-Host ""
Write-Host "--- 4. Extreme FileSize (BIGINT) Boundaries ---" -ForegroundColor Yellow

# E3.13: FileSize = 0 bytes
$r13 = Call-InsertDoc 1 "Zero Byte File" "desc" "zero.pdf" "zero.pdf" "/path/zero.pdf" 0 ".pdf" "extreme_tester"
Record-Test "E3.13: FileSize = 0 bytes" $r13.Success "DocId: $($r13.DocumentId)"

# E3.14: FileSize = 50 MB (Boundary: 52,428,800 bytes)
$size50MB = 52428800
$r14 = Call-InsertDoc 1 "50MB File" "desc" "50mb.pdf" "50mb.pdf" "/path/50mb.pdf" $size50MB ".pdf" "extreme_tester"
$retSize50 = [int64](Execute-SqlScalar "SELECT FileSize FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r14.DocumentId);")
Record-Test "E3.14: FileSize = 50MB (52,428,800 bytes)" ($r14.Success -and ($retSize50 -eq $size50MB)) "Stored: $retSize50 bytes"

# E3.15: Large FileSize = 10 GB (10,737,418,240 bytes - BIGINT test)
$size10GB = 10737418240
$r15 = Call-InsertDoc 1 "10GB File" "desc" "10gb.pdf" "10gb.pdf" "/path/10gb.pdf" $size10GB ".pdf" "extreme_tester"
$retSize10G = [int64](Execute-SqlScalar "SELECT FileSize FROM dbo.Sys_SharedDocument WHERE DocumentId = $($r15.DocumentId);")
Record-Test "E3.15: Large FileSize = 10GB (10,737,418,240 bytes)" ($r15.Success -and ($retSize10G -eq $size10GB)) "Stored: $retSize10G bytes"

Write-Host ""
Write-Host "--- 5. Foreign Key Integrity Constraint ---" -ForegroundColor Yellow

# E3.16: Non-existent CategoryId (999999) -> FK violation
$r16 = Call-InsertDoc 999999 "Invalid Category Doc" "desc" "inv.pdf" "inv.pdf" "/path/inv.pdf" 1024 ".pdf" "extreme_tester"
$fkBlocked = (-not $r16.Success) -and ($r16.Error -like "*FK_Sys_SharedDocument_Category*")
Record-Test "E3.16: FK Referential Integrity (CategoryId 999999 rejected)" $fkBlocked "Error: $($r16.Error)"

Write-Host ""
Write-Host "--- 6. Update Edge Cases & File Replacement Preservation ---" -ForegroundColor Yellow

# E3.17: Partial Update with NULL File Params -> Old File Preserved
$targetDocId = $r1.DocumentId
$u1 = Call-UpdateDoc $targetDocId 1 "Updated Name (NULL Files)" "Updated Desc" $null $null $null $null $null "updater"
$docAfterU1 = Execute-SqlScalar "SELECT CONCAT(FileName, '|', OriginalFileName, '|', FilePath) FROM dbo.Sys_SharedDocument WHERE DocumentId = $targetDocId;"
$p17 = $u1.Success -and ($docAfterU1 -eq "name_250.pdf|name_250.pdf|/path/250.pdf")
Record-Test "E3.17: Partial Update with NULL Files (Original file preserved)" $p17 "Preserved FileInfo: $docAfterU1"

# E3.18: Partial Update with Empty String File Params -> Old File Preserved
$u2 = Call-UpdateDoc $targetDocId 1 "Updated Name (Empty Files)" "Updated Desc" "" "" "" 0 "" "updater"
$docAfterU2 = Execute-SqlScalar "SELECT CONCAT(FileName, '|', OriginalFileName, '|', FilePath) FROM dbo.Sys_SharedDocument WHERE DocumentId = $targetDocId;"
$p18 = $u2.Success -and ($docAfterU2 -eq "name_250.pdf|name_250.pdf|/path/250.pdf")
Record-Test "E3.18: Partial Update with Empty Strings (Original file preserved)" $p18 "Preserved FileInfo: $docAfterU2"

# E3.19: Full Update with New File Params -> File Replaced
$u3 = Call-UpdateDoc $targetDocId 2 "Updated Name (New File)" "Updated Desc" "new_file.docx" "original_new.docx" "/path/new_file.docx" 5000 ".docx" "updater"
$docAfterU3 = Execute-SqlScalar "SELECT CONCAT(FileName, '|', OriginalFileName, '|', FilePath, '|', FileSize, '|', FileExtension, '|', CategoryId) FROM dbo.Sys_SharedDocument WHERE DocumentId = $targetDocId;"
$p19 = $u3.Success -and ($docAfterU3 -eq "new_file.docx|original_new.docx|/path/new_file.docx|5000|.docx|2")
Record-Test "E3.19: Full Update with File Replacement" $p19 "Updated FileInfo: $docAfterU3"

# E3.20: Update Non-Existent DocumentId (999999)
$u4 = Call-UpdateDoc 999999 1 "Non Existent" "desc" $null $null $null $null $null "updater"
Record-Test "E3.20: Update Non-Existent DocumentId (Safe execution)" $u4.Success "Returned: $($u4.Result)"

Write-Host ""
Write-Host "--- 7. Category Referential Integrity on Delete ---" -ForegroundColor Yellow

# E3.21: Cannot delete Category 1 while documents reference it
$delCat1Res = [int](Execute-SqlScalar "EXEC dbo.Sys_DocumentCategory_Delete @CategoryId = 1, @DeletedBy = 'tester';")
$p21 = ($delCat1Res -eq -1)
Record-Test "E3.21: Delete Category with Documents Blocked (Returns -1)" $p21 "Result: $delCat1Res (Expected: -1)"

# E3.22: Insert dummy category with no documents, then delete it
$dummyCatId = [int](Execute-SqlScalar @"
EXEC dbo.Sys_DocumentCategory_InsertUpdate 
    @CategoryId = 0, 
    @CategoryName = N'Chuyên mục thử nghiệm xóa', 
    @Description = N'Dummy category for deletion test', 
    @DisplayOrder = 99, 
    @IsActive = 1, 
    @UserName = 'extreme_tester';
"@)

$delDummyRes = [int](Execute-SqlScalar "EXEC dbo.Sys_DocumentCategory_Delete @CategoryId = $dummyCatId, @DeletedBy = 'tester';")
$dummyIsDeleted = [bool](Execute-SqlScalar "SELECT IsDeleted FROM dbo.Sys_DocumentCategory WHERE CategoryId = $dummyCatId;")
$p22 = ($delDummyRes -eq 1) -and $dummyIsDeleted
Record-Test "E3.22: Delete Empty Category Succeeds (Returns 1, IsDeleted=1)" $p22 "Result: $delDummyRes, IsDeleted: $dummyIsDeleted"

# E3.23: Duplicate CategoryName Prevention (Returns -9)
$dupCatRes = [int](Execute-SqlScalar @"
EXEC dbo.Sys_DocumentCategory_InsertUpdate 
    @CategoryId = 0, 
    @CategoryName = N'Biểu mẫu Hợp đồng', 
    @Description = N'Duplicate attempt', 
    @DisplayOrder = 1, 
    @IsActive = 1, 
    @UserName = 'extreme_tester';
"@)
$p23 = ($dupCatRes -eq -9)
Record-Test "E3.23: Duplicate CategoryName Rejected (Returns -9)" $p23 "Result: $dupCatRes (Expected: -9)"

# --- CLEANUP ---
Write-Host ""
Write-Host "--- CLEANUP: Cleaning up extreme test documents and dummy category ---" -ForegroundColor Yellow
Execute-SqlNonQuery "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy = 'extreme_tester';"
Execute-SqlNonQuery "DELETE FROM dbo.Sys_DocumentCategory WHERE CategoryName = N'Chuyên mục thử nghiệm xóa';"
Write-Host "Cleanup completed." -ForegroundColor Green

# --- SUMMARY ---
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " SUITE 3 SUMMARY:" -ForegroundColor Cyan
$suite3PassedCount = ($testSuite3Results | Where-Object { $_.Passed }).Count
$suite3TotalCount = $testSuite3Results.Count
$colorSummary3 = if ($suite3PassedCount -eq $suite3TotalCount) { "Green" } else { "Red" }
Write-Host " Tests Passed: $suite3PassedCount / $suite3TotalCount" -ForegroundColor $colorSummary3
Write-Host "================================================================" -ForegroundColor Cyan

if ($suite3PassedCount -eq $suite3TotalCount) {
    Write-Host "SUITE 3 OVERALL: ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "SUITE 3 OVERALL: SOME TESTS FAILED!" -ForegroundColor Red
    exit 1
}
