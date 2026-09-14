var _ProjectTaskReportUrls = {
    getReport: "/Cate/ProjectTaskReport/GetReport",
    exportReport: "/Cate/ProjectTaskReport/Export",
};

$(document).ready(function () {
    initProjectTaskReportWeekFilter();

    $("#btnSearchProjectTaskReport").on("click", function () {
        searchProjectTaskReport();
    });

    $("#btnExportProjectTaskReport").on("click", function () {
        exportProjectTaskReport();
    });

    $("#frmProjectTaskReportSearch").on("keydown", "input:not(.select2-search__field)", function (e) {
        if (e.keyCode === 13) {
            e.preventDefault();
            searchProjectTaskReport();
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


function parseProjectTaskReportDate(value) {
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

function formatProjectTaskReportDate(date) {
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    return day + '/' + month + '/' + date.getFullYear();
}

function addProjectTaskReportDays(date, days) {
    var result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    result.setDate(result.getDate() + days);
    return result;
}

function getProjectTaskReportWeekStart(date) {
    var day = date.getDay();
    var diff = day === 0 ? -6 : 1 - day;
    return addProjectTaskReportDays(date, diff);
}

function setProjectTaskReportWeek(date) {
    var weekStart = getProjectTaskReportWeekStart(date);
    var weekEnd = addProjectTaskReportDays(weekStart, 6);

    $("#FromDate").val(formatProjectTaskReportDate(weekStart));
    $("#ToDate").val(formatProjectTaskReportDate(weekEnd));
    $("#WeekDate").val(formatProjectTaskReportDate(weekStart));
    $("#WeekEndText").text(formatProjectTaskReportDate(weekEnd));
}

function initProjectTaskReportWeekFilter() {
    var initialDate = parseProjectTaskReportDate($("#FromDate").val()) || new Date();
    setProjectTaskReportWeek(initialDate);

    if ($.fn.datetimepicker) {
        $("#WeekDate").datetimepicker({
            format: "DD/MM/YYYY",
            locale: "vi"
        }).on("dp.change", function (e) {
            if (e.date) {
                setProjectTaskReportWeek(e.date.toDate());
            }
        });
    }

    $("#btnPrevWeek").on("click", function () {
        var current = parseProjectTaskReportDate($("#FromDate").val()) || new Date();
        setProjectTaskReportWeek(addProjectTaskReportDays(current, -7));
    });

    $("#btnNextWeek").on("click", function () {
        var current = parseProjectTaskReportDate($("#FromDate").val()) || new Date();
        setProjectTaskReportWeek(addProjectTaskReportDays(current, 7));
    });

    $("#WeekDate").on("change", function () {
        var selected = parseProjectTaskReportDate($(this).val());
        if (selected) {
            setProjectTaskReportWeek(selected);
        }
    });
}

function loadProjectTaskReportEmployees(boPhanId, selectedIds) {
    $.ajax({
        url: _ProjectTaskReportUrls.getEmployees,
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

            syncProjectTaskReportEmployeeIds();

            $employeeSelect.off("change").on("change", function () {
                syncProjectTaskReportEmployeeIds();
            });
        }
    });
}

function syncProjectTaskReportEmployeeIds() {
    var values = $("#EmployeeSelect").val() || [];
    $("#EmployeeIDs").val(values.join(";"));
}

function searchProjectTaskReport() {
    syncProjectTaskReportEmployeeIds();

    $.ajax({
        url: _ProjectTaskReportUrls.getReport,
        type: "POST",
        cache: false,
        data: $("#frmProjectTaskReportSearch").serialize(),
        success: function (html) {
            $("#ProjectTaskReportContainer").html(html);
        }
    });
}

function exportProjectTaskReport() {
    syncProjectTaskReportEmployeeIds();
    window.location = _ProjectTaskReportUrls.exportReport + "?" + $("#frmProjectTaskReportSearch").serialize();
}