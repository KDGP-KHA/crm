/**
 * Dashboard Chart JS - CenIT TOC CRM
 * Xử lý tương tác chuyển đổi năm áp dụng, tìm kiếm từ khóa, xuất file Excel, render biểu đồ ApexCharts và mở modal chi tiết
 */

window._apexCharts = window._apexCharts || {};
var _searchDebounceTimer = null;

/**
/**
 * Tải lại dữ liệu Dashboard theo Năm áp dụng, Đơn vị xuất phát và từ khóa tìm kiếm
 */
function onDashboardYearChange(year, keyword) {
    onDashboardFilterChange(year, keyword);
}

function onDashboardSourceDepartmentChange(sourceDeptId) {
    onDashboardFilterChange(null, null, sourceDeptId);
}

function onDashboardFilterChange(year, keyword, sourceDepartmentId) {
    var selectedYear = year || $("#ApplyYear").val() || new Date().getFullYear();
    var kw = typeof keyword !== "undefined" && keyword !== null ? keyword : ($("#dbSearchKeyword").val() || "");
    var deptId = typeof sourceDepartmentId !== "undefined" && sourceDepartmentId !== null 
        ? sourceDepartmentId 
        : ($("#Dashboard_SourceDepartmentID").val() || 0);

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
            keyword: kw,
            sourceDepartmentId: deptId
        },
        cache: false,
        success: function (html) {
            $container.html(html);
            // Sau khi cập nhật lại view tổng quát, tải lại cả 2 biểu đồ
            loadGroupServiceChart(selectedYear);
            loadDepartmentChart(selectedYear);
            initDashboardPaginations();
        },
        error: function (xhr, status, error) {
            console.error("Lỗi khi tải dữ liệu Dashboard:", error);
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
    onDashboardFilterChange(year, keyword);
}

/**
 * Xóa ô tìm kiếm và tải lại toàn bộ dữ liệu của năm
 */
function resetDashboardFilter() {
    $("#dbSearchKeyword").val("");
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    onDashboardFilterChange(year, "");
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
    if (_tablePagination['#tableKeyProjects']) {
        _tablePagination['#tableKeyProjects'].filterTerm = query;
        _tablePagination['#tableKeyProjects'].currentPage = 1;
        renderTablePage('#tableKeyProjects');
    } else {
        filterSingleTable(".db-keyprojects-table", query);
    }

    // 2. Lọc các bảng con (Cơ hội đang quan tâm & Cảnh báo ActionTime)
    $(".db-custom-subtable").each(function () {
        var tid = "#" + $(this).attr("id");
        if (_tablePagination[tid]) {
            _tablePagination[tid].filterTerm = query;
            _tablePagination[tid].currentPage = 1;
            renderTablePage(tid);
        } else {
            filterSingleTable(this, query);
        }
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
/**
 * Trạng thái phân trang cho các bảng dữ liệu Dashboard
 */
var _tablePagination = {};

/**
 * Khởi tạo đồng loạt phân trang cho cả 3 bảng trên Dashboard
 */
function initDashboardPaginations() {
    if ($.fn.select2 && $('#selectAssignedStaleSales').hasClass('select2-hidden-accessible')) {
        try { $('#selectAssignedStaleSales').select2('destroy'); } catch (e) { }
    }
    initTablePagination('#tableKeyProjects', '#footerKeyProjects', 10);
    initTablePagination('#tableFollowedOpps', '#footerFollowedOpps', 10);
    initTablePagination('#tableStaleSales', '#footerStaleSales', 10);
}


function initTablePagination(tableId, footerId, defaultPageSize) {
    var $table = $(tableId);
    var $footer = $(footerId);
    if (!$table.length || !$footer.length) return;

    var pageSize = defaultPageSize || 10;
    var $rows = $table.find("tbody tr");
    if (!$rows.length) {
        $footer.hide();
        return;
    }

    $footer.show();
    var currentSize = parseInt($footer.find(".db-page-size-select").val(), 10) || pageSize;

    _tablePagination[tableId] = {
        footerId: footerId,
        pageSize: currentSize,
        currentPage: 1,
        filterTerm: ""
    };

    renderTablePage(tableId);
}

function changeTablePageSize(tableId, newSize) {
    if (!_tablePagination[tableId]) return;
    _tablePagination[tableId].pageSize = parseInt(newSize, 10) || 10;
    _tablePagination[tableId].currentPage = 1;
    renderTablePage(tableId);
}

function goToTablePage(tableId, pageNumber) {
    if (!_tablePagination[tableId]) return;
    _tablePagination[tableId].currentPage = parseInt(pageNumber, 10) || 1;
    renderTablePage(tableId);
}

function renderTablePage(tableId) {
    var state = _tablePagination[tableId];
    if (!state) return;

    var $table = $(tableId);
    var $footer = $(state.footerId);
    if (!$table.length || !$footer.length) return;

    var $allRows = $table.find("tbody tr");
    var filterTerm = (state.filterTerm || "").toLowerCase();
    var filterEmployee = (state.filterEmployee || "").toLowerCase();

    // Lọc các dòng khớp query và filterEmployee
    var $matchedRows = $allRows.filter(function () {
        var rowText = $(this).text().toLowerCase();
        if (filterTerm && rowText.indexOf(filterTerm) === -1) {
            return false;
        }

        if (filterEmployee) {
            var rowEmp = ($(this).data("assigned-employee") || $(this).find("td:last-child").text() || "").trim().toLowerCase();
            if (filterEmployee === "__unassigned__") {
                if (rowEmp !== "" && rowEmp !== "—" && rowEmp !== "-") return false;
            } else {
                if (rowEmp.indexOf(filterEmployee) === -1) return false;
            }
        }

        return true;
    });

    var totalRecords = $matchedRows.length;
    var pageSize = state.pageSize;
    var totalPages = Math.ceil(totalRecords / pageSize) || 1;

    if (state.currentPage > totalPages) {
        state.currentPage = totalPages;
    }
    if (state.currentPage < 1) {
        state.currentPage = 1;
    }
    var currentPage = state.currentPage;

    var startIndex = (currentPage - 1) * pageSize;
    var endIndex = Math.min(startIndex + pageSize, totalRecords);

    // Ẩn tất cả và chỉ hiện các dòng của trang hiện tại
    $allRows.hide();
    var $visiblePageRows = $matchedRows.slice(startIndex, endIndex);
    $visiblePageRows.show();

    // Cập nhật lại số thứ tự (STT) hiển thị liên tục
    $visiblePageRows.each(function (idx) {
        $(this).find("td:first-child").text(startIndex + idx + 1);
    });

    var unit = tableId.indexOf("KeyProjects") !== -1 ? "dự án" : "hồ sơ";

    // Cập nhật thông tin dòng hiển thị
    var $info = $footer.find("[id^='info']");
    if ($info.length) {
        if (totalRecords === 0) {
            $info.html("Hiển thị <strong>0</strong> " + unit);
        } else {
            $info.html("Hiển thị <strong>" + (startIndex + 1) + "</strong> - <strong>" + endIndex + "</strong> / <strong>" + totalRecords + "</strong> " + unit);
        }
    }

    // Cập nhật badge số lượng ở card header
    var $cardHeader = $table.closest(".card").find(".card-header");
    var $badge = $cardHeader.find(".badge[id^='badge']");
    if ($badge.length) {
        $badge.text(totalRecords + " " + unit);
    }

    // Cập nhật thanh phân trang
    var $pagination = $footer.find(".pagination");
    if (!$pagination.length) return;

    if (totalPages <= 1) {
        $pagination.html('');
        return;
    }

    var html = "";

    // Nút Đầu & Trước
    var isPrevDisabled = currentPage <= 1 ? " disabled" : "";
    html += '<li class="page-item' + isPrevDisabled + '">';
    html += '  <a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', 1)" title="Trang đầu">&laquo;</a>';
    html += '</li>';
    html += '<li class="page-item' + isPrevDisabled + '">';
    html += '  <a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', ' + (currentPage - 1) + ')" title="Trang trước">&lsaquo;</a>';
    html += '</li>';

    // Dải số trang (tối đa 5 nút)
    var maxButtons = 5;
    var startPage = Math.max(1, currentPage - Math.floor(maxButtons / 2));
    var endPage = Math.min(totalPages, startPage + maxButtons - 1);
    if (endPage - startPage + 1 < maxButtons) {
        startPage = Math.max(1, endPage - maxButtons + 1);
    }

    if (startPage > 1) {
        html += '<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', 1)">1</a></li>';
        if (startPage > 2) {
            html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
        }
    }

    for (var p = startPage; p <= endPage; p++) {
        var isActive = (p === currentPage) ? " active font-bold" : "";
        html += '<li class="page-item' + isActive + '">';
        html += '  <a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', ' + p + ')">' + p + '</a>';
        html += '</li>';
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
        }
        html += '<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', ' + totalPages + ')">' + totalPages + '</a></li>';
    }

    // Nút Tiếp & Cuối
    var isNextDisabled = currentPage >= totalPages ? " disabled" : "";
    html += '<li class="page-item' + isNextDisabled + '">';
    html += '  <a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', ' + (currentPage + 1) + ')" title="Trang sau">&rsaquo;</a>';
    html += '</li>';
    html += '<li class="page-item' + isNextDisabled + '">';
    html += '  <a class="page-link" href="javascript:void(0)" onclick="goToTablePage(\'' + tableId + '\', ' + totalPages + ')" title="Trang cuối">&raquo;</a>';
    html += '</li>';

    $pagination.html(html);
}

/**
 * Lọc bảng dữ liệu theo từng Card khi người dùng gõ từ khóa vào ô tìm kiếm của Card đó
 */
function filterCardTable(input, tableId) {
    var query = ($(input).val() || "").trim().toLowerCase();

    // Nếu bảng có cấu hình phân trang, cập nhật filterTerm và render lại trang 1
    if (_tablePagination[tableId]) {
        _tablePagination[tableId].filterTerm = query;
        _tablePagination[tableId].currentPage = 1;
        renderTablePage(tableId);
        return;
    }

    var $table = $(tableId);
    if (!$table.length) return;

    var $rows = $table.find("tbody tr");
    if (!$rows.length) return;

    var emp = "";
    var $empSelect = $(input).closest(".card-header").find("select[id^='selectAssigned']");
    if ($empSelect.length) {
        emp = ($empSelect.val() || "").trim().toLowerCase();
    }

    var visibleCount = 0;
    $rows.each(function () {
        var text = $(this).text().toLowerCase();
        var rowEmp = ($(this).data("assigned-employee") || $(this).find("td:last-child").text() || "").trim().toLowerCase();

        var matchQuery = !query || text.indexOf(query) !== -1;
        var matchEmp = true;
        if (emp) {
            if (emp === "__unassigned__") {
                matchEmp = (rowEmp === "" || rowEmp === "—" || rowEmp === "-");
            } else {
                matchEmp = rowEmp.indexOf(emp) !== -1;
            }
        }

        if (matchQuery && matchEmp) {
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
 * Lọc bảng dữ liệu theo Người phụ trách khi chọn combobox trên header của Card
 */
function filterCardTableByEmployee(select, tableId) {
    var emp = ($(select).val() || "").trim();

    // Nếu bảng có cấu hình phân trang, cập nhật filterEmployee và render lại trang 1
    if (_tablePagination[tableId]) {
        _tablePagination[tableId].filterEmployee = emp;
        _tablePagination[tableId].currentPage = 1;
        renderTablePage(tableId);
        return;
    }

    var $table = $(tableId);
    if (!$table.length) return;

    var $rows = $table.find("tbody tr");
    if (!$rows.length) return;

    var filterTerm = "";
    var $search = $(select).closest(".card-header").find("input[type='text']");
    if ($search.length) {
        filterTerm = ($search.val() || "").trim().toLowerCase();
    }

    var empLower = emp.toLowerCase();
    var visibleCount = 0;
    $rows.each(function () {
        var rowText = $(this).text().toLowerCase();
        var rowEmp = ($(this).data("assigned-employee") || $(this).find("td:last-child").text() || "").trim().toLowerCase();

        var matchQuery = !filterTerm || rowText.indexOf(filterTerm) !== -1;
        var matchEmp = true;
        if (empLower) {
            if (empLower === "__unassigned__") {
                matchEmp = (rowEmp === "" || rowEmp === "—" || rowEmp === "-");
            } else {
                matchEmp = rowEmp.indexOf(empLower) !== -1;
            }
        }

        if (matchQuery && matchEmp) {
            $(this).show();
            visibleCount++;
        } else {
            $(this).hide();
        }
    });

    var $badge = $(select).closest(".card-header").find(".badge[id^='badge']");
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
    var sourceDeptId = $("#Dashboard_SourceDepartmentID").val() || 0;
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.export
        ? window._dashboardChartUrls.export
        : "/Cate/DigitalSales/Export";

    var url = baseUrl + "?applyYear=" + encodeURIComponent(year);
    if (sourceDeptId > 0) {
        url += "&sourceDepartmentID=" + encodeURIComponent(sourceDeptId);
    }

    if (type === 'keyProject') {
        var kw = $("#tableKeyProjects").closest(".card").find("input").val() || "";
        url += "&isKeyProject=true" + (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
    } else if (type === 'followed') {
        var kw = $("#tableFollowedOpps").closest(".card").find("input").val() || "";
        url += "&isFollowed=true" + (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
    } else if (type === 'stale') {
        var kw = $("#tableStaleSales").closest(".card").find("input").val() || "";
        var empSelect = $("#tableStaleSales").closest(".card").find("select[id^='selectAssigned']");
        var empId = empSelect.find("option:selected").data("emp-id");
        var empName = empSelect.val() || "";
        url += (kw ? "&keyword=" + encodeURIComponent(kw.trim()) : "");
        if (empId) {
            url += "&employeeID=" + encodeURIComponent(empId);
        } else if (empName && empName !== "__UNASSIGNED__") {
            url += "&keyword=" + encodeURIComponent(empName.trim());
        }
    }

    window.location.href = url;
}

/**
 * Tải danh sách xuất file Excel chung toàn bộ
 */
function exportDashboardList() {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var sourceDeptId = $("#Dashboard_SourceDepartmentID").val() || 0;
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.export
        ? window._dashboardChartUrls.export
        : "/Cate/DigitalSales/Export";
    var url = baseUrl + "?applyYear=" + encodeURIComponent(year);
    if (sourceDeptId > 0) {
        url += "&sourceDepartmentID=" + encodeURIComponent(sourceDeptId);
    }
    window.location.href = url;
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

            var items = result.data.slice().sort(function (a, b) {
                return (b.Quantity || 0) - (a.Quantity || 0);
            });

            var series = [], labels = [], groupIds = [];
            for (var i = 0; i < items.length; i++) {
                series.push(items[i].Quantity);
                labels.push(items[i].Name);
                groupIds.push(items[i].GroupServiceID);
            }

            _renderApexHorizontalBar(
                'gs-opp-chart',
                labels,
                series,
                function (dataPointIndex) {
                    if (typeof dataPointIndex !== "undefined" && groupIds[dataPointIndex]) {
                        detailOpportunityByGroupService(groupIds[dataPointIndex]);
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
 * Bảng 16 màu sắc phân biệt rõ ràng (chống trùng lặp 5 màu mặc định của ApexCharts)
 */
var _dashboardDistinctPalette = [
    '#2563eb', // 1. Blue
    '#10b981', // 2. Emerald Green
    '#f59e0b', // 3. Amber
    '#ef4444', // 4. Red / Coral
    '#8b5cf6', // 5. Violet
    '#06b6d4', // 6. Cyan
    '#ec4899', // 7. Pink
    '#f97316', // 8. Orange
    '#14b8a6', // 9. Teal
    '#6366f1', // 10. Indigo
    '#84cc16', // 11. Lime
    '#d946ef', // 12. Fuchsia
    '#0284c7', // 13. Sky Blue
    '#eab308', // 14. Yellow
    '#e11d48', // 15. Rose
    '#475569'  // 16. Slate
];

/**
 * Hàm vẽ ApexCharts dạng Cột ngang xếp hạng (Horizontal Bar Chart)
 * Hiển thị tốt cả các nhóm số liệu rất nhỏ (1, 2...) so với nhóm lớn (100+), kèm 16 màu riêng biệt
 */
function _renderApexHorizontalBar(elementId, labels, series, onSelectIndex) {
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

    var total = 0;
    var maxVal = 0;
    for (var i = 0; i < series.length; i++) {
        var v = Number(series[i]) || 0;
        total += v;
        if (v > maxVal) maxVal = v;
    }

    var chartHeight = Math.max(350, labels.length * 36 + 50);
    el.innerHTML = '';

    var bindYAxisLabelClicks = function (chartContext) {
        if (!chartContext || !chartContext.el || typeof onSelectIndex !== "function") return;
        var yLabels = chartContext.el.querySelectorAll('.apexcharts-yaxis-label');
        for (var idx = 0; idx < yLabels.length; idx++) {
            (function (labelEl, itemIdx) {
                labelEl.style.cursor = 'pointer';
                labelEl.onclick = function () {
                    onSelectIndex(itemIdx);
                };
            })(yLabels[idx], idx);
        }
    };

    var options = {
        series: [{
            name: 'Số lượng',
            data: series
        }],
        colors: _dashboardDistinctPalette,
        chart: {
            type: 'bar',
            height: chartHeight,
            width: '100%',
            toolbar: { show: false },
            fontFamily: 'inherit',
            events: {
                dataPointSelection: function (event, chartContext, config) {
                    if (config && typeof config.dataPointIndex !== "undefined" && typeof onSelectIndex === "function") {
                        onSelectIndex(config.dataPointIndex);
                    }
                },
                mounted: function (chartContext) {
                    bindYAxisLabelClicks(chartContext);
                },
                updated: function (chartContext) {
                    bindYAxisLabelClicks(chartContext);
                }
            }
        },
        plotOptions: {
            bar: {
                horizontal: true,
                distributed: true,
                barHeight: '62%',
                borderRadius: 4,
                dataLabels: {
                    position: 'top'
                }
            }
        },
        dataLabels: {
            enabled: true,
            textAnchor: 'start',
            offsetX: 6,
            style: {
                fontSize: '12px',
                fontWeight: 700,
                colors: ['#1e293b']
            },
            formatter: function (val) {
                var pct = total > 0 ? ((val * 100) / total).toFixed(1).replace(/\.0$/, '') : 0;
                return val + ' (' + pct + '%)';
            }
        },
        xaxis: {
            categories: labels,
            max: maxVal > 0 ? Math.ceil(maxVal * 1.32) : 10,
            labels: {
                style: {
                    fontSize: '11.5px',
                    colors: '#64748b'
                },
                formatter: function (val) {
                    return Math.round(val);
                }
            },
            axisBorder: { show: false },
            axisTicks: { show: false }
        },
        yaxis: {
            labels: {
                maxWidth: 145,
                style: {
                    fontSize: '12.5px',
                    fontWeight: 600,
                    colors: '#1e293b'
                }
            }
        },
        grid: {
            borderColor: '#f1f5f9',
            strokeDashArray: 3,
            xaxis: { lines: { show: true } },
            yaxis: { lines: { show: false } },
            padding: { top: -8, right: 18, bottom: 0, left: 4 }
        },
        legend: {
            show: false
        },
        tooltip: {
            y: {
                formatter: function (val) {
                    var pct = total > 0 ? ((val * 100) / total).toFixed(1).replace(/\.0$/, '') : 0;
                    return val + " sản phẩm dịch vụ (" + pct + "%)";
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
 * Hàm vẽ ApexCharts dạng Pie thống nhất (kèm bảng 16 màu riêng biệt)
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
        colors: _dashboardDistinctPalette,
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
 * Tải dữ liệu và vẽ biểu đồ Cơ cấu theo Đơn vị xuất phát (Donut / Pie Chart ApexCharts - US-18)
 */
function loadDepartmentChart(year) {
    var el = document.querySelector('#dept-opp-chart');
    if (!el) return;

    if (window._apexCharts && window._apexCharts['dept-opp-chart']) {
        try {
            window._apexCharts['dept-opp-chart'].destroy();
        } catch (e) { }
        window._apexCharts['dept-opp-chart'] = null;
    }

    el.innerHTML = '<div class="text-center text-secondary-l1 py-4"><i class="fa fa-spinner fa-spin mr-1"></i> Đang tải biểu đồ đơn vị...</div>';

    var selectedYear = year || $("#ApplyYear").val() || new Date().getFullYear();
    var chartUrl = window._dashboardChartUrls && window._dashboardChartUrls.getDepartmentChart
        ? window._dashboardChartUrls.getDepartmentChart
        : "/Dashboard/Dashboard/GetDepartmentChart";

    $.ajax({
        url: chartUrl,
        type: "POST",
        data: {
            "applyYear": selectedYear
        },
        success: function (result) {
            if (!result || !result.data || !result.data.length) {
                el.innerHTML = '<div class="text-center text-secondary-l1 py-4">Không có dữ liệu đơn vị</div>';
                return;
            }

            var series = [], labels = [], deptIds = [];
            for (var i = 0; i < result.data.length; i++) {
                series.push(result.data[i].TotalCount);
                labels.push(result.data[i].TenBoPhan);
                deptIds.push(result.data[i].BoPhan_ID);
            }

            _renderApexPie(
                'dept-opp-chart',
                labels,
                series,
                function (event, chartContext, config) {
                    if (config && typeof config.dataPointIndex !== "undefined" && typeof deptIds[config.dataPointIndex] !== "undefined") {
                        var clickedDeptId = deptIds[config.dataPointIndex];
                        if (clickedDeptId > 0 && $("#Dashboard_SourceDepartmentID").length) {
                            $("#Dashboard_SourceDepartmentID").val(clickedDeptId);
                            onDashboardSourceDepartmentChange(clickedDeptId);
                        }
                    }
                }
            );
        },
        error: function (xhr, status, error) {
            console.error("Lỗi khi tải biểu đồ Đơn vị xuất phát:", error);
            el.innerHTML = '<div class="text-center text-danger py-4">Không thể tải biểu đồ đơn vị</div>';
        }
    });
}

/**
 * Mở modal popup danh sách cơ hội / dự án kinh doanh dịch vụ số theo trạng thái
 */
window.openDigitalSalesByStatus = function (statusId, statusName) {
    var year = $("#ApplyYear").val() || new Date().getFullYear();
    var sourceDeptId = $("#Dashboard_SourceDepartmentID").val() || 0;
    var baseUrl = window._dashboardChartUrls && window._dashboardChartUrls.digitalSalesByStatus
        ? window._dashboardChartUrls.digitalSalesByStatus
        : '/Dashboard/Dashboard/DigitalSalesByStatus';

    var url = baseUrl + '?applyYear=' + encodeURIComponent(year)
        + '&statusId=' + encodeURIComponent(statusId || 0)
        + '&statusName=' + encodeURIComponent(statusName || '')
        + '&sourceDepartmentId=' + encodeURIComponent(sourceDeptId);

    var $btn = $('<a data-modal="" data-modal-id="DigitalSalesByStatus" data-width="1200" href="' + url + '"></a>');
    $("body").append($btn);
    $btn.trigger("click");
    $btn.remove();
};

$(document).ready(function () {
    // Tải biểu đồ Cơ hội theo nhóm dịch vụ và biểu đồ Đơn vị xuất phát lần đầu
    loadGroupServiceChart($("#ApplyYear").val());
    loadDepartmentChart($("#ApplyYear").val());

    // Khởi tạo phân trang cho cả 3 bảng Dashboard
    initDashboardPaginations();

    // Sự kiện chuyển tab
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab-overview") {
            $(window).trigger('resize');
            if (window._apexCharts && window._apexCharts['gs-opp-chart']) {
                window._apexCharts['gs-opp-chart'].render();
            }
            if (window._apexCharts && window._apexCharts['dept-opp-chart']) {
                window._apexCharts['dept-opp-chart'].render();
            }
        }
    });
});
