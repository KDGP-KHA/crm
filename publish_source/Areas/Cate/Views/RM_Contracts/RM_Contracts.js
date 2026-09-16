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
                "render": function (data, type, row) {
                    var name = row.ContractName || "";
                    return '<div class="font-weight-bold text-primary">' + (data || "") + '</div>' +
                        '<div class="text-dark-m2 text-90 mt-1">' + name + '</div>';
                }
            },
            {
                "data": "DigitalSalesName",
                "defaultContent": "",
                "render": function (data, type, row) {
                    if (!data || !row.DigitalSalesID) return '<span class="text-secondary">—</span>';
                    var code = row.DigitalSalesCode ? '<small class="d-block text-secondary">' + row.DigitalSalesCode + '</small>' : '';
                    var product = row.NameProduct ? '<div class="text-dark-m2 text-90 mt-1"><i class="fa fa-cubes text-purple-m1 mr-1"></i>' + row.NameProduct + '</div>' : '';
                    return '<a class="font-weight-bold text-primary" href="/Cate/DigitalSales/Detail/' + row.DigitalSalesID + '" title="Mở hồ sơ KD SPDV số">' + data + '</a>' + code + product;
                }
            },
            {
                "data": "CustomerName",
                "orderable": false,
                "defaultContent": "",
                "render": function (data) {
                    return data ? '<i class="fa fa-building text-secondary mr-1"></i>' + data : '';
                }
            },
            {
                "data": "TotalAmount",
                "orderable": false,
                "defaultContent": "",
                "render": function (data, type, row) {
                    var signed = row.SignDate ? moment(row.SignDate).format("DD/MM/YYYY") : "Chưa ký";
                    var value = formatCurrency(data || row.ContractValue);
                    return '<div class="font-weight-bold text-success text-105">' + value + ' triệu VNĐ</div>' +
                        '<small class="text-secondary">Ký: ' + signed + '</small>';
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
