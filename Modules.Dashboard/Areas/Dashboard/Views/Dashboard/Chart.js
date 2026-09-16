/**
 * Dashboard Chart JS
 * Xử lý tương tác chuyển đổi năm áp dụng và tab dữ liệu
 */

function onDashboardYearChange(year) {
    if (!year) return;

    // Cập nhật hiển thị năm trên tab Kế hoạch kinh doanh
    $(".current-selected-year").text(year);

    var $container = $("#overview-content-container");
    if (!$container.length) return;

    // Hiển thị trạng thái đang tải
    $container.css({ opacity: 0.5, pointerEvents: "none" });

    var url = window._dashboardChartUrls ? window._dashboardChartUrls.getOverview : "/Dashboard/Dashboard/GetChartOverviewData";

    $.ajax({
        url: url,
        type: "GET",
        data: { applyYear: year },
        cache: false,
        success: function (html) {
            $container.html(html);
        },
        error: function (xhr, status, error) {
            console.error("Lỗi khi tải dữ liệu Dashboard theo năm:", error);
            if (typeof toastr !== "undefined") {
                toastr.error("Không thể tải dữ liệu năm " + year + ". Vui lòng thử lại!");
            }
        },
        complete: function () {
            $container.css({ opacity: 1, pointerEvents: "auto" });
        }
    });
}

$(document).ready(function () {
    // Khởi tạo tooltip hoặc sự kiện chuyển tab nếu có
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab-overview") {
            // Trigger resize hoặc re-render charts nếu có
            $(window).trigger('resize');
        }
    });
});

/**
 * Mở modal popup danh sách cơ hội / dự án kinh doanh dịch vụ số theo trạng thái
 */
window.openDigitalSalesByStatus = function (statusId, statusName) {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.digitalSalesByStatus
        ? window._dashboardChartUrls.digitalSalesByStatus
        : '/Dashboard/Dashboard/DigitalSalesByStatus';

    var url = baseUrl + '?applyYear=' + encodeURIComponent(year)
        + '&statusId=' + encodeURIComponent(statusId || 0)
        + '&statusName=' + encodeURIComponent(statusName || '');

    var $btn = $('<a data-modal="" data-modal-id="DigitalSalesByStatus" data-width="1200" href="' + url + '"></a>');
    $("body").append($btn);
    $btn.trigger("click");
    $btn.remove();
};
