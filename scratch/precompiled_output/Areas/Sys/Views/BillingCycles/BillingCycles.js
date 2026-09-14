var _BillingCyclesActionURLs = {
    BillingCycles_GetData: "/Sys/BillingCycles/Get"
};
var _tableBillingCycles;

$(document).ready(function () {
    initTableBillingCycles();
});

function Search() {
    _tableBillingCycles.ajax.reload(null, false);
}

function initTableBillingCycles() {
    _tableBillingCycles = $("#DSBillingCycles").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": true,
        "order": [[1, "desc"]],
        "ajax": {
            "url": _BillingCyclesActionURLs.BillingCycles_GetData,
            "type": "POST",
            "dataType": "JSON",
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "CycleName",
                "defaultContent": "",
                "className": "text-left",
            },
            {
                "data": "Islimited",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? `<i class="fas fa-check text-green"></i>`: ``;
                }
            },

            {
                "data": "CycleType",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return row.Islimited == false ? `Không thời hạn`
                        : (
                            row.Quantity + " " + (row.CycleType == "DAY" ? "Ngày" : (row.CycleType == "MONTH" ? "Tháng" : "Năm"))
                        );
                }
            },
            {
                "data": "BillingCycleID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditBillingCycles",
                            "btn btn-lighter-primary mr-1 mb-1 text-120",
                            "/Sys/BillingCycles/Edit/" + data,
                            '<i class="far fa-edit"></i>',
                            "Cập nhật");
                        html += _renderButton(true,
                            "DeleteBillingCycles",
                            "btn btn-lighter-danger mr-1 mb-1 text-120",
                            "/Sys/BillingCycles/Delete/" + data,
                            '<i class="far fa-trash-alt"></i>',
                            "Xoá");
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function BillingCycles_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableBillingCycles.ajax.reload(null, false);
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function (data, textStatus, xhr) {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    _tableBillingCycles.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
