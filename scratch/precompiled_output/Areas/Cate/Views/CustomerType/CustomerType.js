var _CustomerTypeActionURLs = {
    CustomerType_GetData: "/Cate/CustomerType/Get"
};
var _tableCustomerType;
$(document).ready(function () {
    if (!_tableCustomerType) {
        initTableCustomerType();
    }
});
function initTableCustomerType() {
    _tableCustomerType = $("#DSCustomerType").DataTable({
        "responsive": false,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "paging": true,
        "searching": true,
        "ajax": {
            "url": _CustomerTypeActionURLs.CustomerType_GetData,
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
                "data": "CustomerTypeCode",
                "defaultContent": ""
            },
            {
                "data": "CustomerTypeName",
                "defaultContent": ""
            },
            {
                "data": "CustomerTypeID",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = '<span>';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
                        html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';
                        html += _renderButton(true, "EditCustomerType", "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item", "/Cate/CustomerType/Edit/" + data, '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật', "Cập nhật", 1024);
                        html += _renderButton(true, "DeleteCustomerType", "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item", "/Cate/CustomerType/Delete/" + data, '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá', "Xoá");
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ],
    });
}

function CustomerType_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableCustomerType.ajax.reload(null, false);
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
                        _tableCustomerType.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

$(document).on('show.bs.modal', '#modal_EditCustomerType', function () {
    const modal = $(this);

    modal.removeClass('modal-xl').addClass('modal-lg');
});