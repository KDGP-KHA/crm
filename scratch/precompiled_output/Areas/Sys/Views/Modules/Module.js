var _ModuleActionURLs = {
    Module_GetData: "/Sys/Modules/Get"
};
var _tableModule;

$(document).ready(function () {
    initTableModule();
});

function initTableModule() {
    _tableModule = $("#DSModule").DataTable({
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
            "url": _ModuleActionURLs.Module_GetData,
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
                "data": "ModuleName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = "";
                    if (type === "display") {
                        html =
                            ' <i class="{1} text-150"></i>&nbsp;<span class="text-150">{0}</span>'.format(data,
                                row["Icon"]);
                        return html;
                    }
                    return data;
                }
            },
            {
                "data": "Description",
                "defaultContent": ""
            },
            {
                "data": "ModuleId",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "PermissionModule",
                            "btn btn-lighter-purple mr-1",
                            "/Sys/Modules/Permission/" + data,
                            '<i class="fas fa-shield-alt text-purple text-120"></i>',
                            "Phân quyền truy cập");

                        html += _renderButton(true,
                            "EditModule",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/Modules/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteModule",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/Modules/Delete/" + data,
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

function Module_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableModule.ajax.reload(null, false);
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
                        _tableModule.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}