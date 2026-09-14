var _chart = null;
var _timer = null;
var _tableOpp = null;
var _tablePrj = null;
var _tablePrjOverview = null;
var _tableOppOverview = null;
var _tableStaleUpdates = null;
var _planTable = null;
var _planTableFilter = null;
var _planSearchTimer = null;

$(document).ready(function () {
    initTableOpp();
    initTablePrj();
    initProjectTable();
    initOpportunityTable();
    initPlanWeekFilter();
    initPlanDashboard();
    initDashboardCharts();
    initStaleUpdates();

    if (typeof _initialChartRows !== 'undefined' && _initialChartRows.length > 0)
        buildChart(_initialChartRows);
    else
        hideChart();

    // Khi đổi ngày.
    $("#EmpFromDate, #EmpToDate").on("change", function () {
        var fromDate = $("#EmpFromDate").val();
        var toDate = $("#EmpToDate").val();
        loadEmployeeDashboard();
    });
    $("#FromDate, #ToDate").on("change", function () {
        loadDashboard();
    });

    // Khi đổi ngày tab kế hoạch kinh doanh.
    $("#PlanFromDate, #PlanToDate").on("change", function () {
        loadPlanDashboard();
    });

});

function initStaleUpdates() {
    var $root = $('#dashboard-stale-updates');
    if (!$root.length || !$('#staleUpdatesTable').length) return;

    if (_tableStaleUpdates) {
        _tableStaleUpdates.destroy();
        _tableStaleUpdates = null;
    }

    _tableStaleUpdates = $('#staleUpdatesTable').DataTable({
        responsive: false,
        autoWidth: false,
        lengthChange: true,
        pageLength: 5,
        lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, 'Tất cả']],
        ordering: true,
        searching: true,
        order: [[4, 'desc']],
        language: _dtLang(),
        columnDefs: [{ orderable: false, targets: [0, 1, 2] }]
    });
}

window.detailOpportunityByUser = function (id, type) {
    var fromDate = $("#EmpFromDate").val();
    var toDate = $("#EmpToDate").val();

    var btn = document.querySelector('[data-modal-id="OpportunityByUser"]');
    if (!btn) return;

    var baseUrl = '/Dashboard/Dashboard/OpportunityByUser/';

    var url = baseUrl + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&id=' + id
        + '&type=' + type
    btn.setAttribute("href", url);
    btn.click();
};

window.detailProjectByUser = function (id) {
    var fromDate = $("#EmpFromDate").val();
    var toDate = $("#EmpToDate").val();

    var btn = document.querySelector('[data-modal-id="ProjectByUser"]');
    if (!btn) return;

    var baseUrl = '/Dashboard/Dashboard/ProjectByUser/';

    var url = baseUrl + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&id=' + id
    btn.setAttribute("href", url);
    btn.click();
};

// Kế hoạch kinh doanh.

window.detailPlanByUser = function (id) {
    var fromDate = $("#PlanFromDate").val();
    var toDate = $("#PlanToDate").val();

    var btn = document.querySelector('[data-modal-id="PlanByUser"]');
    if (!btn) return;

    var url = '/Dashboard/Dashboard/PlanByUser/'
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&id=' + id;

    btn.setAttribute("href", url);
    btn.click();
};

window.detailProjectBySuccessRate = function (successRate, isGreaterOrEqual) {
    var fromDate = $("#FromDate").val();
    var toDate = $("#ToDate").val();

    var btn = document.querySelector('[data-modal-id="ProjectBySuccessRate"]');
    if (!btn) return;

    var url =
        '/Dashboard/Dashboard/ProjectBySuccessRate'
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&successRate=' + encodeURIComponent(successRate);

    if (typeof isGreaterOrEqual !== "undefined") {
        url += '&isGreaterOrEqual=' + encodeURIComponent(isGreaterOrEqual);
    }

    btn.setAttribute("href", url);
    btn.click();
};

window.detailOpportunityBySuccessRate = function (successRate, isGreaterOrEqual) {
    var fromDate = $("#FromDate").val();
    var toDate = $("#ToDate").val();

    var btn = document.querySelector('[data-modal-id="OpportunityBySuccessRate"]');

    if (!btn) return;

    var url =
        '/Dashboard/Dashboard/OpportunityBySuccessRate'
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&successRate=' + encodeURIComponent(successRate);

    if (typeof isGreaterOrEqual !== "undefined") {
        url += '&isGreaterOrEqual=' + encodeURIComponent(isGreaterOrEqual);
    }

    btn.setAttribute("href", url);
    btn.click();
};

// Mở popup danh sách cơ hội kinh doanh của một nhóm dịch vụ (click từ chart)
window.detailOpportunityByGroupService = function (groupServiceId) {
    var fromDate = $("#FromDate").val();
    var toDate = $("#ToDate").val();

    var btn = document.querySelector('[data-modal-id="OpportunityByGroupService"]');
    if (!btn) return;

    var url =
        '/Dashboard/Dashboard/OpportunityByGroupService'
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&groupServiceId=' + encodeURIComponent(groupServiceId);

    btn.setAttribute("href", url);
    btn.click();
};

// Mở popup danh sách dự án của một nhóm dịch vụ (click từ chart)
window.detailProjectByGroupService = function (groupServiceId) {
    var fromDate = $("#FromDate").val();
    var toDate = $("#ToDate").val();

    var btn = document.querySelector('[data-modal-id="ProjectByGroupService"]');
    if (!btn) return;

    var url =
        '/Dashboard/Dashboard/ProjectByGroupService'
        + '?fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate)
        + '&groupServiceId=' + encodeURIComponent(groupServiceId);

    btn.setAttribute("href", url);
    btn.click();
};

function loadPlanDashboard() {
    var fromDate = $("#PlanFromDate").val();
    var toDate = $("#PlanToDate").val();
    var keyword = $("#PlanSearchText").val();

    $.ajax({
        url: '/Dashboard/Dashboard/PlanFilter',
        type: 'GET',
        data: { fromDate: fromDate, toDate: toDate, keyword: keyword },
        success: function (res) {
            $("#plan-container").html(res);
            syncPlanWeekFromResult();
            initPlanDashboard();
        }
    });
}

function syncPlanWeekFromResult() {
    var $board = $('.plan-table-board').first();
    if (!$board.length) return;

    var weekStart = $board.attr('data-week-start');
    var weekEnd = $board.attr('data-week-end');

    if (weekStart) {
        $("#PlanFromDate").val(weekStart);
        $("#PlanWeekDate").val(weekStart);
    }

    if (weekEnd) {
        $("#PlanToDate").val(weekEnd);
        $("#PlanWeekEndText").text(weekEnd);
    }
}

function parsePlanDate(value) {
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

function formatPlanDate(date) {
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    return day + '/' + month + '/' + date.getFullYear();
}

function formatPlanIsoDate(date) {
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    return date.getFullYear() + '-' + month + '-' + day;
}

function addPlanDays(date, days) {
    var result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    result.setDate(result.getDate() + days);
    return result;
}

function getPlanWeekStart(date) {
    var day = date.getDay();
    var diff = day === 0 ? -6 : 1 - day;
    return addPlanDays(date, diff);
}

function setPlanWeek(date, shouldLoad) {
    var weekStart = getPlanWeekStart(date);
    var weekEnd = addPlanDays(weekStart, 6);

    $("#PlanFromDate").val(formatPlanDate(weekStart));
    $("#PlanToDate").val(formatPlanDate(weekEnd));
    $("#PlanWeekDate").val(formatPlanDate(weekStart));
    $("#PlanWeekEndText").text(formatPlanDate(weekEnd));

    if (shouldLoad) {
        renderPlanWeekStrip(weekStart);
        loadPlanDashboard();
    }
}

function renderPlanWeekStrip(weekStart) {
    var $strip = $('#planWeekStrip');
    if (!$strip.length) return;

    var today = new Date();
    today = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    var selected = (today >= weekStart && today <= addPlanDays(weekStart, 6)) ? today : weekStart;
    var dayNames = ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy'];
    var html = '';

    for (var i = 0; i < 7; i++) {
        var day = addPlanDays(weekStart, i);
        var iso = formatPlanIsoDate(day);
        var isToday = formatPlanIsoDate(day) === formatPlanIsoDate(today);
        var isActive = formatPlanIsoDate(day) === formatPlanIsoDate(selected);
        var classes = 'plan-week-day' + (isToday ? ' is-today' : '') + (isActive ? ' is-active' : '');

        html += '<a href="javascript:void(0)" class="' + classes + '" data-plan-day-filter="' + iso + '">';
        html += '<span>' + dayNames[day.getDay()] + ' - ' + formatPlanDate(day).substring(0, 5) + '</span>';
        html += '</a>';
    }

    $strip.attr('data-selected-date', formatPlanIsoDate(selected));
    $strip.html(html);

    if ($('#planFilterNote').length) {
        $('#planFilterNote').text('Hiển thị kế hoạch trong tuần ' + formatPlanDate(weekStart) + ' - ' + formatPlanDate(addPlanDays(weekStart, 6)) + '.');
    }

    applyPlanTableFilters();
}

function initPlanWeekFilter() {
    var initialDate = parsePlanDate($("#PlanFromDate").val()) || new Date();
    setPlanWeek(initialDate, false);

    $("#btnPlanPrevWeek").off("click.planWeek").on("click.planWeek", function () {
        var current = parsePlanDate($("#PlanFromDate").val()) || new Date();
        setPlanWeek(addPlanDays(current, -7), true);
    });

    $("#btnPlanNextWeek").off("click.planWeek").on("click.planWeek", function () {
        var current = parsePlanDate($("#PlanFromDate").val()) || new Date();
        setPlanWeek(addPlanDays(current, 7), true);
    });

    $("#PlanWeekDate").off("change.planWeek").on("change.planWeek", function () {
        var selected = parsePlanDate($(this).val());
        if (selected) {
            setPlanWeek(selected, true);
        }
    });

    $("#PlanSearchText").off("keydown.planWeek").on("keydown.planWeek", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            loadPlanDashboard();
        }
    });
}

function OpportunityPlan_OnProcessSuccess(response) {
    if (response && response.status !== undefined) {
        try {
            if (response.message) {
                eval(response.message);
            }
        } catch (e) {
            console.warn("eval error:", e);
        }

        if (response.status) {
            if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances["Content"]) {
                CKEDITOR.instances["Content"].destroy(true);
            }

            $(".modal.show").modal("hide");
            loadPlanDashboard();
        }
    } else {
        $("#bodyForm").html(response);
    }
}

function loadDashboard() {

    var fromDate = $("#FromDate").val();
    var toDate = $("#ToDate").val();

    $.ajax({
        url: '/Dashboard/Dashboard/OverviewFilter',
        type: 'GET',
        data: {
            FromDate: fromDate,
            ToDate: toDate
        },
        success: function (res) {
            $("#overview-container").html(res);
            initStaleUpdates();
            initProjectTable();
            initOpportunityTable();
            initDashboardCharts();
        }
    });
}

function loadEmployeeDashboard() {

    var fromDate = $("#EmpFromDate").val();
    var toDate = $("#EmpToDate").val();

    $.ajax({
        url: '/Dashboard/Dashboard/GetData',
        type: 'GET',
        data: {
            FromDate: fromDate,
            ToDate: toDate
        },
        success: function (res) {
            $("#dashboardEmployee").html(res);
            initTableOpp();
            initTablePrj();
            buildChart(res.ChartRows || []);
        }
    });
}

function initDashboardCharts() {

    var opportunityByStatus = window.opportunityByStatus || [];
    var projectByStatus = window.projectByStatus || [];

    // Chart "Co hoi theo dich vu" cu da duoc thay bang chart theo NHOM dich vu
    // (ApexCharts, ve trong _Overview.cshtml). O day chi con 2 chart cot.
    if (!document.getElementById("status-chart")) return;

    // Huy chart cu neu co.
    if (window.statusChart) window.statusChart.destroy();
    if (window.projectChart) window.projectChart.destroy();

    // STATUS CHART
    var statusLabels = opportunityByStatus.map(x => x.StatusName);
    var statusData = opportunityByStatus.map(x => x.Total);
    var statusColors = opportunityByStatus.map(x => getStatusColor(x.StatusClass));

    var statusCtx = document.getElementById("status-chart");

    window.statusChart = new Chart(statusCtx, {
        type: "bar",
        data: {
            labels: statusLabels,
            datasets: [{
                label: "Cơ hội",
                data: statusData,
                backgroundColor: statusColors
            }]
        },
        options: {
            plugins: { legend: { display: false } }
        }
    });

    // PROJECT STATUS
    var projectLabels = projectByStatus.map(x => x.StatusName);
    var projectData = projectByStatus.map(x => x.Total);
    var projectColors = projectByStatus.map(x => getStatusColor(x.StatusClass));

    var projectCtx = document.getElementById("project-status-chart");

    window.projectChart = new Chart(projectCtx, {
        type: "bar",
        data: {
            labels: projectLabels,
            datasets: [{
                label: "Dự án",
                data: projectData,
                backgroundColor: projectColors
            }]
        },
        options: {
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    });
}

function initTableOpp() {
    if (_tableOpp) { _tableOpp.destroy(); _tableOpp = null; }
    if (!$('#opportunityTable').length) return;

    _tableOpp = $('#opportunityTable').DataTable({
        responsive: false,
        autoWidth: false,
        lengthChange: true,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'Tất cả']],
        ordering: true,
        searching: false,
        language: _dtLang(),
        columnDefs: [{ orderable: false, targets: 0 }]
    });
}

function initTablePrj() {
    if (_tablePrj) { _tablePrj.destroy(); _tablePrj = null; }
    if (!$('#projectTable').length) return;

    _tablePrj = $('#projectTable').DataTable({
        responsive: false,
        autoWidth: false,
        lengthChange: true,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'Tất cả']],
        ordering: false,
        searching: false,
        language: _dtLang(),
        columnDefs: [{ orderable: false, targets: '_all' }]
    });
}

function initProjectTable() {
    if (_tablePrjOverview) { _tablePrjOverview.destroy(); _tablePrjOverview = null; }
    if (!$('#projectsTable').length) return;

    _tablePrjOverview = $('#projectsTable').DataTable({
        responsive: false,
        autoWidth: false,
        lengthChange: true,
        pageLength: 5,
        lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, 'Tất cả']],
        ordering: true,
        searching: true, // Bật search.
        language: _dtLang(),
        columnDefs: [{ orderable: false, targets: '_all' }],
    });
}

function initOpportunityTable() {
    if (_tableOppOverview) { _tableOppOverview.destroy(); _tableOppOverview = null; }
    if (!$('#opportunitysTable').length) return;

    _tableOppOverview = $('#opportunitysTable').DataTable({
        responsive: false,
        autoWidth: false,
        lengthChange: true,
        pageLength: 5,
        lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, 'Tất cả']],
        ordering: true,
        searching: true,
        language: _dtLang(),
        columnDefs: [{ orderable: false, targets: '_all' }],
    });
}

$(document).off('click.dashboardExport', '.dashboard-export').on('click.dashboardExport', '.dashboard-export', function (event) {
    event.preventDefault();

    var separator = this.href.indexOf('?') >= 0 ? '&' : '?';
    var fromDate = $('#FromDate').val() || '';
    var toDate = $('#ToDate').val() || '';
    window.location.href = this.href + separator
        + 'fromDate=' + encodeURIComponent(fromDate)
        + '&toDate=' + encodeURIComponent(toDate);
});

function normalizeDashboardUsername(value) {
    return (value || '').toString().trim().toLowerCase();
}

function getDashboardUsernameAliases(value) {
    var normalized = normalizeDashboardUsername(value);
    if (!normalized) return [];

    var aliases = [normalized];
    var atIndex = normalized.indexOf('@');
    if (atIndex > 0) {
        aliases.push(normalized.substring(0, atIndex));
    }

    return aliases.filter(function (item, index, arr) {
        return item && arr.indexOf(item) === index;
    });
}


function initPlanDashboard() {
    cleanupPlanDataTable();
    bindPlanRowActions();
    bindPlanTableFilters();
    applyPlanTableFilters();
}

initPlanTable = initPlanDashboard;

function cleanupPlanDataTable() {
    if (_planTable && $('#planTable').length && $.fn.DataTable.isDataTable('#planTable')) {
        _planTable.destroy();
    }

    _planTable = null;

    if (_planTableFilter && $.fn.dataTable && $.fn.dataTable.ext && $.fn.dataTable.ext.search) {
        var filterIndex = $.fn.dataTable.ext.search.indexOf(_planTableFilter);
        if (filterIndex >= 0) {
            $.fn.dataTable.ext.search.splice(filterIndex, 1);
        }
    }

    _planTableFilter = null;
}

function bindPlanTableFilters() {
    // Các filter của kế hoạch đều gom về một hàm để dễ kiểm soát số đếm và dòng hiển thị.
    $('#chkPlanPersonal').off('change.plan').on('change.plan', function () {
        applyPlanTableFilters();
    });

    $('#PlanSearchText').off('input.plan').on('input.plan', function () {
        var keyword = normalizeDashboardUsername($(this).val());

        if (_planSearchTimer) {
            clearTimeout(_planSearchTimer);
        }

        if (keyword.length >= 2) {
            _planSearchTimer = setTimeout(function () {
                loadPlanDashboard();
            }, 500);
            return;
        }

        applyPlanTableFilters();
    });

    $(document).off('click.planDayFilter', '[data-plan-day-filter]').on('click.planDayFilter', '[data-plan-day-filter]', function () {
        var $day = $(this);

        $('[data-plan-day-filter]').removeClass('is-active');
        $day.addClass('is-active');
        $('#planWeekStrip').attr('data-selected-date', $day.attr('data-plan-day-filter'));

        applyPlanTableFilters();
    });
}

function applyPlanTableFilters() {
    // Không dùng DataTables ở bảng kế hoạch: chỉ ẩn/hiện row theo ngày, lịch cá nhân và từ khóa.
    var selectedDate = $('#planWeekStrip').attr('data-selected-date') || '';
    var keyword = normalizeDashboardUsername($('#PlanSearchText').val());
    var onlyPersonal = $('#chkPlanPersonal').is(':checked');
    var currentAliases = getDashboardUsernameAliases(window._currentUsername);
    var visibleCount = 0;
    var weekTotal = 0;
    var dayCounts = {};
    var $firstVisibleRow = null;

    $('[data-plan-day-filter]').removeClass('is-active');
    $('[data-plan-day-filter="' + selectedDate + '"]').addClass('is-active');

    $('[data-plan-row]').each(function () {
        var $row = $(this);
        var planDate = $row.attr('data-plan-date') || '';
        var isVisible = true;
        var isCountable = true;

        if (keyword) {
            var searchText = normalizeDashboardUsername($row.attr('data-search'));
            isCountable = searchText.indexOf(keyword) >= 0;
        }

        if (isCountable && onlyPersonal && currentAliases.length) {
            var rowAliases = getDashboardUsernameAliases($row.attr('data-username'));
            isCountable = rowAliases.some(function (alias) {
                return currentAliases.indexOf(alias) >= 0;
            });
        }

        if (isCountable) {
            weekTotal++;
            dayCounts[planDate] = (dayCounts[planDate] || 0) + 1;
        }

        if (!isCountable) {
            isVisible = false;
        }

        if (isVisible && !keyword && selectedDate && planDate !== selectedDate) {
            isVisible = false;
        }

        $row.toggle(isVisible);

        if (isVisible) {
            visibleCount++;
            if (!$firstVisibleRow) {
                $firstVisibleRow = $row;
            }
        }
    });

    if (keyword && $firstVisibleRow && $firstVisibleRow.length) {
        var matchedDate = $firstVisibleRow.attr('data-plan-date');
        $('[data-plan-day-filter]').removeClass('is-active');
        $('[data-plan-day-filter="' + matchedDate + '"]').addClass('is-active');
        $('#planWeekStrip').attr('data-selected-date', matchedDate);

        var rowEl = $firstVisibleRow.get(0);
        if (rowEl && rowEl.scrollIntoView) {
            rowEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    $('#planTableEmptyRow').toggle(visibleCount === 0);
    updatePlanDayCounts(dayCounts);
    $('#planFilterTotal').text(weekTotal);
}

function updatePlanDayCounts(dayCounts) {
    // Badge trên tab ngày luôn tính theo filter tuần hiện tại, không tính riêng ngày đang chọn.
    $('[data-plan-day-filter]').each(function () {
        var $day = $(this);
        var day = $day.attr('data-plan-day-filter');
        var count = dayCounts[day] || 0;
        var $count = $day.find('.plan-week-day__count');

        if (count > 0) {
            if (!$count.length) {
                $count = $('<span class="plan-week-day__count"></span>');
                $day.append($count);
            }
            $count.text(count).show();
        } else {
            $count.remove();
        }
    });
}

function updatePlanWeekTotal() {
    var total = $('.plan-table-board').first().attr('data-week-total') || '0';
    $('#planFilterTotal').text(total);
}

function bindPlanRowActions() {
    // Xem thêm mở modal riêng để nội dung dài không làm vỡ layout bảng.
    $(document).off('click.planToggle', '.plan-toggle').on('click.planToggle', '.plan-toggle', function (e) {
        e.preventDefault();
        e.stopPropagation();

        var $toggle = $(this);
        var target = $toggle.data('target');
        var $container = $toggle.closest('.plan-card, td');
        var $full = $('#' + target + '-full');

        if (!$full.length) {
            $full = $container.find('.plan-full').first();
        }

        $('#planContentModalBody').text($full.text());
        $('#planContentModal').modal('show');
    });
}

function _dtLang() {
    return {
        lengthMenu: 'Hiển thị _MENU_ dòng',
        info: 'Trang _PAGE_ / _PAGES_ &nbsp;|&nbsp; Tổng _TOTAL_ dòng',
        infoEmpty: 'Không có dữ liệu',
        emptyTable: 'Không có dữ liệu',
        paginate: { first: '«', last: '»', next: '›', previous: '‹' },
        processing: 'Đang tải...'
    };
}

// Chart.

function hideChart() {
    if (_chart) { _chart.destroy(); _chart = null; }
    var el = document.getElementById('opportunityChart');
    if (el) el.style.display = 'none';
}

function buildChart(rows) {
    var chartEl = document.getElementById('opportunityChart');
    if (!chartEl) return;
    if (!rows || !rows.length) { hideChart(); return; }
    if (_chart) { _chart.destroy(); _chart = null; }
    chartEl.style.display = '';

    $.each(rows, function (_, r) {
        r._empID = r.employeeID || r.EmployeeID || r.fullName || r.FullName;
        r._name = r.fullName || r.FullName || '';
        r._code = r.employeeCode || r.EmployeeCode || '';
        r._stage = r.salesStageID || r.SalesStageID || 0;
        r._sname = r.stageName || r.StageName || '';
        r._cls = r.statusClass || r.StatusClass || '';
        r._count = r.count || r.Count || 0;
    });

    var empMap = {}, emps = [], stageMap = {}, stages = [];
    $.each(rows, function (_, r) {
        if (!empMap[r._empID]) {
            empMap[r._empID] = true;
            emps.push({ key: r._empID, label: r._code ? r._name + ' (' + r._code + ')' : r._name, total: 0 });
        }
        if (!stageMap[r._stage]) {
            stageMap[r._stage] = true;
            stages.push({ id: r._stage, name: r._sname, cls: r._cls });
        }
    });

    $.each(emps, function (_, e) {
        e.total = 0;
        $.each(rows, function (_, r) { if (r._empID === e.key) e.total += r._count; });
    });

    stages.sort(function (a, b) { return a.id - b.id; });

    var datasets = $.map(stages, function (st) {
        return {
            label: st.name,
            backgroundColor: getCssColor(st.cls),
            borderWidth: 0,
            borderRadius: 2,
            data: $.map(emps, function (e) {
                var f = $.grep(rows, function (r) { return r._empID === e.key && r._stage === st.id; });
                return f.length ? f[0]._count : 0;
            })
        };
    });

    var canvasHeight = Math.max(200, emps.length * 36 + 60);
    chartEl.style.height = canvasHeight + 'px';
    chartEl.style.width = '100%';

    _chart = new Chart(chartEl.getContext('2d'), {
        type: 'bar',
        data: {
            labels: $.map(emps, function (e) { return e.label; }),
            datasets: datasets
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'top', labels: { font: { size: 12 }, padding: 16 } },
                tooltip: { callbacks: { label: function (c) { return ' ' + c.dataset.label + ': ' + c.raw; } } }
            },
            scales: {
                x: { stacked: true, ticks: { stepSize: 1, font: { size: 12 } }, grid: { color: '#f0f0f0' } },
                y: { stacked: true, ticks: { font: { size: 12 } } }
            }
        }
    });
}

function getCssColor(cls) {
    if (!cls) return '#BDBDBD';
    var el = document.createElement('span');
    el.className = 'badge ' + cls;
    el.style.display = 'none';
    document.body.appendChild(el);
    var c = window.getComputedStyle(el).backgroundColor || '#BDBDBD';
    document.body.removeChild(el);
    return c;
}

function getStatusColor(statusClass) {
    if (!statusClass) return "#4e73df";

    if (statusClass.includes("badge-success")) return "#28a745";
    if (statusClass.includes("badge-secondary")) return "#6c757d";
    if (statusClass.includes("badge-primary")) return "#007bff";
    if (statusClass.includes("badge-warning")) return "#ffc107";
    if (statusClass.includes("badge-danger")) return "#dc3545";
    if (statusClass.includes("badge-info")) return "#17a2b8";
    if (statusClass.includes("badge-dark")) return "#343a40";

    return "#4e73df";
}
