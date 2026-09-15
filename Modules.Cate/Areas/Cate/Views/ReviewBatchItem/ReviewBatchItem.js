var _tableReviewDigitalSales;
var _reviewDigitalSalesStateKey = "review_digital_sales_table_state";
var _reviewDigitalSalesFilterKey = "review_digital_sales_filter";

function encodeReviewDigitalSales(value) {
    return $("<div>").text(value || "").html();
}

function getReviewDigitalSalesMessages() {
    return window.reviewDigitalSalesMessages || {};
}

function buildReviewDigitalSalesUrl(row) {
    var url = "/Cate/DigitalSales/Detail/" + row.DigitalSalesID;
    var batchID = parseInt($("#ReviewDigitalSalesBatchID").val() || 0, 10);
    if (!row.IsReviewed && batchID > 0) {
        url += "?reviewBatchID=" + batchID;
    }
    return url;
}

function renderReviewDigitalSalesRecord(row) {
    var url = buildReviewDigitalSalesUrl(row);
    var badgeClass = "badge-secondary";
    if (row.StatusID === 2) badgeClass = "badge-info";
    else if (row.StatusID === 3 || row.StatusID === 6) badgeClass = "badge-danger";
    else if (row.StatusID === 7) badgeClass = "badge-primary";
    else if (row.StatusID === 8) badgeClass = "badge-success";
    else if (row.StatusID !== 1) badgeClass = "badge-warning text-dark";

    var html = '<div class="mb-1 d-flex align-items-center flex-wrap">'
        + '<span class="badge ' + badgeClass + ' px-2 py-1 sale-badge">' + encodeReviewDigitalSales(row.StatusName || "—") + '</span>'
        + '</div>';
    html += '<a class="font-weight-bold text-primary d-block sale-title" style="font-size:15px" title="Xem chi tiết 360 độ" href="' + url + '">'
        + encodeReviewDigitalSales(row.Title) + '</a>';
    html += '<div class="mt-1 d-flex align-items-center flex-wrap">'
        + '<span class="badge bgc-warning-l3 text-warning-d3 border-1 brc-warning-m2 mr-1 font-mono font-bold px-2 py-1 radius-1 shadow-sm sale-badge">'
        + '<i class="fa fa-hashtag mr-1 opacity-75"></i>' + encodeReviewDigitalSales(row.Code || "—") + '</span>';
    if (row.IsKeyProject) {
        html += '<span class="badge bgc-orange-l3 text-orange-d3 border-1 brc-orange-m2 mr-1 font-bold px-2 py-1 radius-1 shadow-sm sale-badge" title="Dự án trọng điểm">'
            + '<i class="fa fa-star text-warning mr-1"></i>Trọng điểm</span>';
    }
    if (row.IsFollowed) {
        html += '<span class="badge bgc-pink-l3 text-pink-d2 border-1 brc-pink-m3 mr-1 font-bold px-2 py-1 radius-1 shadow-sm sale-badge" title="Hồ sơ bạn đang quan tâm">'
            + '<i class="fa fa-bookmark text-danger mr-1"></i>Quan tâm</span>';
    }
    html += '</div>';
    if (row.ProductServiceNames) {
        html += '<div class="sale-subtext text-secondary mt-1"><i class="fa fa-tags text-purple mr-1"></i>'
            + encodeReviewDigitalSales(row.ProductServiceNames) + '</div>';
    }
    return html;
}

function renderReviewDigitalSalesInfo(row) {
    var messages = getReviewDigitalSalesMessages();
    if (!row || !row.LastReviewDate) {
        return '<span class="text-secondary-l1 font-italic">' + encodeReviewDigitalSales(messages.notReviewed) + '</span>';
    }

    var date = moment(row.LastReviewDate);
    var dateText = date.format("HH:mm") === "00:00" ? date.format("DD/MM/YYYY") : date.format("DD/MM/YYYY HH:mm");
    var comment = row.LastReviewComment || "";
    if (comment.length > 120) comment = comment.substring(0, 120) + "...";

    var countText = (messages.reviewCount || "{0}").replace("{0}", row.ReviewCount || 0);
    var countBadge = row.ReviewCount > 1
        ? ' <span class="badge badge-secondary">' + encodeReviewDigitalSales(countText) + '</span>'
        : "";

    var conclusion = "";
    if (row.FinalReviewConclusion) {
        var conclusionClass = row.FinalReviewConclusion === 1 ? "badge-success"
            : row.FinalReviewConclusion === 2 ? "badge-warning" : "badge-danger";
        var conclusionText = row.FinalReviewConclusion === 1 ? messages.conclusionAccepted
            : row.FinalReviewConclusion === 2 ? messages.conclusionInterested : messages.conclusionRejected;
        conclusion = '<div class="mt-1"><span class="badge ' + conclusionClass + '">'
            + encodeReviewDigitalSales(messages.conclusionLabel) + ': '
            + encodeReviewDigitalSales(conclusionText) + '</span></div>';
    }

    return '<div class="small text-muted"><i class="far fa-clock mr-1"></i>' + dateText + countBadge + '</div>'
        + '<div class="small text-primary font-weight-bold"><i class="fa fa-user mr-1"></i>' + encodeReviewDigitalSales(row.LastReviewerName) + '</div>'
        + (comment ? '<div class="small mt-1">' + encodeReviewDigitalSales(comment) + '</div>' : '')
        + conclusion;
}

function saveReviewDigitalSalesState() {
    if (!_tableReviewDigitalSales) return;
    var order = _tableReviewDigitalSales.order();
    var state = {
        start: _tableReviewDigitalSales.page.info().start,
        length: _tableReviewDigitalSales.page.len(),
        orderColumn: order.length ? order[0][0] : 1,
        orderDir: order.length ? order[0][1] : "asc"
    };
    localStorage.setItem(_reviewDigitalSalesStateKey, JSON.stringify(state));
}

function getReviewDigitalSalesState() {
    try {
        return JSON.parse(localStorage.getItem(_reviewDigitalSalesStateKey)) || {};
    } catch (error) {
        localStorage.removeItem(_reviewDigitalSalesStateKey);
        return {};
    }
}

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

function searchReviewDigitalSales() {
    saveReviewDigitalSalesFilter();
    // Reset về trang đầu khi đổi giá trị tìm kiếm, đồng bộ hành vi với _SearchProject / _SearchBusinessOpportunity
    if (_tableReviewDigitalSales) _tableReviewDigitalSales.ajax.reload();
}

function initReviewDigitalSalesTable() {
    var state = getReviewDigitalSalesState();
    var savedOrderColumn = parseInt(state.orderColumn || 1, 10);
    if ([1, 2, 3].indexOf(savedOrderColumn) < 0) savedOrderColumn = 1;
    _tableReviewDigitalSales = $("#DSDigitalSalesReview").DataTable({
        responsive: true,
        lengthChange: true,
        processing: true,
        serverSide: true,
        ordering: true,
        displayStart: state.start || 0,
        pageLength: state.length || 10,
        order: [[savedOrderColumn, state.orderDir || "asc"]],
        ajax: {
            url: "/Cate/ReviewBatchItem/GetDigitalSales",
            type: "POST",
            dataType: "JSON",
            data: function (data) {
                data.Keyword = $("#ReviewDigitalSalesKeyword").val();
                data.ReviewBatchID = $("#ReviewDigitalSalesBatchID").val() || 0;
                data.StatusID = $("#ReviewDigitalSalesStatusID").val() || 0;
                data.ProcessID = $("#ReviewDigitalSalesProcessID").val() || 0;
                data.ProgressID = $("#ReviewDigitalSalesProgressID").val() || 0;
                data.DepartmentID = $("#ReviewDigitalSalesDepartmentID").val() || 0;
                data.EmployeeID = $("#ReviewDigitalSalesEmployeeID").val() || 0;
                data.IsReviewed = String($('input[name="IsReviewed"]:checked').val()).toLowerCase() === "true";
                return data;
            }
        },
        columns: [
            {
                data: null,
                orderable: false,
                render: function (_, __, ___, meta) { return meta.settings._iDisplayStart + meta.row + 1; }
            },
            {
                data: "Title",
                className: "text-left",
                render: function (_, __, row) { return renderReviewDigitalSalesRecord(row); }
            },
            { data: "CustomerName", className: "text-left", defaultContent: "" },
            {
                data: "AssignedEmployeeName",
                className: "text-left",
                render: function (value, _, row) {
                    return '<strong>' + encodeReviewDigitalSales(value) + '</strong>'
                        + (row.DepartmentName ? '<div class="small text-secondary">' + encodeReviewDigitalSales(row.DepartmentName) + '</div>' : '');
                }
            },
            { data: null, orderable: false, className: "text-left", render: function (_, __, row) { return renderReviewDigitalSalesInfo(row); } },
            {
                data: "DigitalSalesID",
                orderable: false,
                render: function (digitalSalesID, _, row) {
                    var messages = getReviewDigitalSalesMessages();
                    var batchID = $("#ReviewDigitalSalesBatchID").val() || 0;
                    var label = row.IsReviewed ? messages.viewDetail : messages.review;
                    var url = buildReviewDigitalSalesUrl(row);
                    var icon = row.IsReviewed ? "far fa-eye" : "fa fa-clipboard-check";
                    var disabled = !row.IsReviewed && parseInt(batchID, 10) <= 0 ? " disabled" : "";
                    return '<a class="btn btn-sm btn-lighter-secondary btn-a-outline-secondary' + disabled + '" href="' + (disabled ? '#' : url) + '">'
                        + '<i class="' + icon + ' text-secondary mr-1"></i>' + encodeReviewDigitalSales(label) + '</a>';
                }
            }
        ]
    });
    _tableReviewDigitalSales.on("draw.dt order.dt length.dt page.dt", saveReviewDigitalSalesState);
}

$(function () {
    var restored = restoreReviewDigitalSalesFilter();
    var restoredEmployeeID = restored ? restored.employeeId : null;
    var restoredProcessID = restored ? restored.processId : null;
    var restoredProgressID = restored ? restored.progressId : null;
    if (typeof window.loadReviewEmployees === "function") window.loadReviewEmployees(restoredEmployeeID);
    if (typeof window.loadReviewProcesses === "function") window.loadReviewProcesses(restoredProcessID, restoredProgressID);
    initReviewDigitalSalesTable();
});

window.searchReviewDigitalSales = searchReviewDigitalSales;
