var _ReviewBatchActionURLs = {
    ReviewBatch_GetData: "/Cate/ReviewBatch/Get"
};
var _tableReviewBatch;
$(document).ready(function () {
    initTableReviewBatch();
});

function initTableReviewBatch() {
    _tableReviewBatch = $("#DSReviewBatch").DataTable({
        "Responsive": true,
        "ReviewBatch": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _ReviewBatchActionURLs.ReviewBatch_GetData,
            "type": "POST",
            "dataType": "JSON",
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "BatchCode",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    if (type !== "display") return data;
                    // Bấm vào mã đợt để mở màn hình rà soát của đợt tương ứng
                    return `<a class="font-weight-bold text-primary"
                               href="/Cate/ReviewBatchItem/Index/${row.ReviewBatchID}"
                               title="Rà soát đợt này">${data || ""}</a>`;
                }
            },
            {
                "data": "BatchName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    if (type !== "display") return data;
                    // Bấm vào tên đợt để mở màn hình rà soát của đợt tương ứng
                    return `<a class="font-weight-bold text-primary"
                               href="/Cate/ReviewBatchItem/Index/${row.ReviewBatchID}"
                               title="Rà soát đợt này">${data || ""}</a>`;
                }
            },
            {
                "data": "FromDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "ToDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "ReviewBatchID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row) {
                    var html = '<span>';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button"'
                            + ' data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">'
                            + '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';

                        // Rà soát
                        html += `<a class="btn btn-lighter-success btn-a-outline-success dropdown-item"
                                    href="/Cate/ReviewBatchItem/Index/${data}">
                                    <i class="far fa-check-circle text-success text-120 mr-1"></i> Rà soát
                                 </a>`;

                        html += `<a class="btn btn-lighter-secondary btn-a-outline-secondary dropdown-item"
                                    href="/Cate/ReviewReport/Index/${data}">
                                    <i class="fas fa-eye text-secondary text-120 mr-1"></i> Xem báo cáo
                                 </a>`;

                        html += _renderButton(true,
                            "EditReviewBatch",
                            "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item",
                            "/Cate/ReviewBatch/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteReviewBatch",
                            "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item",
                            "/Cate/ReviewBatch/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá',
                            "Xoá");

                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function ReviewBatch_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableReviewBatch.ajax.reload(null, false);
                response.status = undefined;
                //$("#ModalContent #modal_" + formId + " form")[0].reset();
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function (data, textStatus, xhr) {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal",
                function () {
                    if (response.status != undefined) {
                        eval(response.message);
                        _tableReviewBatch.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
