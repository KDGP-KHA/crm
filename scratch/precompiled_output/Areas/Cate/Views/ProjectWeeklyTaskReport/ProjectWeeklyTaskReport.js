var _ProjectWeeklyTaskReportUrls = {
    getReport: "/Cate/ProjectWeeklyTaskReport/GetReport",
    exportReport: "/Cate/ProjectWeeklyTaskReport/Export",
};

$(document).ready(function () {
    initProjectWeeklyTaskReportWeekFilter();

    $("#btnSearchProjectWeeklyTaskReport").on("click", function () {
        searchProjectWeeklyTaskReport();
    });

    $("#btnExportProjectWeeklyTaskReport").on("click", function () {
        exportProjectWeeklyTaskReport();
    });

    $("#frmProjectWeeklyTaskReportSearch").on("keydown", "input:not(.select2-search__field)", function (e) {
        if (e.keyCode === 13) {
            e.preventDefault();
            searchProjectWeeklyTaskReport();
        }
    });
});

$(document).off("click", ".btn-toggle-timeline");

$(document).on("click", ".btn-toggle-timeline", function () {

    var target = $(this).data("target");

    var rows = $(".extra-" + target);

    if (rows.is(":visible")) {

        rows.hide();

        $(this).text(
            "Xem thêm (" + rows.length + ")"
        );
    }
    else {

        rows.show();

        $(this).text("Thu gọn");
    }
});


function parseProjectWeeklyTaskReportDate(value) {
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

function formatProjectWeeklyTaskReportDate(date) {
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    return day + '/' + month + '/' + date.getFullYear();
}

function addProjectWeeklyTaskReportDays(date, days) {
    var result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    result.setDate(result.getDate() + days);
    return result;
}

function getProjectWeeklyTaskReportWeekStart(date) {
    var day = date.getDay();
    var diff = day === 0 ? -6 : 1 - day;
    return addProjectWeeklyTaskReportDays(date, diff);
}

function setProjectWeeklyTaskReportWeek(date) {
    var weekStart = getProjectWeeklyTaskReportWeekStart(date);
    var weekEnd = addProjectWeeklyTaskReportDays(weekStart, 6);

    $("#FromDate").val(formatProjectWeeklyTaskReportDate(weekStart));
    $("#ToDate").val(formatProjectWeeklyTaskReportDate(weekEnd));
    $("#WeekDate").val(formatProjectWeeklyTaskReportDate(weekStart));
    $("#WeekEndText").text(formatProjectWeeklyTaskReportDate(weekEnd));
}

function initProjectWeeklyTaskReportWeekFilter() {
    var initialDate = parseProjectWeeklyTaskReportDate($("#FromDate").val()) || new Date();
    setProjectWeeklyTaskReportWeek(initialDate);

    if ($.fn.datetimepicker) {
        $("#WeekDate").datetimepicker({
            format: "DD/MM/YYYY",
            locale: "vi"
        }).on("dp.change", function (e) {
            if (e.date) {
                setProjectWeeklyTaskReportWeek(e.date.toDate());
            }
        });
    }

    $("#btnPrevWeek").on("click", function () {
        var current = parseProjectWeeklyTaskReportDate($("#FromDate").val()) || new Date();
        setProjectWeeklyTaskReportWeek(addProjectWeeklyTaskReportDays(current, -7));
    });

    $("#btnNextWeek").on("click", function () {
        var current = parseProjectWeeklyTaskReportDate($("#FromDate").val()) || new Date();
        setProjectWeeklyTaskReportWeek(addProjectWeeklyTaskReportDays(current, 7));
    });

    $("#WeekDate").on("change", function () {
        var selected = parseProjectWeeklyTaskReportDate($(this).val());
        if (selected) {
            setProjectWeeklyTaskReportWeek(selected);
        }
    });
}

function loadProjectWeeklyTaskReportEmployees(boPhanId, selectedIds) {
    $.ajax({
        url: _ProjectWeeklyTaskReportUrls.getEmployees,
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

            syncProjectWeeklyTaskReportEmployeeIds();

            $employeeSelect.off("change").on("change", function () {
                syncProjectWeeklyTaskReportEmployeeIds();
            });
        }
    });
}

function syncProjectWeeklyTaskReportEmployeeIds() {
    var values = $("#EmployeeSelect").val() || [];
    $("#EmployeeIDs").val(values.join(";"));
}

function searchProjectWeeklyTaskReport() {
    syncProjectWeeklyTaskReportEmployeeIds();

    $.ajax({
        url: _ProjectWeeklyTaskReportUrls.getReport,
        type: "POST",
        cache: false,
        data: $("#frmProjectWeeklyTaskReportSearch").serialize(),
        success: function (html) {
            $("#ProjectWeeklyTaskReportContainer").html(html);
        }
    });
}

function exportProjectWeeklyTaskReport() {
    syncProjectWeeklyTaskReportEmployeeIds();
    window.location = _ProjectWeeklyTaskReportUrls.exportReport + "?" + $("#frmProjectWeeklyTaskReportSearch").serialize();
}