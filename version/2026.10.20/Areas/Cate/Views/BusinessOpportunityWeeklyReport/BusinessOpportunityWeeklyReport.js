var _businessOpportunityWeeklyReportUrls = {
    getReport: "/Cate/BusinessOpportunityWeeklyReport/GetReport",
    exportReport: "/Cate/BusinessOpportunityWeeklyReport/Export",
    getEmployees: "/Cate/BusinessOpportunityWeeklyReport/GetEmployeeByDepartment"
};

$(document).ready(function () {
    initBusinessOpportunityWeeklyReportWeekFilter();
    initBusinessOpportunityWeeklyReportDepartmentFilter();
    initBusinessOpportunityWeeklyReportEmployeeFilter();

    $("#btnSearchBusinessOpportunityWeeklyReport").on("click", function () {
        searchBusinessOpportunityWeeklyReport();
    });

    $("#btnExportBusinessOpportunityWeeklyReport").on("click", function () {
        exportBusinessOpportunityWeeklyReport();
    });

    $("#frmBusinessOpportunityWeeklyReportSearch").on("keydown", "input:not(.select2-search__field)", function (e) {
        if (e.keyCode === 13) {
            e.preventDefault();
            searchBusinessOpportunityWeeklyReport();
        }
    });
});


function initBusinessOpportunityWeeklyReportDepartmentFilter() {
    if (!$.fn.select2) return;

    var $dept = $("#BoPhanID");
    if ($dept.hasClass("select2-hidden-accessible")) {
        $dept.select2("destroy");
    }
    $dept.select2({
        width: "100%",
        placeholder: "Chọn 1 giá trị",
        allowClear: true,
        dropdownParent: $("#card-business-opportunity-weekly-report")
    });
}

function parseBusinessOpportunityWeeklyReportDate(value) {
    if (!value) return null;

    var parts = value.split('/');
    if (parts.length !== 3) return null;

    var day = parseInt(parts[0], 10);
    var month = parseInt(parts[1], 10) - 1;
    var year = parseInt(parts[2], 10);
    var date = new Date(year, month, day);

    if (date.getFullYear() !== year || date.getMonth() !== month || date.getDate() !== day) {
        return null;
    }

    return date;
}

function formatBusinessOpportunityWeeklyReportDate(date) {
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    return day + '/' + month + '/' + date.getFullYear();
}

function addBusinessOpportunityWeeklyReportDays(date, days) {
    var result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    result.setDate(result.getDate() + days);
    return result;
}

function getBusinessOpportunityWeeklyReportWeekStart(date) {
    var day = date.getDay();
    var diff = day === 0 ? -6 : 1 - day;
    return addBusinessOpportunityWeeklyReportDays(date, diff);
}

function setBusinessOpportunityWeeklyReportWeek(date) {
    var weekStart = getBusinessOpportunityWeeklyReportWeekStart(date);
    var weekEnd = addBusinessOpportunityWeeklyReportDays(weekStart, 6);

    $("#FromDate").val(formatBusinessOpportunityWeeklyReportDate(weekStart));
    $("#ToDate").val(formatBusinessOpportunityWeeklyReportDate(weekEnd));
    $("#WeekDate").val(formatBusinessOpportunityWeeklyReportDate(weekStart));
    $("#WeekEndText").text(formatBusinessOpportunityWeeklyReportDate(weekEnd));
}

function initBusinessOpportunityWeeklyReportWeekFilter() {
    var initialDate = parseBusinessOpportunityWeeklyReportDate($("#FromDate").val()) || new Date();
    setBusinessOpportunityWeeklyReportWeek(initialDate);

    if ($.fn.datetimepicker) {
        $("#WeekDate").datetimepicker({
            format: "DD/MM/YYYY",
            locale: "vi"
        }).on("dp.change", function (e) {
            if (e.date) {
                setBusinessOpportunityWeeklyReportWeek(e.date.toDate());
            }
        });
    }

    $("#btnPrevWeek").on("click", function () {
        var current = parseBusinessOpportunityWeeklyReportDate($("#FromDate").val()) || new Date();
        setBusinessOpportunityWeeklyReportWeek(addBusinessOpportunityWeeklyReportDays(current, -7));
    });

    $("#btnNextWeek").on("click", function () {
        var current = parseBusinessOpportunityWeeklyReportDate($("#FromDate").val()) || new Date();
        setBusinessOpportunityWeeklyReportWeek(addBusinessOpportunityWeeklyReportDays(current, 7));
    });

    $("#WeekDate").on("change", function () {
        var selected = parseBusinessOpportunityWeeklyReportDate($(this).val());
        if (selected) {
            setBusinessOpportunityWeeklyReportWeek(selected);
        }
    });
}

function initBusinessOpportunityWeeklyReportEmployeeFilter() {
    loadBusinessOpportunityWeeklyReportEmployees($("#BoPhanID").val(), $("#EmployeeIDs").val());

    $("#BoPhanID").on("change", function () {
        $("#EmployeeIDs").val("");
        loadBusinessOpportunityWeeklyReportEmployees($(this).val(), "");
    });
}

function loadBusinessOpportunityWeeklyReportEmployees(boPhanId, selectedIds) {
    $.ajax({
        url: _businessOpportunityWeeklyReportUrls.getEmployees,
        type: "GET",
        dataType: "json",
        data: {
            boPhanId: boPhanId
        },
        success: function (items) {
            var $employeeSelect = $("#EmployeeSelect");
            var selectedValues = selectedIds ? selectedIds.split(";") : [];

            if ($employeeSelect.hasClass("select2-hidden-accessible")) {
                $employeeSelect.select2("destroy");
            }

            $employeeSelect.empty();
            $.each(items || [], function (_, item) {
                var option = new Option(item.Text, item.Value, false, selectedValues.indexOf(item.Value) >= 0);
                $employeeSelect.append(option);
            });

            if ($.fn.select2) {
                $employeeSelect.select2({
                    width: "100%",
                    placeholder: "Tất cả nhân viên",
                    allowClear: true,
                    closeOnSelect: false
                });
            }

            syncBusinessOpportunityWeeklyReportEmployeeIds();

            $employeeSelect.off("change").on("change", function () {
                syncBusinessOpportunityWeeklyReportEmployeeIds();
            });
        }
    });
}

function syncBusinessOpportunityWeeklyReportEmployeeIds() {
    var values = $("#EmployeeSelect").val() || [];
    $("#EmployeeIDs").val(values.join(";"));
}

function searchBusinessOpportunityWeeklyReport() {
    syncBusinessOpportunityWeeklyReportEmployeeIds();

    $.ajax({
        url: _businessOpportunityWeeklyReportUrls.getReport,
        type: "POST",
        cache: false,
        data: $("#frmBusinessOpportunityWeeklyReportSearch").serialize(),
        success: function (html) {
            $("#BusinessOpportunityWeeklyReportContainer").html(html);
        }
    });
}

function exportBusinessOpportunityWeeklyReport() {
    syncBusinessOpportunityWeeklyReportEmployeeIds();
    window.location = _businessOpportunityWeeklyReportUrls.exportReport + "?" + $("#frmBusinessOpportunityWeeklyReportSearch").serialize();
}