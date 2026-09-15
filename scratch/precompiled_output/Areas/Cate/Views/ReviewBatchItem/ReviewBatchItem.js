var _ProjectActionURLs = {
    Project_GetData: "/Cate/ReviewBatchItem/GetProject"
};

var _BusinessOpportunityActionURLs = {
    BusinessOpportunity_GetData: "/Cate/ReviewBatchItem/GetBusinessOpportunity"
};

var _tableProject;
var _tableBusinessOpportunity;

/**
 * Cột "Thông tin rà soát trong đợt": thời gian, người thực hiện, nội dung
 * của lượt rà soát mới nhất; kèm tổng số lượt nếu có nhiều hơn 1.
 */
function renderReviewInfo(row) {
    if (!row || !row.LastReviewDate) {
        return '<span class="text-secondary-l1 font-italic">Chưa rà soát</span>';
    }

    var m = moment(row.LastReviewDate);
    var dateStr = m.format("HH:mm") === "00:00"
        ? m.format("DD/MM/YYYY")
        : m.format("DD/MM/YYYY HH:mm");

    var comment = row.LastReviewComment || "";
    if (comment.length > 120) {
        comment = comment.substring(0, 120) + "...";
    }

    var moreBadge = row.ReviewCount > 1
        ? ' <span class="badge badge-secondary">' + row.ReviewCount + ' lượt</span>'
        : '';

    return '<div class="small text-muted"><i class="far fa-clock mr-1"></i>' + dateStr + moreBadge + '</div>'
        + '<div class="small text-primary font-weight-bold"><i class="fa fa-user mr-1"></i>' + (row.LastReviewerName || "") + '</div>'
        + (comment ? '<div class="small mt-1">' + $("<div/>").text(comment).html() + '</div>' : '');
}

$(document).ready(function () {

    restoreProjectTableState();
    restoreBusinessTableState();

    initTableProject();
    initTableBusinessOpportunity();
});

/* =========================================================
   PROJECT
========================================================= */

function saveProjectTableState() {

    if (!_tableProject) return;

    const order = _tableProject.order();

    const state = {
        start: _tableProject.page.info().start,
        length: _tableProject.page.len(),
        orderColumn: order[0][0],
        orderDir: order[0][1]
    };

    localStorage.setItem(
        "review_project_table_state",
        JSON.stringify(state)
    );
}

function restoreProjectTableState() {

    const state = JSON.parse(
        localStorage.getItem("review_project_table_state")
    );

    if (!state) return;

    _projectReviewStart = state.start || 0;
    _projectReviewLength = state.length || 10;
    _projectReviewOrder = state.orderColumn || 1;
    _projectReviewOrderDir = state.orderDir || "desc";
}

function SearchProject() {

    saveProjectFilter();

    if (_tableProject) {
        _tableProject.ajax.reload(null, false);
    }
}

function initTableProject() {

    _tableProject = $("#DSProject").DataTable({

        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": true,

        // restore paging
        "displayStart": _projectReviewStart || 0,

        // restore page size
        "pageLength": _projectReviewLength || 10,

        // restore sort
        "order": [[
            parseInt(_projectReviewOrder || 1),
            _projectReviewOrderDir || "desc"
        ]],

        "ajax": {
            "url": _ProjectActionURLs.Project_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#ProjectKeyword").val();
                d.ReviewBatchID = $("#ProjectReviewBatchID").val();
                d.Status = $("#ProjectStatus").val();
                d.BoPhanID = $("#ProjectBoPhanID").val();
                d.EmployeeID = $("#ProjectEmployeeID").val();
                d.IsReviewed = $('input[name="ProjectIsReviewed"]:checked').val();
                return d;
            }
        },

        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart
                        + meta.row + 1;
                }
            },
            {
                "data": "ProjectName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row) {
                    let reviewBatchID = $("#ProjectReviewBatchID").val() || 0;
                    let isReviewed = $('input[name="ProjectIsReviewed"]:checked').val() === "true";

                    let url = isReviewed
                        ? `/Cate/ProjectOverview/Index/${row.ProjectID}`
                        : `/Cate/ProjectOverview/Index/${row.ProjectID}?reviewBatchID=${reviewBatchID}`;

                    return `
                        <div class="font-weight-bold text-primary">
                            <a href="${url}">
                                ${row.ProjectName}
                            </a>
                        </div>
                        <div>
                            <span class="badge ${row.StatusClass} text-white mr-1">
                                ${row.StatusName}
                            </span>
                        </div>
                    `;
                }
            },
            {
                "data": "CustomerName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data) {
                    return data || "";
                }
            },
            {
                "data": "StartDate",
                "defaultContent": "",
                "render": function (data) {
                    return data
                        ? moment(data).format("DD/MM/YYYY")
                        : "";
                }
            },
            {
                "data": "AMName",
                "defaultContent": "",
                "render": function (data, type, row) {
                    if (!data) return "";
                    return "<b>" +
                        data.split(",").map(x => x.trim()).join("<br/>") +
                        "</b>";
                }
            },
            {
                "data": null,
                "className": "text-left",
                "orderable": false,
                "render": function (data, type, row) {
                    return renderReviewInfo(row);
                }
            },
            {
                "data": "ProjectID",
                "style": "width:100px;",
                "orderable": false,

                "render": function (data, type, row) {
                    if (type === "display") {
                        let reviewBatchID = $("#ProjectReviewBatchID").val() || 0;
                        let isReviewed = $('input[name="ProjectIsReviewed"]:checked').val() === "true";

                        let url = isReviewed
                            ? `/Cate/ProjectOverview/Index/${data}`
                            : `/Cate/ProjectOverview/Index/${data}?reviewBatchID=${reviewBatchID}`;

                        return `
                            <a class="btn btn-sm btn-lighter-secondary btn-a-outline-secondary"
                               href="${url}">
                                <i class="far fa-eye text-secondary mr-1"></i>
                                ${isReviewed ? "Xem chi tiết" : "Rà soát"}
                            </a>
                        `;
                    }
                    return data;
                }
            }
        ]
    });

    // save state
    _tableProject.on("draw.dt order.dt length.dt page.dt", function () {
        saveProjectTableState();
    });
}

/* =========================================================
   BUSINESS OPPORTUNITY
========================================================= */

function saveBusinessTableState() {

    if (!_tableBusinessOpportunity) return;

    const order = _tableBusinessOpportunity.order();

    const state = {
        start: _tableBusinessOpportunity.page.info().start,
        length: _tableBusinessOpportunity.page.len(),
        orderColumn: order[0][0],
        orderDir: order[0][1]
    };

    localStorage.setItem("review_business_table_state", JSON.stringify(state));
}

function restoreBusinessTableState() {

    const state = JSON.parse(
        localStorage.getItem("review_business_table_state")
    );

    if (!state) return;

    _boReviewStart = state.start || 0;
    _boReviewLength = state.length || 10;
    _boReviewOrder = state.orderColumn || 1;
    _boReviewOrderDir = state.orderDir || "desc";
}

function SearchBusinessOpportunity() {

    saveBusinessOpportunityFilter();

    if (_tableBusinessOpportunity) {
        _tableBusinessOpportunity.ajax.reload(null, false);
    }
}

function initTableBusinessOpportunity() {

    _tableBusinessOpportunity = $("#DSBusinessOpportunity").DataTable({

        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": true,

        // restore paging
        "displayStart": _boReviewStart || 0,

        // restore page size
        "pageLength": _boReviewLength || 10,

        // restore sort
        "order": [[
            parseInt(_boReviewOrder || 1),
            _boReviewOrderDir || "desc"
        ]],

        "ajax": {
            "url": _BusinessOpportunityActionURLs.BusinessOpportunity_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#BusinessOpportunityKeyword").val();
                d.ReviewBatchID = $("#BusinessOpportunityReviewBatchID").val();
                d.StatusID = $("#BusinessOpportunityStatusID").val();
                d.BoPhanID = $("#BusinessOpportunityBoPhanID").val();
                d.EmployeeID = $("#BusinessOpportunityEmployeeID").val();
                d.IsReviewed = $('input[name="BusinessOpportunityIsReviewed"]:checked').val();
                return d;
            }
        },

        "columns": [

            {
                "data": "",
                "defaultContent": "1",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart
                        + meta.row + 1;
                }
            },
            {
                "data": "OpportunityName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row) {
                    let reviewBatchID = $("#BusinessOpportunityReviewBatchID").val() || 0;
                    let isReviewed = $('input[name="BusinessOpportunityIsReviewed"]:checked').val() === "true";

                    let url = isReviewed
                        ? `/Cate/BusinessOpportunityOverview/Index/${row.BusinessOpportunityID}`
                        : `/Cate/BusinessOpportunityOverview/Index/${row.BusinessOpportunityID}?reviewBatchID=${reviewBatchID}`;

                    return `
                        <div class="font-weight-bold text-primary">
                            <a href="${url}">
                                ${row.OpportunityName}
                            </a>
                        </div>
                        <div>
                            <span class="badge ${row.StatusClass} text-white mr-1">
                                ${row.StatusName}
                            </span>
                        </div>
                    `;
                }
            },
            {
                "data": "CustomerName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data) {
                    return data || "";
                }
            },
            {
                "data": "ClosingProbability",
                "defaultContent": "",
                "render": function (data) {
                    return data + "%" || "";
                }
            },
            {
                "data": "AMName",
                "defaultContent": "",
                "render": function (data, type, row) {

                    if (!data) return "";

                    return "<b>" +
                        data.split(",").map(x => x.trim()).join("<br/>") +
                    "</b>";
                }
            },
            {
                "data": null,
                "className": "text-left",
                "orderable": false,
                "render": function (data, type, row) {
                    return renderReviewInfo(row);
                }
            },
            {
                "data": "BusinessOpportunityID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row) {
                    if (type === "display") {
                        let reviewBatchID = $("#BusinessOpportunityReviewBatchID").val() || 0;
                        let isReviewed = $('input[name="BusinessOpportunityIsReviewed"]:checked').val() === "true";

                        let url = isReviewed
                            ? `/Cate/BusinessOpportunityOverview/Index/${data}`
                            : `/Cate/BusinessOpportunityOverview/Index/${data}?reviewBatchID=${reviewBatchID}`;

                        return `
                            <a class="btn btn-sm btn-lighter-secondary btn-a-outline-secondary"
                               href="${url}">
                                <i class="far fa-eye text-secondary mr-1"></i>
                                ${isReviewed ? "Xem chi tiết" : "Rà soát"}
                            </a>
                        `;
                    }
                    return data;
                }
            }
        ]
    });

    // save state
    _tableBusinessOpportunity.on(
        "draw.dt order.dt length.dt page.dt",
        function () {
            saveBusinessTableState();
        });
}