param(
    [string]$ConnectionString = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=30;TrustServerCertificate=True;"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$passed = 0
$failed = 0

function Assert-True([bool]$Condition, [string]$Name) {
    if ($Condition) {
        $script:passed++
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    else {
        $script:failed++
        Write-Host "FAIL: $Name" -ForegroundColor Red
    }
}

function Read-Source([string]$RelativePath) {
    return [IO.File]::ReadAllText((Join-Path $root $RelativePath), [Text.Encoding]::UTF8)
}

$model = Read-Source "Core.Cate\Models\RM_DigitalSalesProductModel.cs"
$biz = Read-Source "Core.Cate\Biz\RM_DigitalSalesBiz.cs"
$controller = Read-Source "Modules.Cate\Areas\Cate\Controllers\DigitalSalesController.cs"
$form = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\_ProductForm.cshtml"
$list = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailProducts.cshtml"
$modal = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\_ProductModal.cshtml"
$modalCss = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\_ProductModal.css"
$detail = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\Detail.cshtml"
$detailScript = Read-Source "Modules.Cate\Areas\Cate\Views\DigitalSales\DigitalSalesDetail.js"
$registry = Read-Source "Modules.Cate\App_Data\Modules\Cate_StoredProcedures.xml"
$databaseSql = Read-Source "Database\DigitalSalesProductDetail.sql"

Assert-True ($model.Contains("List<RM_DigitalSalesProductCostModel> Costs")) "Model có danh sách chi phí"
Assert-True ($model.Contains("List<RM_DigitalSalesProductRevenueModel> Revenues")) "Model có danh sách doanh thu"
Assert-True ($model.Contains("List<RM_DigitalSalesProductMemberModel> ProductMembers")) "Model thành viên được giữ để tương thích"
Assert-True ($model.Contains("ExpectedRevenue.Value / 1000000m")) "Giá trị hiển thị chuyển sang triệu VNĐ"
Assert-True ($biz.Contains("RM_DigitalSalesProduct_SaveDetail")) "Biz dùng stored procedure lưu tổng hợp"
Assert-True ($controller.Contains("SaveProductDetail(model, User.UserName)")) "Controller lưu toàn bộ form trong một thao tác"
Assert-True ($controller.Contains("ModelState.AddModelError")) "Controller kiểm tra validation nghiệp vụ"
Assert-True (-not $form.Contains('data-add-row="member"') -and $form.Contains('data-add-row="cost"') -and $form.Contains('data-add-row="revenue"')) "Form chỉ quản lý nhiều dòng chi phí và doanh thu"
Assert-True (-not $form.Contains("ProductMembers") -and -not $list.Contains("MemberCount")) "Form và card dịch vụ không còn phần thành viên"
Assert-True (-not $controller.Contains("model.ProductMembers = _salesCache.GetProductMembers(id)") -and -not $controller.Contains('ModelState.AddModelError("ProductMembers"')) "Controller không load hoặc validate thành viên dịch vụ"
Assert-True ($biz.Contains('"<Items />"')) "Biz giữ tham số thành viên rỗng để tương thích stored procedure"
Assert-True (-not $form.Contains('nav-tabs')) "Form dịch vụ không chia tab"
Assert-True (-not $form.Contains("Contract")) "Form không chứa hợp đồng"
Assert-True (-not $form.Contains("Checklist")) "Form không chứa công việc/checklist"
Assert-True ($modal.Contains("modal-lg") -and -not $modal.Contains("modal-xl") -and $detailScript.Contains("event.preventDefault()")) "Modal lg và submit AJAX khép kín"
Assert-True ($modal.Contains('@class = "modal-content border-0 shadow-lg radius-2 overflow-hidden"') -and $modal.Contains('modal-dialog-scrollable')) "Form giữ đúng cấu trúc modal scrollable"
Assert-True ($form.Contains("product-detail-select none-select2") -and $form.Contains("product-service-select none-select2") -and $detailScript.Contains('dropdownParent: $modal')) "Dropdown modal được cô lập khỏi Select2 tự động"
Assert-True ($detailScript.Contains("scrollDigitalSalesProductRowIntoView(`$row)")) "Dòng chi tiết mới tự cuộn vào vùng nhìn thấy"
Assert-True ($form.Contains('row product-main-row') -and (($form.Split('col-md-6 form-group').Count - 1) -ge 2)) "Sản phẩm và Gói cước nằm cùng hàng 6/6"
Assert-True ($form.Contains('row product-metrics-row') -and (($form.Split('col-md-3 form-group').Count - 1) -ge 4)) "Số lượng, doanh thu dự kiến và hai thời hạn nằm cùng hàng 3/3/3/3"
Assert-True ($form.Contains('row product-note-row') -and $form.Contains('col-12 form-group mb-0')) "Ghi chú chiếm một hàng riêng"
Assert-True ([regex]::Matches($form, 'date-picker product-date-input').Count -eq 8) "Tất cả trường ngày hiện có và dòng động dùng date picker"
Assert-True ($detailScript.Contains('format: "dd/mm/yyyy"') -and $detailScript.Contains('language: "vi"') -and $detailScript.Contains("todayBtn: true")) "Date picker dùng dd/MM/yyyy và cấu hình tiếng Việt"
Assert-True ($form.Contains('product-detail-select none-select2 product-detail-select-sm') -and $modalCss.Contains('.product-detail-select-sm + .select2-container') -and $modalCss.Contains('height: 31px !important')) "Select2 Loại chi phí có chiều cao nhỏ đồng bộ input cùng hàng"
Assert-True ($detail.Contains('_ProductModal.css?v=@DateTime.Now.Ticks') -and $modalCss.Contains('.product-date-input') -and $modalCss.Contains('.datepicker.datepicker-dropdown.dropdown-menu')) "Trang Detail tải CSS modal, hiển thị icon và đưa date picker lên trên modal"
Assert-True (-not $modal.Contains('<script>') -and $detailScript.Contains('initDigitalSalesProductRuntimeForm')) "Hành vi modal sản phẩm nằm trong JavaScript chính, không phụ thuộc script inline AJAX"
Assert-True ($list.Contains("TotalCostMillion") -and $list.Contains("ProfitMillion")) "Danh sách hiển thị tổng chi phí và lợi nhuận"
Assert-True ($registry.Contains("RM_DigitalSalesProductCost_GetByProductID") -and $registry.Contains("RM_DigitalSalesProductRevenue_GetByProductID") -and $registry.Contains("RM_DigitalSalesProductMember_GetByProductID")) "Đăng ký đầy đủ stored procedure nguồn"
$saveProcedureSql = $databaseSql.Substring($databaseSql.IndexOf("ALTER PROCEDURE dbo.RM_DigitalSalesProduct_SaveDetail"), $databaseSql.IndexOf("ALTER PROCEDURE dbo.RM_DigitalSalesProduct_GetBySalesID") - $databaseSql.IndexOf("ALTER PROCEDURE dbo.RM_DigitalSalesProduct_SaveDetail"))
Assert-True (-not $saveProcedureSql.Contains("UPDATE dbo.RM_DigitalSalesProductMember") -and -not $saveProcedureSql.Contains("INSERT INTO dbo.RM_DigitalSalesProductMember")) "Lưu sản phẩm không thay đổi dữ liệu thành viên cũ"

$encodingFiles = @(
    "Core.Cate\Models\RM_DigitalSalesProductModel.cs",
    "Core.Cate\Biz\RM_DigitalSalesBiz.cs",
    "Core.Cate\Caches\RM_DigitalSalesCache.cs",
    "Modules.Cate\Areas\Cate\Controllers\DigitalSalesController.cs",
    "Modules.Cate\Areas\Cate\Views\DigitalSales\_ProductForm.cshtml",
    "Modules.Cate\Areas\Cate\Views\DigitalSales\_ProductModal.cshtml",
    "Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailProducts.cshtml",
    "Modules.Cate\App_Data\Modules\Cate_StoredProcedures.xml",
    "Database\DigitalSalesProductDetail.sql"
)
foreach ($relativePath in $encodingFiles) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $root $relativePath))
    Assert-True ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) "UTF-8 BOM: $relativePath"
}

Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
SELECT
    CASE WHEN OBJECT_ID('dbo.RM_DigitalSalesProductCost', 'U') IS NOT NULL THEN 1 ELSE 0 END AS HasCost,
    CASE WHEN OBJECT_ID('dbo.RM_DigitalSalesProductRevenue', 'U') IS NOT NULL THEN 1 ELSE 0 END AS HasRevenue,
    CASE WHEN OBJECT_ID('dbo.RM_DigitalSalesProductMember', 'U') IS NOT NULL THEN 1 ELSE 0 END AS HasMember,
    CASE WHEN OBJECT_ID('dbo.RM_DigitalSalesProduct_SaveDetail', 'P') IS NOT NULL THEN 1 ELSE 0 END AS HasSaveProcedure,
    (SELECT COUNT(1) FROM dbo.Sys_Messages WHERE LangCode = 'vi-VN' AND LabelKey IN
        ('DigitalSalesProduct_ExpectedRevenueMillion_Label', 'DigitalSalesProduct_MemberSection_Title',
         'DigitalSalesProduct_CostSection_Title', 'DigitalSalesProduct_RevenueSection_Title')) AS MessageCount;
"@
    $reader = $command.ExecuteReader()
    [void]$reader.Read()
    Assert-True ([int]$reader["HasCost"] -eq 1) "DB có bảng chi phí dịch vụ DigitalSales"
    Assert-True ([int]$reader["HasRevenue"] -eq 1) "DB có bảng doanh thu dịch vụ DigitalSales"
    Assert-True ([int]$reader["HasMember"] -eq 1) "DB có bảng thành viên dịch vụ DigitalSales"
    Assert-True ([int]$reader["HasSaveProcedure"] -eq 1) "DB có stored procedure lưu tổng hợp"
    Assert-True ([int]$reader["MessageCount"] -eq 4) "DB có các App_Message chính"
    $reader.Close()
}
finally {
    $connection.Close()
}

Write-Host "Result: $passed passed, $failed failed"
if ($failed -gt 0) { exit 1 }
