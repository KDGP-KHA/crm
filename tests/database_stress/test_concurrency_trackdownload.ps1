# Concurrency Stress Test for Sys_SharedDocument_TrackDownload
# Tests:
# 1. 100 concurrent download tracking calls on a single document
# 2. Interleaved concurrent calls across multiple documents (5 documents x 20 calls = 100 calls)
# 3. TrackDownload on non-existent document
# 4. TrackDownload on deleted document

$ErrorActionPreference = "Stop"
$connStr = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Max Pool Size=200;Connect Timeout=30;"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " TEST SUITE 1: Sys_SharedDocument_TrackDownload CONCURRENCY" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$csharpCode = @"
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

public class TrackDownloadResult
{
    public int WorkerId { get; set; }
    public bool Success { get; set; }
    public int ReturnedCount { get; set; }
    public long ElapsedMs { get; set; }
    public string Error { get; set; }
}

public class ConcurrencyTester
{
    public static ConcurrentBag<TrackDownloadResult> RunConcurrentCalls(string connStr, int docId, int totalCalls, int maxDegree)
    {
        var bag = new ConcurrentBag<TrackDownloadResult>();
        Parallel.For(0, totalCalls, new ParallelOptions { MaxDegreeOfParallelism = maxDegree }, i =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "dbo.Sys_SharedDocument_TrackDownload";
                        cmd.Parameters.AddWithValue("@DocumentId", docId);
                        cmd.Parameters.AddWithValue("@DownloadedBy", "worker_" + i);

                        var res = cmd.ExecuteScalar();
                        sw.Stop();
                        bag.Add(new TrackDownloadResult
                        {
                            WorkerId = i,
                            Success = true,
                            ReturnedCount = Convert.ToInt32(res),
                            ElapsedMs = sw.ElapsedMilliseconds,
                            Error = null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                bag.Add(new TrackDownloadResult
                {
                    WorkerId = i,
                    Success = false,
                    ReturnedCount = -1,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Error = ex.Message
                });
            }
        });
        return bag;
    }

    public static ConcurrentBag<TrackDownloadResult> RunInterleavedCalls(string connStr, int[] docIds, int totalCalls, int maxDegree)
    {
        var bag = new ConcurrentBag<TrackDownloadResult>();
        Parallel.For(0, totalCalls, new ParallelOptions { MaxDegreeOfParallelism = maxDegree }, i =>
        {
            int targetDocId = docIds[i % docIds.Length];
            var sw = Stopwatch.StartNew();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "dbo.Sys_SharedDocument_TrackDownload";
                        cmd.Parameters.AddWithValue("@DocumentId", targetDocId);
                        cmd.Parameters.AddWithValue("@DownloadedBy", "worker_" + i);

                        var res = cmd.ExecuteScalar();
                        sw.Stop();
                        bag.Add(new TrackDownloadResult
                        {
                            WorkerId = i,
                            Success = true,
                            ReturnedCount = Convert.ToInt32(res),
                            ElapsedMs = sw.ElapsedMilliseconds,
                            Error = null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                bag.Add(new TrackDownloadResult
                {
                    WorkerId = i,
                    Success = false,
                    ReturnedCount = -1,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Error = ex.Message
                });
            }
        });
        return bag;
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'ConcurrencyTester').Type) {
    Add-Type -TypeDefinition $csharpCode -ReferencedAssemblies "System.Data.dll"
}

function Execute-ScalarSql([string]$sql) {
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

# --- CLEANUP ANY PREVIOUS RESIDUAL TEST RECORDS ---
Execute-ScalarSql "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy IN ('stress_tester', 'multi_stress_tester');"

# --- SETUP: Insert Test Document ---
Write-Host "`n[SETUP] Creating test document for single-target concurrency..." -ForegroundColor Yellow
$doc1Id = Execute-ScalarSql @"
EXEC dbo.Sys_SharedDocument_Insert
    @CategoryId = 1,
    @DocumentName = N'Concurrency Stress Test Doc 1',
    @Description = N'Stress testing TrackDownload atomic increment',
    @FileName = N'stress_doc_1.pdf',
    @OriginalFileName = N'stress_doc_1.pdf',
    @FilePath = N'/Contents/Uploads/SharedDocuments/202609/stress_doc_1.pdf',
    @FileSize = 1048576,
    @FileExtension = '.pdf',
    @CreatedBy = 'stress_tester';
"@

Write-Host "Created DocumentId: $doc1Id" -ForegroundColor Green

# Verify initial DownloadCount
$initialCount = [int](Execute-ScalarSql "SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = $doc1Id;")
Write-Host "Initial DownloadCount: $initialCount" -ForegroundColor Green

# --- TEST 1.1: 100 Concurrent TrackDownload Calls on Single Document ---
Write-Host "`n[TEST 1.1] Launching 100 concurrent calls to Sys_SharedDocument_TrackDownload (Degree = 30)..." -ForegroundColor Yellow
$totalCalls = 100
$sw = [System.Diagnostics.Stopwatch]::StartNew()

$results = [ConcurrencyTester]::RunConcurrentCalls($connStr, $doc1Id, $totalCalls, 30)

$sw.Stop()
Write-Host "Completed $totalCalls concurrent calls in $($sw.ElapsedMilliseconds) ms" -ForegroundColor Cyan

# Analyze results
$successCount = ($results | Where-Object { $_.Success }).Count
$failCount = ($results | Where-Object { -not $_.Success }).Count
$finalDbCount = [int](Execute-ScalarSql "SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = $doc1Id;")

$successColor = if ($successCount -eq $totalCalls) { "Green" } else { "Red" }
$failColor = if ($failCount -eq 0) { "Green" } else { "Red" }
$finalCountColor = if ($finalDbCount -eq ($initialCount + $totalCalls)) { "Green" } else { "Red" }

Write-Host "Successes: $successCount / $totalCalls" -ForegroundColor $successColor
Write-Host "Failures: $failCount" -ForegroundColor $failColor
if ($failCount -gt 0) {
    $results | Where-Object { -not $_.Success } | Select-Object -First 5 | ForEach-Object {
        Write-Host "  Worker $($_.WorkerId) failed: $($_.Error)" -ForegroundColor Red
    }
}
Write-Host "Expected Final DownloadCount: $($initialCount + $totalCalls)" -ForegroundColor Cyan
Write-Host "Actual Final DB DownloadCount: $finalDbCount" -ForegroundColor $finalCountColor

# Check for duplicate returned counts
# Note: Since the SP executes:
# UPDATE dbo.Sys_SharedDocument SET DownloadCount = DownloadCount + 1 ...
# SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = @DocumentId
# in separate statements without an explicit transaction or OUTPUT clause,
# does another transaction update between UPDATE and SELECT?
$returnedCounts = $results | Where-Object { $_.Success } | ForEach-Object { $_.ReturnedCount }
$uniqueReturnedCounts = $returnedCounts | Select-Object -Unique
Write-Host "Unique Returned Counts: $($uniqueReturnedCounts.Count) / $successCount" -ForegroundColor $(if ($uniqueReturnedCounts.Count -eq $successCount) { "Green" } else { "Yellow" })

if ($uniqueReturnedCounts.Count -ne $successCount) {
    Write-Host "NOTE: $($successCount - $uniqueReturnedCounts.Count) returned counts had identical values during read after update (due to standard read uncommitted/committed behavior between separate UPDATE and SELECT statements in the SP)." -ForegroundColor Yellow
}

$test1_Passed = ($successCount -eq $totalCalls) -and ($finalDbCount -eq ($initialCount + $totalCalls))
Write-Host "Test 1.1 Result: $(if ($test1_Passed) { 'PASS (No Lost Updates: Final DB Count matches exactly)' } else { 'FAIL' })" -ForegroundColor $(if ($test1_Passed) { "Green" } else { "Red" })

# --- TEST 1.2: Interleaved Multi-Document Concurrency (5 docs x 20 calls = 100 calls) ---
Write-Host "`n[TEST 1.2] Launching interleaved concurrency across 5 documents (20 calls each)..." -ForegroundColor Yellow
$multiDocIds = @()
for ($d = 1; $d -le 5; $d++) {
    $mId = Execute-ScalarSql @"
EXEC dbo.Sys_SharedDocument_Insert
    @CategoryId = 2,
    @DocumentName = N'Interleaved Doc $d',
    @Description = N'Multi-document stress test',
    @FileName = N'multi_$d.docx',
    @OriginalFileName = N'multi_$d.docx',
    @FilePath = N'/Contents/Uploads/SharedDocuments/202609/multi_$d.docx',
    @FileSize = 2048,
    @FileExtension = '.docx',
    @CreatedBy = 'multi_stress_tester';
"@
    $multiDocIds += [int]$mId
}

Write-Host "Created 5 documents: $($multiDocIds -join ', ')" -ForegroundColor Green

$totalMultiCalls = 100 # 5 docs * 20 calls
$multiSw = [System.Diagnostics.Stopwatch]::StartNew()
$multiResults = [ConcurrencyTester]::RunInterleavedCalls($connStr, [int[]]$multiDocIds, $totalMultiCalls, 30)
$multiSw.Stop()

Write-Host "Completed $totalMultiCalls interleaved calls in $($multiSw.ElapsedMilliseconds) ms" -ForegroundColor Cyan

$multiSuccessCount = ($multiResults | Where-Object { $_.Success }).Count
Write-Host "Multi-doc Concurrency Successes: $multiSuccessCount / $totalMultiCalls" -ForegroundColor $(if ($multiSuccessCount -eq $totalMultiCalls) { "Green" } else { "Red" })

$multiDocCountsAccurate = $true
foreach ($dId in $multiDocIds) {
    $dbCnt = [int](Execute-ScalarSql "SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = $dId;")
    Write-Host "  Doc $dId DownloadCount = $dbCnt (Expected: 20)" -ForegroundColor $(if ($dbCnt -eq 20) { "Green" } else { "Red" })
    if ($dbCnt -ne 20) { $multiDocCountsAccurate = $false }
}

$test2_Passed = ($multiSuccessCount -eq $totalMultiCalls) -and $multiDocCountsAccurate
Write-Host "Test 1.2 Result: $(if ($test2_Passed) { 'PASS (All 5 documents accurately incremented by 20)' } else { 'FAIL' })" -ForegroundColor $(if ($test2_Passed) { "Green" } else { "Red" })

# --- TEST 1.3: TrackDownload on Non-Existent DocumentId ---
Write-Host "`n[TEST 1.3] TrackDownload on non-existent DocumentId (999999)..." -ForegroundColor Yellow
$nonExistentRet = Execute-ScalarSql "EXEC dbo.Sys_SharedDocument_TrackDownload @DocumentId = 999999, @DownloadedBy = 'nobody';"
$nonExistentDisp = if ($nonExistentRet -eq $null) { "NULL (as expected)" } else { $nonExistentRet }
Write-Host "Result on non-existent DocId: $nonExistentDisp" -ForegroundColor Green
$test3_Passed = ($nonExistentRet -eq $null)
Write-Host "Test 1.3 Result: $(if ($test3_Passed) { 'PASS' } else { 'FAIL' })" -ForegroundColor $(if ($test3_Passed) { "Green" } else { "Red" })

# --- TEST 1.4: TrackDownload on Soft-Deleted Document ---
Write-Host "`n[TEST 1.4] TrackDownload on soft-deleted document..." -ForegroundColor Yellow
Execute-ScalarSql "EXEC dbo.Sys_SharedDocument_Delete @DocumentId = $doc1Id, @DeletedBy = 'deleter';"
$countBefore = [int](Execute-ScalarSql "SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = $doc1Id;")
$retOnDeleted = Execute-ScalarSql "EXEC dbo.Sys_SharedDocument_TrackDownload @DocumentId = $doc1Id, @DownloadedBy = 'intruder';"
$countAfter = [int](Execute-ScalarSql "SELECT DownloadCount FROM dbo.Sys_SharedDocument WHERE DocumentId = $doc1Id;")

Write-Host "Count Before: $countBefore | Returned: $retOnDeleted | Count After: $countAfter" -ForegroundColor Cyan
$test4_Passed = ($countAfter -eq $countBefore)
Write-Host "Soft-deleted document download increment blocked: $test4_Passed" -ForegroundColor $(if ($test4_Passed) { "Green" } else { "Red" })
Write-Host "Test 1.4 Result: $(if ($test4_Passed) { 'PASS (Download count did not increment for deleted document)' } else { 'FAIL' })" -ForegroundColor $(if ($test4_Passed) { "Green" } else { "Red" })

# --- CLEANUP ---
Write-Host "`n[CLEANUP] Cleaning up test records..." -ForegroundColor Yellow
Execute-ScalarSql "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy IN ('stress_tester', 'multi_stress_tester');"
Write-Host "Cleanup completed." -ForegroundColor Green

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host " SUITE 1 SUMMARY:" -ForegroundColor Cyan
Write-Host " Test 1.1 (100 Concurrent Calls, Single Doc): $(if ($test1_Passed) { 'PASS' } else { 'FAIL' })"
Write-Host " Test 1.2 (Interleaved Multi-Doc Concurrency): $(if ($test2_Passed) { 'PASS' } else { 'FAIL' })"
Write-Host " Test 1.3 (Non-Existent DocId Handling): $(if ($test3_Passed) { 'PASS' } else { 'FAIL' })"
Write-Host " Test 1.4 (Deleted Document Protection): $(if ($test4_Passed) { 'PASS' } else { 'FAIL' })"
Write-Host "================================================================" -ForegroundColor Cyan

if ($test1_Passed -and $test2_Passed -and $test3_Passed -and $test4_Passed) {
    Write-Host "SUITE 1 OVERALL: ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "SUITE 1 OVERALL: SOME TESTS FAILED!" -ForegroundColor Red
    exit 1
}
