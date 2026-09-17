/**
 * Dashboard Chart JS - CenIT TOC CRM
 * Xử lý tương tác chuyển đổi năm áp dụng, tìm kiếm từ khóa, xuất file Excel, render biểu đồ ApexCharts và mở modal chi tiết
 */

window._apexCharts = window._apexCharts || {};
var _searchDebounceTimer = null;

/**
 * Tải lại dữ liệu Dashboard theo Năm áp dụng và từ khóa tìm kiếm
 */
function onDashboardYearChange(year, keyword) {
    var selectedYear = year || $("#ApplyYear").val() || new Date().getFullYear();
    var kw = typeof keyword !== "undefined" ? keyword : ($("#dbSearchKeyword").val() || "");

    // Cập nhật hiển thị năm trên tab Kế hoạch kinh doanh
    $(".current-selected-year").text(selectedYear);

    var $container = $("#overview-content-container");
    if (!$container.length) return;

    // Hiển thị trạng thái đang tải
    $container.css({ opacity: 0.5, pointerEvents: "none" });

    var url = window._dashboardChartUrls ? window._dashboardChartUrls.getOverview : "/Dashboard/Dashboard/GetChartOverviewData";

    $.ajax({
        url: url,
        type: "GET",
        data: {
            applyYear: selectedYear,
            keyword: kw
        },
        cache: false,
        success: function (html) {
            $container.html(html);
            // Sau khi cập nhật lại view tổng quát, tải lại biểu đồ Cơ hội theo nhóm dịch vụ
            loadGroupServiceChart(selectedYear);
        },
        error: function (xhr, status, error) {
            console.error("Lỗi khi tải dữ liệu Dashboard theo năm:", error);
            if (typeof toastr !== "undefined") {
                toastr.error("Không thể tải dữ liệu năm " + selectedYear + ". Vui lòng thử lại!");
            }
        },
        complete: function () {
            $container.css({ opacity: 1, pointerEvents: "auto" });
        }
    });
}

/**
 * Kích hoạt tìm kiếm từ nút hoặc phím Enter
 */
function triggerDashboardFilter() {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var keyword = ($("#dbSearchKeyword").val() || "").trim();
    onDashboardYearChange(year, keyword);
}

/**
 * Xóa ô tìm kiếm và tải lại toàn bộ dữ liệu của năm
 */
function resetDashboardFilter() {
    $("#dbSearchKeyword").val("");
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    onDashboardYearChange(year, "");
}

/**
 * Bắt sự kiện bàn phím trên ô input tìm kiếm (Enter: reload server, Phím khác: instant client filter)
 */
function onKeywordKeyUp(event) {
    if (event && event.keyCode === 13) {
        event.preventDefault();
        triggerDashboardFilter();
        return;
    }

    clearTimeout(_searchDebounceTimer);
    _searchDebounceTimer = setTimeout(function () {
        var term = ($("#dbSearchKeyword").val() || "").trim();
        liveFilterTables(term);
    }, 250);
}

/**
 * Lọc nhanh tức thì các bảng dữ liệu trên client-side khi người dùng gõ phím
 */
function liveFilterTables(term) {
    var query = (term || "").toLowerCase();

    // 1. Lọc bảng Dự án Trọng điểm
    filterSingleTable(".db-keyprojects-table", query);

    // 2. Lọc các bảng con (Cơ hội đang quan tâm & Cảnh báo ActionTime)
    $(".db-custom-subtable").each(function () {
        filterSingleTable(this, query);
    });
}

function filterSingleTable(tableSelector, query) {
    var $table = $(tableSelector);
    if (!$table.length) return;

    var $rows = $table.find("tbody tr");
    if (!$rows.length) return;

    var visibleCount = 0;
    $rows.each(function () {
        var text = $(this).text().toLowerCase();
        if (!query || text.indexOf(query) !== -1) {
            $(this).show();
            visibleCount++;
        } else {
            $(this).hide();
        }
    });
}

/**
 * Lọc bảng dữ liệu theo từng Card khi người dùng gõ từ khóa vào ô tìm kiếm của Card đó
 */
function filterCardTable(input, tableId) {
    var query = ($(input).val() || "").trim().toLowerCase();
    var $table = $(tableId);
    if (!$table.length) return;

    var $rows = $table.find("tbody tr");
    if (!$rows.length) return;

    var visibleCount = 0;
    $rows.each(function () {
        var text = $(this).text().toLowerCase();
        if (!query || text.indexOf(query) !== -1) {
            $(this).show();
            visibleCount++;
        } else {
            $(this).hide();
        }
    });

    // Cập nhật số lượng hiển thị trên badge của Card
    var $badge = $(input).closest(".card-header").find(".badge[id^='badge']");
    if ($badge.length) {
        var unit = "hồ sơ";
        if (tableId.indexOf("KeyProjects") !== -1) unit = "dự án";
        else if (tableId.indexOf("Followed") !== -1) unit = "hồ sơ";
        $badge.text(visibleCount + " " + unit);
    }
}

/**
 * Tải danh sách xuất file Excel theo từng Card (Dự án trọng điểm, Cơ hội quan tâm, SP DVS chưa cập nhật ActionTime)
 */
function exportCardData(type) {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.export
        ? window._dashboardChartUrls.export
        : "/Cate/DigitalSales/Export";

    var url = baseUrl + "?applyYear=" + encodeURIComponent(year);

    if (type === 'keyProject') {
        var kw = $("#tableKeyProjects").closest(".card").find("input").val() || "";
        url += "&isKeyProject=true" + (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
    } else if (type === 'followed') {
        var kw = $("#tableFollowedOpps").closest(".card").find("input").val() || "";
        url += "&isFollowed=true" + (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
    } else if (type === 'stale') {
        var kw = $("#tableStaleSales").closest(".card").find("input").val() || "";
        url += (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
    }

    window.location.href = url;
}

/**
 * Tải danh sách xuất file Excel chung toàn bộ
 */
function exportDashboardList() {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.export
        ? window._dashboardChartUrls.export
        : "/Cate/DigitalSales/Export";
    window.location.href = baseUrl + "?applyYear=" + encodeURIComponent(year);
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
                    return val + " sản phẩm dịch vụ";
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
