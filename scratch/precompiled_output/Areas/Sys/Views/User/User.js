var _UserActionURLs = {
    User_GetData: "/Sys/User/Get"
};
var _tableUser;

$(document).ready(function () {
    initTableUser();
});
function Search() {
    _tableUser.ajax.reload(null, false);
}
function initTableUser() {
    _tableUser = $("#DSUser").DataTable({
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
            "url": _UserActionURLs.User_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "BoPhanId": function () {
                    return $("#SearchUser select#BoPhanId").val();
                },
                "ChucVuId": function () {
                    return $("#SearchUser select#ChucVuId").val();
                },
                "TuKhoa": function () {
                    return $("#SearchUser #TuKhoa").val();
                },
            }
        },
        "columns": [
            {
                "data": "",
                "className": "text-center",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "FullName",
                "defaultContent": "",
                "className": "pl-2 pl-lg-4 d-flex",
                "render": function (data, type, row, meta) {
                    var avatarPath = row["AvatarPath"] == null ? "/Contents/Base/imgs/avatar-default.png" : row["AvatarPath"];
                    var html = "";
                    if (type === "display") {
                        if (data == null) data = "";
                        var extendInfo = '<ul class="list-unstyled text-dark-tp3 my-0">{0}</ul>';
                        var extendInfoItem = '<li class="mb-1 text-nowrap {2}"><i class="w-2 text-center {1} text-95"></i>&nbsp;{0}</li>';
                        var htmlExtendInfo = extendInfoItem.format(row["Email"] ?? "(Chưa cập nhật)", "fas fa-envelope", "text-green-d2");
                        htmlExtendInfo += extendInfoItem.format(row["Phone"] ?? "(chưa cập nhật)", "fas fa-mobile", "text-green-d2");
                        html = ' {2}<div class="mx-2 text-grey-d1 my-auto text-left"><div class="text-600 text-blue-d1"><span class="text-95 btn-text-dark btn-h-text-primary">{0}</span></div>{1}</div>'
                            .format(data, extendInfo.format(htmlExtendInfo), '<img alt="{0}" src="{1}" class="radius-round mr-2 w-5 h-5 my-auto">'.format(data, avatarPath));
                        return html;
                    }
                    return data;
                }
            },
            {
                "data": "UserName",
                "defaultContent": ""
            },
            //{
            //    "data": "TenBoPhan",
            //    "defaultContent": "",
            //    "className": "text-left",
            //},
            //{
            //    "data": "TenChucVu",
            //    "defaultContent": ""
            //},
            {
                "data": "UserId",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block"><button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-ellipsis-h text-120"></i></button><div class="dropdown-menu dropdown-menu-right">';
                        if (row.IsActive) {
                            html += _renderButton(true,
                                "EditUser",
                                "btn btn-outline-success btn-a-outline-success mr-1 dropdown-item",
                                "/Sys/User/Edit/" + data,
                                '<i class="fa fa-edit text-120 mr-1"></i> Cập nhật',
                                "Cập nhật", 1024);
                            html += _renderButton(true,
                                "PermitUser",
                                "btn btn-outline-warning btn-a-outline-warning mr-1 dropdown-item",
                                "/Sys/User/Permit/" + data,
                                '<i class="fas fa-user-shield text-120 mr-1"></i> Quyền',
                                "Quyền");

                            html += _renderButton(true,
                                "BoPhanUser",
                                "btn btn-outline-info btn-a-outline-info mr-1 dropdown-item",
                                "/Sys/User/BoPhan/" + data,
                                '<i class="fas fa-sitemap text-120 mr-1"></i> Phân đơn vị',
                                "Phân đơn vị");

                            html += _renderButton(true,
                                "PermitReviewUser",
                                "btn btn-outline-purple btn-a-outline-purple mr-1 dropdown-item",
                                "/Sys/User/PermitReview/" + data,
                                '<i class="fas fa-user-edit text-120 mr-1"></i> Phân quyền rà soát',
                                "Phân quyền rà soát");

                            html += _renderButton(true,
                                "ChangePassword",
                                "btn btn-outline-primary btn-a-outline-primary mr-1 dropdown-item",
                                "/Sys/User/ChangePassword/" + data,
                                '<i class="fa fa-key text-120 mr-1"></i> Đổi mật khẩu',
                                "Đổi mật khẩu");

                            html += _renderButton(true,
                                "DeActiveUser",
                                "btn btn-outline-danger btn-a-outline-danger mr-1 dropdown-item",
                                "/Sys/User/DeActive/" + data,
                                '<i class="fas fa-user-lock text-120 mr-1"></i> Ngưng hoạt động',
                                "Ngưng hoạt động");

                            html += _renderButton(true,
                                "ResetPassword",
                                "btn btn-outline-purple btn-a-outline-purple mr-1 dropdown-item",
                                "/Sys/User/ResetPassword/" + data,
                                '<i class="fa fa-paper-plane text-120 mr-1"></i> Reset mật khẩu',
                                "Reset mật khẩu");

                            html += _renderButton(true,
                                "DeleteUser",
                                "btn btn-outline-danger btn-a-outline-danger mr-1 dropdown-item",
                                "/Sys/User/Delete/" + data,
                                '<i class="fas fa-trash-alt text-120 mr-1"></i> Xóa tài khoản',
                                "Xóa tài khoản");
                        } else {

                            html += _renderButton(true,
                                "ActiveUser",
                                "btn btn-outline-primary btn-a-outline-primary mr-1 dropdown-item",
                                "/Sys/User/Active/" + data,
                                '<i class="fa fa-check-square text-120 mr-1"></i> Kích hoạt',
                                "Kích hoạt");

                            html += _renderButton(true,
                                "DeleteUser",
                                "btn btn-outline-danger btn-a-outline-danger mr-1 dropdown-item",
                                "/Sys/User/Delete/" + data,
                                '<i class="fas fa-trash-alt text-120 mr-1"></i> Xóa tài khoản',
                                "Xóa tài khoản");
                        }
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ],
        "createdRow": function (row, data, dataIndex) {
            $(row).addClass("d-style bgc-h-default-l4");
        }
    });
}

function User_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableUser.ajax.reload(null, false);
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
                        _tableUser.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else if ($(response).hasClass("modal-header")) {
        $("#ModalContent #modal_" + formId + " #modal-content").html(response);
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}