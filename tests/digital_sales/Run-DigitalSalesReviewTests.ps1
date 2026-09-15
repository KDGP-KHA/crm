param(
    [string]$WorkspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
$passed = 0
$failed = 0

function Assert-ReviewTest([bool]$Condition, [string]$Message) {
    if ($Condition) {
        $script:passed++
        Write-Host "[PASS] $Message" -ForegroundColor Green
    }
    else {
        $script:failed++
        Write-Host "[FAIL] $Message" -ForegroundColor Red
    }
}

$relativeViews = @(
    'DigitalSales/Detail.cshtml',
    'DigitalSales/DigitalSalesDetail.js',
    'DigitalSales/_DetailTracking.cshtml',
    'ReviewBatchItem/Index.cshtml',
    'ReviewBatchItem/ReviewBatchItem.js',
    'ReviewBatchItem/ReviewBatchItem.css',
    'ReviewBatchItem/_DigitalSales.cshtml',
    'ReviewBatchItem/_SearchDigitalSales.cshtml',
    'ReviewBatchItem/_ReviewBatch.cshtml',
    'ReviewBatchItem/_ReviewForm.cshtml',
    'ReviewBatchItem/_ReviewHistory.cshtml',
    'ReviewReport/Index.cshtml'
)

foreach ($relativeView in $relativeViews) {
    $paths = @(
        (Join-Path $WorkspaceRoot "Modules.Cate/Areas/Cate/Views/$relativeView"),
        (Join-Path $WorkspaceRoot "CenIT.Solution.TOC.WebApp/Areas/Cate/Views/$relativeView"),
        (Join-Path $WorkspaceRoot "publish_source/Areas/Cate/Views/$relativeView")
    )
    $hashes = $paths | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
    Assert-ReviewTest (($hashes | Select-Object -Unique).Count -eq 1) "Triple mirror: $relativeView"

    $hasBom = $true
    foreach ($path in $paths) {
        $bytes = [IO.File]::ReadAllBytes($path)
        $hasBom = $hasBom -and $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
    }
    Assert-ReviewTest $hasBom "UTF-8 BOM: $relativeView"
}

$reviewIndex = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/Index.cshtml'))
$detailView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/DigitalSales/Detail.cshtml'))
$detailScript = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/DigitalSales/DigitalSalesDetail.js'))
$reviewForm = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/_ReviewForm.cshtml'))
$reviewBatchView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/_ReviewBatch.cshtml'))
$reviewListScript = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/ReviewBatchItem.js'))
$reviewSearchView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/_SearchDigitalSales.cshtml'))
$reviewListView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/_DigitalSales.cshtml'))
$reviewModel = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Core.Cate/Models/RM_ReviewBatchItemModel.cs'))
$reviewDatabase = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Database/DigitalSalesReview.sql'))
$detailTrackingView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/DigitalSales/_DetailTracking.cshtml'))
$reviewHistoryView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/_ReviewHistory.cshtml'))
$reviewController = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Controllers/ReviewBatchItemController.cs'))
$reviewBiz = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Core.Cate/Biz/RM_ReviewBatchItemBiz.cs'))
$reportModel = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Core.Cate/Models/ReviewReportModel.cs'))
$reportView = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewReport/Index.cshtml'))
$reportController = [IO.File]::ReadAllText((Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Controllers/ReviewReportController.cs'))
Assert-ReviewTest ($reviewIndex.Contains('_DigitalSales') -and -not $reviewIndex.Contains('_Project') -and -not $reviewIndex.Contains('_BusinessOpportunity')) 'Màn hình rà soát chỉ còn DigitalSales'
Assert-ReviewTest ($detailView.Contains('tab-review-history') -and $detailView.Contains('reviewFormPane') -and $detailView.Contains('reviewBatchAutoTrigger')) 'Chi tiết có tab lịch sử và panel rà soát'
Assert-ReviewTest ($reviewForm.Contains('DigitalSalesID') -and -not $reviewForm.Contains('ObjectType') -and -not $reviewForm.Contains('ObjectID')) 'Form mới chỉ nhận DigitalSalesID'
Assert-ReviewTest ($detailView.IndexOf('ckfinder/ckfinder.js') -ge 0 -and $detailView.IndexOf('ckfinder/ckfinder.js') -lt $detailView.IndexOf('ckeditor4/ckeditor.js')) 'CKFinder được nạp trước CKEditor'
Assert-ReviewTest ($reviewBatchView.Contains('.modal-header [data-dismiss="modal"], .modal-footer [data-dismiss="modal"]') -and $reviewBatchView.Contains('keyboard: false')) 'Panel rà soát loại bỏ nút đóng và khóa phím Escape'
Assert-ReviewTest ($reviewListScript.Contains('function buildReviewDigitalSalesUrl(row)') -and $reviewListScript.Contains('if (!row.IsReviewed && batchID > 0)')) 'Tên và nút hành động dùng URL rà soát theo trạng thái'
Assert-ReviewTest (([regex]::Matches($reviewSearchView, 'search-row')).Count -eq 2 -and $reviewSearchView.Contains('button-box')) 'Search DigitalSales dùng bố cục hai hàng như rà soát cũ'
Assert-ReviewTest (-not $reviewSearchView.Contains('ReviewDigitalSalesBusinessType') -and -not $reviewSearchView.Contains('resetReviewDigitalSales')) 'Search không còn Loại hình và nút Đặt lại'
Assert-ReviewTest (-not $reviewListScript.Contains('data.BusinessType') -and -not $reviewListScript.Contains('ReviewDigitalSalesBusinessType')) 'Request và state không còn bộ lọc Loại hình'
Assert-ReviewTest (([regex]::Matches($reviewListView, '<th>')).Count -eq 6 -and -not $reviewListView.Contains('ReviewDigitalSales_BusinessType_Label') -and -not $reviewListView.Contains('ReviewDigitalSales_Status_Label')) 'Bảng bỏ hai cột Loại hình và Trạng thái riêng'
Assert-ReviewTest ($reviewListScript.Contains('renderReviewDigitalSalesRecord') -and $reviewListScript.Contains('row.StatusName') -and $reviewListScript.Contains('row.ProductServiceNames') -and $reviewListScript.Contains('row.IsKeyProject') -and $reviewListScript.Contains('row.IsFollowed')) 'Cột hồ sơ hiển thị trạng thái, mã, huy hiệu và dịch vụ'
Assert-ReviewTest (-not $reviewListScript.Contains('{ data: "BusinessTypeName"') -and -not $reviewListScript.Contains('{ data: "StatusName"')) 'DataTable không còn cột Loại hình/Trạng thái độc lập'
Assert-ReviewTest ($reviewSearchView.Contains('ReviewDigitalSalesStatusID') -and $reviewSearchView.Contains('ReviewDigitalSalesIsReviewedNo') -and $reviewSearchView.Contains('ReviewDigitalSalesIsReviewedYes')) 'Bộ lọc Trạng thái và tình trạng rà soát được giữ nguyên'
Assert-ReviewTest ($reviewListScript.Contains('data.IsReviewed =') -and $reviewListScript.Contains('IsReviewed: $(') -and $reviewDatabase.Contains('WHERE @IsReviewed IS NULL OR IsReviewed = @IsReviewed')) 'Chuỗi lọc IsReviewed từ UI đến DB không thay đổi'
Assert-ReviewTest ($reviewModel.Contains('bool IsKeyProject') -and $reviewModel.Contains('bool IsFollowed') -and $reviewModel.Contains('string ProductServiceNames')) 'Model có dữ liệu hiển thị mở rộng cho cột hồ sơ'
Assert-ReviewTest ($reviewDatabase.Contains("('ReviewDigitalSales_Column_Record', N'Hồ sơ KD sản phẩm DVS')")) 'Script DB cập nhật đúng tiêu đề cột'
Assert-ReviewTest (-not $detailTrackingView.Contains('@if (realTaskCount == 0)') -and $detailTrackingView.Contains('if (realTaskCount == 0)')) 'Razor tracking không có tiền tố @ thừa trong block foreach'
Assert-ReviewTest ($reviewForm.Contains('RadioButtonFor(model => model.ReviewConclusion') -and $reviewForm.Contains('review-conclusion-options') -and $reviewForm.Contains('ValidationMessageFor(model => model.ReviewConclusion')) 'Form có radio kết luận và validation inline'
Assert-ReviewTest ($reviewForm.Contains("items: ['Undo', 'Redo']") -and $reviewForm.Contains("items: ['Image', 'Table']") -and -not $reviewForm.Contains("items: ['Source'")) 'CKEditor chỉ giữ nhóm công cụ thường dùng'
Assert-ReviewTest ($reviewController.Contains('ReviewConclusion = RM_ReviewConclusion.Accepted')) 'Form rà soát mới mặc định Chấp nhận'
Assert-ReviewTest ($reviewController.Contains('ValidateReviewConclusion(model)') -and $reviewController.Contains('ReviewConclusion_Required') -and $reviewController.Contains('BuildReviewConclusionOptions')) 'Controller validate kết luận khi xác nhận'
Assert-ReviewTest ($reviewModel.Contains('byte? ReviewConclusion') -and $reviewModel.Contains('byte? FinalReviewConclusion') -and $reviewModel.Contains('class RM_ReviewConclusion')) 'Model có kết luận lịch sử và kết luận cuối'
Assert-ReviewTest ($reviewBiz.Contains('model.ReviewConclusion') -and $reviewDatabase.Contains('@ReviewConclusion TINYINT')) 'Biz và stored procedure truyền kết luận'
Assert-ReviewTest ($reviewHistoryView.Contains('item.ReviewConclusion.HasValue') -and $reviewListScript.Contains('row.FinalReviewConclusion')) 'Lịch sử và danh sách hiển thị kết luận'
Assert-ReviewTest ($reviewHistoryView.Contains('review-history-batch-header') -and $reviewHistoryView.Contains('ReviewConclusion_Final_Label') -and $reviewHistoryView.Contains('review-history-entry')) 'Tab lịch sử có tổng hợp đợt, kết luận cuối và card từng lượt'
Assert-ReviewTest ($reviewHistoryView.Contains('review-history-preview') -and $reviewHistoryView.Contains("off('click.reviewHistory'") -and -not $reviewHistoryView.Contains('preview-image')) 'Nội dung lịch sử thu gọn và không render ảnh lặp'
Assert-ReviewTest ($detailScript.Contains("#reviewHistoryContainer .review-history-entry")) 'Badge tab đếm theo card lịch sử mới'
Assert-ReviewTest ($reviewDatabase.Contains('ORDER BY history.ReviewLevel ASC, history.CreatedDate DESC, history.ReviewHistoryID DESC')) 'Kết luận cuối ưu tiên cấp cao nhất và lần mới nhất'
Assert-ReviewTest ($reportModel.Contains('FinalReviewConclusion') -and $reportView.Contains('/Cate/DigitalSales/Detail/') -and $reportController.Contains('GetConclusionText')) 'Báo cáo web và Excel dùng DigitalSales và kết luận cuối'
Assert-ReviewTest (-not $reviewDatabase.Contains('UPDATE dbo.RM_DigitalSalesFollow') -and -not $reviewController.Contains('ToggleFollow')) 'Kết luận Quan tâm không thay đổi cờ theo dõi'

$procedureRegistries = @(
    (Join-Path $WorkspaceRoot 'Modules.Cate/App_Data/Modules/Cate_StoredProcedures.xml'),
    (Join-Path $WorkspaceRoot 'CenIT.Solution.TOC.WebApp/App_Data/Modules/Cate_StoredProcedures.xml')
)
$registryHashes = $procedureRegistries | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
Assert-ReviewTest (($registryHashes | Select-Object -Unique).Count -eq 1) 'Registry stored procedure đồng bộ Module/WebApp'
foreach ($procedureName in @('RM_DigitalSalesReview_GetList', 'RM_DigitalSalesReview_Save', 'RM_DigitalSalesReview_GetHistory')) {
    $registeredEverywhere = $true
    foreach ($registryPath in $procedureRegistries) {
        $registeredEverywhere = $registeredEverywhere -and [IO.File]::ReadAllText($registryPath).Contains($procedureName)
    }
    Assert-ReviewTest $registeredEverywhere "Registry có $procedureName"
}

$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) {
    & $node.Source --check (Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/ReviewBatchItem/ReviewBatchItem.js')
    Assert-ReviewTest ($LASTEXITCODE -eq 0) 'JavaScript danh sách hợp lệ'
    & $node.Source --check (Join-Path $WorkspaceRoot 'Modules.Cate/Areas/Cate/Views/DigitalSales/DigitalSalesDetail.js')
    Assert-ReviewTest ($LASTEXITCODE -eq 0) 'JavaScript chi tiết hợp lệ'
}

$config = [xml](Get-Content -Raw (Join-Path $WorkspaceRoot 'CenIT.Solution.TOC.WebApp/Web.config'))
$connectionString = ($config.configuration.connectionStrings.add | Where-Object name -eq 'TOC.Conn.Major').connectionString
$connection = New-Object Data.SqlClient.SqlConnection $connectionString
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
SELECT COUNT(1) FROM sys.procedures
WHERE name IN ('RM_DigitalSalesReview_GetList', 'RM_DigitalSalesReview_Save', 'RM_DigitalSalesReview_GetHistory');
SELECT COUNT(1) FROM sys.parameters
WHERE object_id IN (OBJECT_ID('dbo.RM_DigitalSalesReview_GetList'), OBJECT_ID('dbo.RM_DigitalSalesReview_Save'), OBJECT_ID('dbo.RM_DigitalSalesReview_GetHistory'))
  AND name = '@ObjectType';
SELECT COUNT(1) FROM dbo.Sys_Messages
WHERE LangCode = 'vi-VN' AND LabelKey LIKE 'ReviewDigitalSales_%';
SELECT COUNT(1) FROM dbo.Sys_Messages
WHERE LangCode = 'vi-VN' AND LabelKey = 'ReviewDigitalSales_Column_Record' AND CONVERT(NVARCHAR(MAX), Message) = N'Hồ sơ KD sản phẩm DVS';
SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.RM_DigitalSalesReview_GetList')) LIKE '%ProductServiceNames%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_DigitalSalesReview_GetList')) LIKE '%IsKeyProject%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_DigitalSalesReview_GetList')) LIKE '%IsFollowed%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_DigitalSalesReview_GetList')) LIKE '%WHERE @IsReviewed IS NULL OR IsReviewed = @IsReviewed%'
            THEN 1 ELSE 0 END;
SELECT CASE WHEN COL_LENGTH('dbo.RM_ReviewHistory', 'ReviewConclusion') = 1
             AND OBJECT_ID('dbo.CK_RM_ReviewHistory_ReviewConclusion', 'C') IS NOT NULL
            THEN 1 ELSE 0 END;
SELECT COUNT(1) FROM dbo.Sys_Messages
WHERE LangCode = 'vi-VN' AND LabelKey LIKE 'ReviewConclusion_%';
SELECT CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.RM_Review_Report_Get')) LIKE '%RM_DigitalSales%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_Review_Report_Get')) LIKE '%FinalReviewConclusion%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_Review_Report_Get')) NOT LIKE '%RM_BusinessOpportunity%'
             AND OBJECT_DEFINITION(OBJECT_ID('dbo.RM_Review_Report_Get')) NOT LIKE '%RM_Project%'
            THEN 1 ELSE 0 END;
"@
    $adapter = New-Object Data.SqlClient.SqlDataAdapter $command
    $dataSet = New-Object Data.DataSet
    [void]$adapter.Fill($dataSet)
    Assert-ReviewTest ([int]$dataSet.Tables[0].Rows[0][0] -eq 3) 'Đủ ba stored procedure DigitalSales review'
    Assert-ReviewTest ([int]$dataSet.Tables[1].Rows[0][0] -eq 0) 'Stored procedure mới không nhận ObjectType'
    Assert-ReviewTest ([int]$dataSet.Tables[2].Rows[0][0] -ge 27) 'App_Message DigitalSales review đầy đủ'
    Assert-ReviewTest ([int]$dataSet.Tables[3].Rows[0][0] -eq 1) 'DB Demo có tiêu đề Hồ sơ KD sản phẩm DVS'
    Assert-ReviewTest ([int]$dataSet.Tables[4].Rows[0][0] -eq 1) 'Stored procedure trả dữ liệu cột hồ sơ và giữ lọc IsReviewed'
    Assert-ReviewTest ([int]$dataSet.Tables[5].Rows[0][0] -eq 1) 'DB có cột và ràng buộc miền kết luận'
    Assert-ReviewTest ([int]$dataSet.Tables[6].Rows[0][0] -ge 8) 'DB có đầy đủ message kết luận'
    Assert-ReviewTest ([int]$dataSet.Tables[7].Rows[0][0] -eq 1) 'Báo cáo DB đã chuyển sang DigitalSales và trả kết luận cuối'
}
finally {
    $connection.Close()
}

Write-Host "DigitalSales Review tests: $passed PASS, $failed FAIL"
if ($failed -gt 0) { exit 1 }
