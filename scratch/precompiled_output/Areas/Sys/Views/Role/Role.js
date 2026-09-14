
var _RoleActionURLs = {
    Role_GetData: "/Sys/Role/Get"
};
var _tableRole;

$(document).ready(function () {
    initTableRole();
});

function initTableRole() {
    _tableRole = $("#DSRole").DataTable({
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
            "url": _RoleActionURLs.Role_GetData,
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
                "data": "Name",
                "defaultContent": ""
            },
            {
                "data": "RoleId",
                //"style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "PermissionRole",
                            "btn btn-lighter-purple mr-1",
                            "/Sys/Role/Permission/" + data,
                            '<i class="fas fa-list-ol text-purple text-120"></i>',
                            "Phân Quyền",
                            "1024px");

                        html += _renderButton(true,
                            "UsersUserRole",
                            "btn btn-lighter-info mr-1",
                            "/Sys/User/UsersViaRole?roleId=" + data,
                            '<i class="fas fa-user-shield text-info text-120"></i>',
                            "Danh sách Tài khoản",
                            "1024px");

                        html += _renderButton(true,
                            "EditRole",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/Role/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteRole",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/Role/Delete/" + data,
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

function Role_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableRole.ajax.reload(null, false);
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
                        _tableRole.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function OnEventActionChecked(chk) {
    var selectedActions = $('input[type="hidden"][id$="SelectedActions"]').val().split(",");

    if ($(chk)[0].checked) {
        selectedActions.push($(chk).val());
    } else {
        var index = selectedActions.indexOf(($(chk).val()));
        selectedActions.splice(index, 1);
    }
    $('input[type="hidden"][id$="SelectedActions"]').val(selectedActions);
}