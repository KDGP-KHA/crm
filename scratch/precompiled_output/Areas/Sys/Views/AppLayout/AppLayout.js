var _AppLayoutActionURLs = {
    AppLayout_GetData: "/Sys/AppLayout/Get"
};
var _tableAppLayout;

$(document).ready(function () {
    initTableAppLayout();
});

function initTableAppLayout() {
    _tableAppLayout = $("#DSAppLayout").DataTable({
        "Responsive": true,
        "language": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _AppLayoutActionURLs.AppLayout_GetData,
            "type": "POST",
            "dataType": "JSON"
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
                "data": "LayoutName",
                "defaultContent": ""
            },
            {
                "data": "LayoutView",
                "defaultContent": ""
            },
            {
                "data": "NumberContentPanel",
                "defaultContent": ""
            },
            {
                "data": "NumberCol",
                "defaultContent": ""
            },
            {
                "data": "Activated",
                "defaultContent": "",
                "className": "text-center",
                "render": function (data, type, row, meta) {
                    var html = "";
                    if (data) {
                        html = '<i class="icon fa fa-check text-green"></i>';
                    }
                    return html;
                }
            },
            {
                "data": "LayoutId",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditAppLayout",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/AppLayout/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");
                        html += _renderButton(true,
                            "ChangeActiveAppLayout",
                            "btn btn-lighter-purple mr-1",
                            "/Sys/AppLayout/ChangeActive/" + data,
                            '<i class="fas fa-toggle-on text-purple text-120"></i>',
                            "Kích hoạt");

                        html += _renderButton(true,
                            "DeleteAppLayout",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/AppLayout/Delete/" + data,
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

function AppLayout_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableAppLayout.ajax.reload(null, false);
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
                        _tableAppLayout.ajax.reload(null, false);
                        response.status = undefined;
                        location.reload();
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}