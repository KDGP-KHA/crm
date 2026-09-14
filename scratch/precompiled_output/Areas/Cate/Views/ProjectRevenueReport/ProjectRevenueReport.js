var _projectRevenueReportUrls = {
    getReport: "/Cate/ProjectRevenueReport/GetReport",
    exportReport: "/Cate/ProjectRevenueReport/Export"
};

$(document).ready(function () {
    initProjectRevenueReportFilters();

    $("#btnSearchProjectRevenueReport").on("click", function () {
        searchProjectRevenueReport();
    });

    $("#btnExportProjectRevenueReport").on("click", function () {
        exportProjectRevenueReport();
    });

    $("#frmProjectRevenueReportSearch").on("keydown", "input", function (e) {
        if (e.keyCode === 13) {
            e.preventDefault();
            searchProjectRevenueReport();
        }
    });
});

function initProjectRevenueReportFilters() {
    initProjectRevenueReportSelect2("#ReportType");
    initProjectRevenueReportSelect2("#Month");
    initProjectRevenueReportSelect2("#Quarter");

    applyProjectRevenueReportFilter();

    $("#ReportType").on("change", function () {
        applyProjectRevenueReportFilter();
    });
}

function initProjectRevenueReportSelect2(selector) {
    if (!$.fn.select2) {
        return;
    }

    var $element = $(selector);
    if ($element.hasClass("select2-hidden-accessible")) {
        $element.select2("destroy");
    }

    $element.select2({
        width: "100%",
        allowClear: true,
        dropdownParent: $("#card-project-revenue-report")
    });
}

function applyProjectRevenueReportFilter() {
    var reportType = ($("#ReportType").val() || "").trim();
    var isMonth = reportType === "Month";
    var isQuarter = reportType === "Quarter";
    var isYear = reportType === "Year";

    toggleProjectRevenueReportField("#ProjectRevenueReportMonthWrapper", isMonth);
    toggleProjectRevenueReportField("#ProjectRevenueReportQuarterWrapper", isQuarter);

    if (!isMonth) {
        clearProjectRevenueReportSelect("#Month");
    }

    if (!isQuarter) {
        clearProjectRevenueReportSelect("#Quarter");
    }

    if (!isMonth && !isQuarter && !isYear) {
        clearProjectRevenueReportSelect("#Month");
        clearProjectRevenueReportSelect("#Quarter");
    }
}

function toggleProjectRevenueReportField(selector, isVisible) {
    var $wrapper = $(selector);
    if (isVisible) {
        $wrapper
            .addClass("d-flex")
            .show();
    } else {
        $wrapper
            .removeClass("d-flex")
            .hide();
    }
}

function clearProjectRevenueReportSelect(selector) {
    var $element = $(selector);
    if (!$element.length) {
        return;
    }

    if ($element.val()) {
        $element.val("").trigger("change.select2");
    }
}

function getProjectRevenueReportMessages() {
    return $("#ProjectRevenueReportMessages").data() || {};
}

function validateProjectRevenueReportSearch() {
    var messages = getProjectRevenueReportMessages();
    var reportType = ($("#ReportType").val() || "").trim();
    var year = ($("#Year").val() || "").trim();
    var month = ($("#Month").val() || "").trim();
    var quarter = ($("#Quarter").val() || "").trim();

    if (!reportType) {
        return messages.reportTypeRequired || "";
    }

    if (reportType !== "Month" && reportType !== "Quarter" && reportType !== "Year") {
        return messages.invalidReportType || "";
    }

    if (!year) {
        return messages.yearRequired || "";
    }

    if (reportType === "Month" && !month) {
        return messages.monthRequired || "";
    }

    if (reportType === "Quarter" && !quarter) {
        return messages.quarterRequired || "";
    }

    return "";
}

function searchProjectRevenueReport() {
    var validationMessage = validateProjectRevenueReportSearch();
    if (validationMessage) {
        alert(validationMessage);
        return;
    }

    $.ajax({
        url: _projectRevenueReportUrls.getReport,
        type: "POST",
        cache: false,
        data: $("#frmProjectRevenueReportSearch").serialize(),
        success: function (html) {
            $("#ProjectRevenueReportContainer").html(html);
        }
    });
}

function exportProjectRevenueReport() {
    var validationMessage = validateProjectRevenueReportSearch();
    if (validationMessage) {
        alert(validationMessage);
        return;
    }

    window.location = _projectRevenueReportUrls.exportReport + "?" + $("#frmProjectRevenueReportSearch").serialize();
}
