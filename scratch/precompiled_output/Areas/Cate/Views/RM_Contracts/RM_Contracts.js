var _ContractsActionURLs = {
    Contracts_GetData: "/Cate/RM_Contracts/Get"
};
var _tableContracts;

$(document).ready(function () {
    initTableContracts();
});

function Search() {
    _tableContracts.ajax.reload(null, false);
}

function formatCurrency(value) {
    if (value == null || value === "") return "";
    return parseFloat(value).toLocaleString("vi-VN");
}

function initTableContracts() {
    _tableContracts = $("#DSContracts").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax": {
            "url": _ContractsActionURLs.Contracts_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#Keyword").val();
                d.StatusID = $("#StatusID").val();
                return d;
            }
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
                "data": "ContractCode",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "ContractName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "CustomerName",
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
                "data": "SignDate",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data != null ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "ContractValue",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return formatCurrency(data);
                }
            },
            {
                "data": "TotalAmount",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return formatCurrency(data);
                }
            },
            {
                "data": "StatusName",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return '<span class="badge ' + row.StatusClass + ' text-white mr-1"> ' + data + ' </span>';
                }
            },
            {
                "data": "UserCreated",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "ContractID",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "ViewDetailContracts",
                            "btn btn-lighter-primary mr-1",
                            "/Cate/RM_Contracts/ViewDetail/" + data,
                            '<i class="far fa-eye text-primary text-120"></i>',
                            "Xem chi tiết", 1024);
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function Contracts_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableContracts.ajax.reload(null, false);
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
                    _tableContracts.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}