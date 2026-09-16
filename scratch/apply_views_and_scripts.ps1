$utf8WithBom = New-Object System.Text.UTF8Encoding($true)

# 1. Prepare _SearchDigitalSales.cshtml content
$searchDigitalSalesContent = @'
@using System.Linq
@using System.Web
@using System.Web.Mvc
@using TSFramework.Libs.Processors
@model Core.Cate.Models.RM_ReviewDigitalSalesSearchModel

<div class="Search card bcard border-0 shadow-sm radius-0" id="SearchReviewDigitalSales">
    <div class="card-header bgc-primary-d1">
        <div class="card-title text-white">
            <i class="fa fa-search"></i>
            &nbsp;@AppProcessor.Messagor.GetMessage("Label_Search")
        </div>
    </div>

    <div class="card-body p-2">
        <div class="d-flex flex-wrap align-items-end mb-2 search-row">
            <div class="Search_Box keyWord">
                @Html.TitleFor(model => model.Keyword, new { @class = "font-bold" })
                @Html.TextBoxFor(model => model.Keyword, new
                {
                    id = "ReviewDigitalSalesKeyword",
                    @class = "form-control",
                    placeholder = AppProcessor.Messagor.GetMessage("Customer_Keyword")
                })
            </div>

            <div class="Search_Box">
                @Html.TitleFor(model => model.ReviewBatchID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.ReviewBatchID,
                    new SelectList(Model.ReviewBatches, "ReviewBatchID", "BatchName", Model.ReviewBatchID),
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesBatchID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>

            <div class="Search_Box">
                @Html.TitleFor(model => model.DepartmentID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.DepartmentID,
                    new SelectList(Model.Departments, "BoPhan_ID", "TenBoPhanView", Model.DepartmentID),
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesDepartmentID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>

            <div class="Search_Box">
                @Html.TitleFor(model => model.EmployeeID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.EmployeeID,
                    Enumerable.Empty<SelectListItem>(),
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesEmployeeID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>
        </div>

        <div class="d-flex flex-wrap align-items-end search-row">
            <div class="Search_Box">
                @Html.TitleFor(model => model.StatusID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.StatusID,
                    Model.StatusOptions,
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesStatusID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>

            <div class="Search_Box">
                @Html.TitleFor(model => model.ProcessID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.ProcessID,
                    Model.ProcessOptions,
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesProcessID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>

            <div class="Search_Box">
                @Html.TitleFor(model => model.ProgressID, new { @class = "font-bold" })
                @Html.DropDownListFor(
                    model => model.ProgressID,
                    Model.ProgressOptions,
                    "",
                    new
                    {
                        id = "ReviewDigitalSalesProgressID",
                        @class = "form-control",
                        data_placeholder = AppProcessor.Messagor.GetMessage("Combobox_Select")
                    })
            </div>

            <div class="Search_Box review-box">
                @Html.TitleFor(model => model.IsReviewed, new { @class = "font-bold d-block" })
                <div class="d-flex align-items-center radio-group">
                    <div class="custom-control custom-radio mr-3">
                        @Html.RadioButtonFor(model => model.IsReviewed, false, new { id = "ReviewDigitalSalesIsReviewedNo", @class = "custom-control-input" })
                        @Html.Label("ReviewDigitalSalesIsReviewedNo", AppProcessor.Messagor.GetMessage("IsReviewed_No_Label"), new { @class = "custom-control-label" })
                    </div>
                    <div class="custom-control custom-radio">
                        @Html.RadioButtonFor(model => model.IsReviewed, true, new { id = "ReviewDigitalSalesIsReviewedYes", @class = "custom-control-input" })
                        @Html.Label("ReviewDigitalSalesIsReviewedYes", AppProcessor.Messagor.GetMessage("IsReviewed_Yes_Label"), new { @class = "custom-control-label" })
                    </div>
                </div>
            </div>

            <div class="Search_Box button-box">
                <button type="button" class="btn btn-primary w-100" onclick="searchReviewDigitalSales();">
                    <i class="fa fa-search-plus"></i>&nbsp;@AppProcessor.Messagor.GetMessage("Label_Search")
                </button>
            </div>
        </div>
    </div>
</div>

<script type="text/javascript">
    (function () {
        var comboboxSelect = '@Html.Raw(HttpUtility.JavaScriptStringEncode(AppProcessor.Messagor.GetMessage("Combobox_Select")))';

        function loadReviewEmployees(selectedEmployeeID) {
            var departmentID = $('#ReviewDigitalSalesDepartmentID').val() || 0;
            var $employee = $('#ReviewDigitalSalesEmployeeID');
            $employee.empty().append($('<option>').val('').text(comboboxSelect));
            $.get('@Url.Action("GetEmployeesByDepartment", "DigitalSales", new { area = "Cate" })', { departmentId: departmentID })
                .done(function (data) {
                    $.each(data || [], function (_, item) {
                        $employee.append($('<option>').val(item.Value).text(item.Text));
                    });
                    if (selectedEmployeeID) $employee.val(selectedEmployeeID);
                    $employee.trigger('chosen:updated');
                    if (selectedEmployeeID && typeof window.searchReviewDigitalSales === 'function') {
                        window.searchReviewDigitalSales();
                    }
                });
        }

        function loadReviewProcesses(selectedProcessID, selectedProgressID) {
            var statusID = $('#ReviewDigitalSalesStatusID').val() || 0;
            var $process = $('#ReviewDigitalSalesProcessID');
            var $progress = $('#ReviewDigitalSalesProgressID');
            $process.empty().append($('<option>').val('').text(comboboxSelect));
            $progress.empty().append($('<option>').val('').text(comboboxSelect));

            if (!statusID || statusID === "0") {
                return;
            }

            $.get('@Url.Action("GetProcessesByStatus", "ReviewBatchItem", new { area = "Cate" })', { statusId: statusID })
                .done(function (data) {
                    $.each(data || [], function (_, item) {
                        $process.append($('<option>').val(item.Value).text(item.Text));
                    });
                    if (selectedProcessID) {
                        $process.val(selectedProcessID);
                        loadReviewProgresses(selectedProgressID);
                    }
                });
        }

        function loadReviewProgresses(selectedProgressID) {
            var processID = $('#ReviewDigitalSalesProcessID').val() || 0;
            var $progress = $('#ReviewDigitalSalesProgressID');
            $progress.empty().append($('<option>').val('').text(comboboxSelect));

            if (!processID || processID === "0") {
                return;
            }

            $.get('@Url.Action("GetProgressesByProcess", "ReviewBatchItem", new { area = "Cate" })', { processId: processID })
                .done(function (data) {
                    $.each(data || [], function (_, item) {
                        $progress.append($('<option>').val(item.Value).text(item.Text));
                    });
                    if (selectedProgressID) {
                        $progress.val(selectedProgressID);
                    }
                });
        }

        window.loadReviewEmployees = loadReviewEmployees;
        window.loadReviewProcesses = loadReviewProcesses;
        window.loadReviewProgresses = loadReviewProgresses;

        // Lưu ý: KHÔNG truyền thẳng định danh searchReviewDigitalSales làm handler,
        // vì hàm này được định nghĩa trong ReviewBatchItem.js (nạp ở section BottomScript,
        // tức là SAU script inline này). Nếu tham chiếu trực tiếp sẽ ném ReferenceError
        // ngay khi bind, làm hỏng toàn bộ IIFE và các bind phía sau không chạy được.
        // Bọc trong hàm ẩn danh để trì hoãn việc phân giải đến lúc sự kiện thực sự xảy ra.
        $('#ReviewDigitalSalesDepartmentID').off('change.reviewDigitalSalesDept').on('change.reviewDigitalSalesDept', function () {
            loadReviewEmployees(null);
            if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
        });

        $('#ReviewDigitalSalesStatusID').off('change.reviewDigitalSalesStatus').on('change.reviewDigitalSalesStatus', function () {
            loadReviewProcesses(null, null);
            if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
        });

        $('#ReviewDigitalSalesProcessID').off('change.reviewDigitalSalesProcess').on('change.reviewDigitalSalesProcess', function () {
            loadReviewProgresses(null);
            if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
        });

        $('#ReviewDigitalSalesProgressID').off('change.reviewDigitalSalesProgress').on('change.reviewDigitalSalesProgress', function () {
            if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
        });

        $('#SearchReviewDigitalSales').off('change.reviewDigitalSales', 'select:not(#ReviewDigitalSalesDepartmentID, #ReviewDigitalSalesStatusID, #ReviewDigitalSalesProcessID, #ReviewDigitalSalesProgressID), input[type=radio]')
            .on('change.reviewDigitalSales', 'select:not(#ReviewDigitalSalesDepartmentID, #ReviewDigitalSalesStatusID, #ReviewDigitalSalesProcessID, #ReviewDigitalSalesProgressID), input[type=radio]', function () {
                if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
            });

        $('#SearchReviewDigitalSales').off('keydown.reviewDigitalSales', 'input, select').on('keydown.reviewDigitalSales', 'input, select', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                if (typeof window.searchReviewDigitalSales === 'function') window.searchReviewDigitalSales();
            }
        });
    })();
</script>
'@

# 2. Prepare ReviewBatchItem.js content
$jsFilePath = "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js"
$jsContent = [System.IO.File]::ReadAllText($jsFilePath)

# Update saveReviewDigitalSalesFilter
$oldSaveFilter = @"
function saveReviewDigitalSalesFilter() {
    var filter = {
        Keyword: $("#ReviewDigitalSalesKeyword").val(),
        ReviewBatchID: $("#ReviewDigitalSalesBatchID").val(),
        StatusID: $("#ReviewDigitalSalesStatusID").val(),
        DepartmentID: $("#ReviewDigitalSalesDepartmentID").val(),
        EmployeeID: $("#ReviewDigitalSalesEmployeeID").val(),
        IsReviewed: $('input[name="IsReviewed"]:checked').val()
    };
    localStorage.setItem(_reviewDigitalSalesFilterKey, JSON.stringify(filter));
}
"@

$newSaveFilter = @"
function saveReviewDigitalSalesFilter() {
    var filter = {
        Keyword: $("#ReviewDigitalSalesKeyword").val(),
        ReviewBatchID: $("#ReviewDigitalSalesBatchID").val(),
        StatusID: $("#ReviewDigitalSalesStatusID").val(),
        ProcessID: $("#ReviewDigitalSalesProcessID").val(),
        ProgressID: $("#ReviewDigitalSalesProgressID").val(),
        DepartmentID: $("#ReviewDigitalSalesDepartmentID").val(),
        EmployeeID: $("#ReviewDigitalSalesEmployeeID").val(),
        IsReviewed: $('input[name="IsReviewed"]:checked').val()
    };
    localStorage.setItem(_reviewDigitalSalesFilterKey, JSON.stringify(filter));
}
"@

# Update restoreReviewDigitalSalesFilter
$oldRestoreFilter = @"
function restoreReviewDigitalSalesFilter() {
    var initialBatchID = $("#ReviewDigitalSalesBatchID").val();
    if (initialBatchID && initialBatchID !== "0") return null;

    var filter;
    try {
        filter = JSON.parse(localStorage.getItem(_reviewDigitalSalesFilterKey));
    } catch (error) {
        localStorage.removeItem(_reviewDigitalSalesFilterKey);
    }
    if (!filter) return null;

    $("#ReviewDigitalSalesKeyword").val(filter.Keyword || "");
    $("#ReviewDigitalSalesBatchID").val(filter.ReviewBatchID || "");
    $("#ReviewDigitalSalesStatusID").val(filter.StatusID || "");
    $("#ReviewDigitalSalesDepartmentID").val(filter.DepartmentID || "");
    $('input[name="IsReviewed"][value="' + (filter.IsReviewed || "false") + '"]').prop("checked", true);
    return filter.EmployeeID || null;
}
"@

$newRestoreFilter = @"
function restoreReviewDigitalSalesFilter() {
    var initialBatchID = $("#ReviewDigitalSalesBatchID").val();
    if (initialBatchID && initialBatchID !== "0") return null;

    var filter;
    try {
        filter = JSON.parse(localStorage.getItem(_reviewDigitalSalesFilterKey));
    } catch (error) {
        localStorage.removeItem(_reviewDigitalSalesFilterKey);
    }
    if (!filter) return null;

    $("#ReviewDigitalSalesKeyword").val(filter.Keyword || "");
    $("#ReviewDigitalSalesBatchID").val(filter.ReviewBatchID || "");
    $("#ReviewDigitalSalesStatusID").val(filter.StatusID || "");
    $("#ReviewDigitalSalesDepartmentID").val(filter.DepartmentID || "");
    $('input[name="IsReviewed"][value="' + (filter.IsReviewed || "false") + '"]').prop("checked", true);
    return {
        employeeId: filter.EmployeeID || null,
        statusId: filter.StatusID || null,
        processId: filter.ProcessID || null,
        progressId: filter.ProgressID || null
    };
}
"@

# Update initReviewDigitalSalesTable ajax data
$oldAjaxData = @"
                data.Keyword = $("#ReviewDigitalSalesKeyword").val();
                data.ReviewBatchID = $("#ReviewDigitalSalesBatchID").val() || 0;
                data.StatusID = $("#ReviewDigitalSalesStatusID").val() || 0;
                data.DepartmentID = $("#ReviewDigitalSalesDepartmentID").val() || 0;
                data.EmployeeID = $("#ReviewDigitalSalesEmployeeID").val() || 0;
                data.IsReviewed = String($('input[name="IsReviewed"]:checked').val()).toLowerCase() === "true";
"@

$newAjaxData = @"
                data.Keyword = $("#ReviewDigitalSalesKeyword").val();
                data.ReviewBatchID = $("#ReviewDigitalSalesBatchID").val() || 0;
                data.StatusID = $("#ReviewDigitalSalesStatusID").val() || 0;
                data.ProcessID = $("#ReviewDigitalSalesProcessID").val() || 0;
                data.ProgressID = $("#ReviewDigitalSalesProgressID").val() || 0;
                data.DepartmentID = $("#ReviewDigitalSalesDepartmentID").val() || 0;
                data.EmployeeID = $("#ReviewDigitalSalesEmployeeID").val() || 0;
                data.IsReviewed = String($('input[name="IsReviewed"]:checked').val()).toLowerCase() === "true";
"@

# Update document ready
$oldReady = @"
`$(function () {
    var restoredEmployeeID = restoreReviewDigitalSalesFilter();
    if (typeof window.loadReviewEmployees === "function") window.loadReviewEmployees(restoredEmployeeID);
    initReviewDigitalSalesTable();
});
"@

$newReady = @"
`$(function () {
    var restored = restoreReviewDigitalSalesFilter();
    var restoredEmployeeID = restored ? restored.employeeId : null;
    var restoredProcessID = restored ? restored.processId : null;
    var restoredProgressID = restored ? restored.progressId : null;
    if (typeof window.loadReviewEmployees === "function") window.loadReviewEmployees(restoredEmployeeID);
    if (typeof window.loadReviewProcesses === "function") window.loadReviewProcesses(restoredProcessID, restoredProgressID);
    initReviewDigitalSalesTable();
});
"@

$jsContent = $jsContent.Replace($oldSaveFilter.Trim(), $newSaveFilter.Trim())
$jsContent = $jsContent.Replace($oldRestoreFilter.Trim(), $newRestoreFilter.Trim())
$jsContent = $jsContent.Replace($oldAjaxData.Trim(), $newAjaxData.Trim())
$jsContent = $jsContent.Replace($oldReady.Trim(), $newReady.Trim())

# Write to all 3 locations for Triple Mirroring
$viewTargets = @(
    "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml",
    "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml",
    "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml"
)

$jsTargets = @(
    "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js",
    "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js",
    "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js"
)

foreach ($target in $viewTargets) {
    $dir = [System.IO.Path]::GetDirectoryName($target)
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force }
    [System.IO.File]::WriteAllText($target, $searchDigitalSalesContent, $utf8WithBom)
    Write-Host "Wrote View (UTF-8 with BOM) -> $target"
}

foreach ($target in $jsTargets) {
    $dir = [System.IO.Path]::GetDirectoryName($target)
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force }
    [System.IO.File]::WriteAllText($target, $jsContent, $utf8WithBom)
    Write-Host "Wrote JS (UTF-8 with BOM) -> $target"
}

