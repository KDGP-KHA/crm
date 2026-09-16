/**
 * Dashboard Chart JS - CenIT TOC CRM
 * Xử lý tương tác chuyển đổi năm áp dụng, render biểu đồ ApexCharts và mở modal chi tiết
 */

window._apexCharts = window._apexCharts || {};

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
            // Sau khi cập nhật lại view tổng quát, tải lại biểu đồ Cơ hội theo nhóm dịch vụ
            loadGroupServiceChart(year);
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

/**
 * Tải dữ liệu và vẽ biểu đồ Cơ hội theo nhóm dịch vụ (Pie Chart)
 */
function loadGroupServiceChart(year) {
    var el = document.querySelector('#gs-opp-chart');
    if (!el) return;

    if (window._apexCharts && window._apexCharts['gs-opp-chart']) {
        try {
            window._apexCharts['gs-opp-chart'].destroy();
        } catch (e) { }
        window._apexCharts['gs-opp-chart'] = null;
    }

    el.innerHTML = '<div class="text-center text-secondary-l1 py-4"><i class="fa fa-spinner fa-spin mr-1"></i> Đang tải biểu đồ...</div>';

    var selectedYear = year || $("#ApplyYear").val() || new Date().getFullYear();
    var fromDate = "01/01/" + selectedYear;
    var toDate = "31/12/" + selectedYear;
    var chartUrl = window._dashboardChartUrls && window._dashboardChartUrls.getGroupServiceChart
        ? window._dashboardChartUrls.getGroupServiceChart
        : "/Dashboard/Dashboard/GetGroupServiceChart";

    $.ajax({
        url: chartUrl,
        type: "POST",
        data: {
            "Type": 0,
            "applyYear": selectedYear,
            "FromDate": fromDate,
            "ToDate": toDate
        },
        success: function (result) {
            if (!result || !result.data || !result.data.length) {
                el.innerHTML = '<div class="text-center text-secondary-l1 py-4">Không có dữ liệu</div>';
                return;
            }

            var series = [], labels = [], groupIds = [];
            for (var i = 0; i < result.data.length; i++) {
                series.push(result.data[i].Quantity);
                labels.push(result.data[i].Name);
                groupIds.push(result.data[i].GroupServiceID);
            }

            _renderApexPie(
                'gs-opp-chart',
                labels,
                series,
                function (event, chartContext, config) {
                    if (config && typeof config.dataPointIndex !== "undefined" && groupIds[config.dataPointIndex]) {
                        var groupId = groupIds[config.dataPointIndex];
                        detailOpportunityByGroupService(groupId);
                    }
                }
            );
        },
        error: function (xhr, status, error) {
            console.error("Lỗi khi tải biểu đồ Cơ hội theo nhóm dịch vụ:", error);
            el.innerHTML = '<div class="text-center text-danger py-4">Không thể tải biểu đồ</div>';
        }
    });
}

/**
 * Hàm vẽ ApexCharts dạng Pie thống nhất
 */
function _renderApexPie(elementId, labels, series, onSelect) {
    var el = document.querySelector('#' + elementId);
    if (!el) return;

    if (window._apexCharts[elementId]) {
        try {
            window._apexCharts[elementId].destroy();
        } catch (e) { }
        window._apexCharts[elementId] = null;
    }

    if (!series || !series.length) {
        el.innerHTML = '<div class="text-center text-secondary-l1 py-4">Không có dữ liệu</div>';
        return;
    }

    el.innerHTML = '';
    var options = {
        series: series,
        labels: labels,
        chart: {
            width: 350,
            type: 'pie',
            events: { dataPointSelection: onSelect }
        },
        plotOptions: {
            pie: {
                dataLabels: { offset: -5 }
            }
        },
        grid: { padding: { top: 0, bottom: 0, left: 0, right: 0 } },
        dataLabels: {
            enabled: true,
            formatter: function (val, opts) {
                return opts.w.config.series[opts.seriesIndex];
            }
        },
        legend: {
            show: true,
            position: 'bottom',
            fontSize: '12px',
            markers: { width: 10, height: 10, radius: 10 }
        },
        tooltip: {
            y: {
                formatter: function (val) {
                    return val + " cơ hội";
                }
            }
        }
    };

    if (typeof ApexCharts !== "undefined") {
        var chart = new ApexCharts(el, options);
        chart.render();
        window._apexCharts[elementId] = chart;
    } else {
        el.innerHTML = '<div class="text-center text-danger py-4">ApexCharts chưa được nạp</div>';
    }
}

/**
 * Mở modal popup danh sách cơ hội kinh doanh theo nhóm dịch vụ (click lát cắt từ chart)
 */
window.detailOpportunityByGroupService = function (groupServiceId) {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var fromDate = "01/01/" + year;
    var toDate = "31/12/" + year;

    var btn = document.querySelector('[data-modal-id="OpportunityByGroupService"]');
    var targetUrl = (window._dashboardChartUrls && window._dashboardChartUrls.opportunityByGroupService
        ? window._dashboardChartUrls.opportunityByGroupService
        : '/Dashboard/Dashboard/OpportunityByGroupService')
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&groupServiceId=' + encodeURIComponent(groupServiceId);

    if (btn) {
        btn.setAttribute("href", targetUrl);
        btn.click();
    } else {
        var $btn = $('<a data-modal="" data-modal-id="OpportunityByGroupService" data-width="1200" href="' + targetUrl + '"></a>');
        $("body").append($btn);
        $btn.trigger("click");
        $btn.remove();
    }
};

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

$(document).ready(function () {
    // Tải biểu đồ Cơ hội theo nhóm dịch vụ lần đầu
    loadGroupServiceChart($("#ApplyYear").val());

    // Sự kiện chuyển tab
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab-overview") {
            $(window).trigger('resize');
            if (window._apexCharts && window._apexCharts['gs-opp-chart']) {
                window._apexCharts['gs-opp-chart'].render();
            }
        }
    });
});
