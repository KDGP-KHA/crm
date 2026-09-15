var _RevenueReceivedActionURLs = {
    RevenueReceived_GetData: "/Cate/RevenueReceived/Get"
};
var _tableRevenueReceived;
$(document).ready(function () {
    initTableRevenueReceived();
});

function initTableRevenueReceived() {
    _tableRevenueReceived = $("#DSRevenueReceived").DataTable({
        "Responsive": true,
        "RevenueReceived": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _RevenueReceivedActionURLs.RevenueReceived_GetData,
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
                "data": "ProjectName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "NameProduct",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "Amount",
                "defaultContent": "",
                "render": $.fn.dataTable.render.number('.', ',', 0, '')
            },
            {
                "data": "ReceivedDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "ReceivedTime",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "RevenueReceivedID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';

                    if (type === "display") {
                        html += _renderButton(true,
                            "EditRevenueReceived",
                            "btn btn-lighter-primary mr-1",
                            "/Cate/RevenueReceived/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteRevenueReceived",
                            "btn btn-lighter-danger mr-1",
                            "/Cate/RevenueReceived/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xoá");
                    }
                    html += "</span>";

                    return html;
                }
            }
        ]
    });
}


function RevenueReceived_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableRevenueReceived.ajax.reload(null, false);
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
                        _tableRevenueReceived.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
