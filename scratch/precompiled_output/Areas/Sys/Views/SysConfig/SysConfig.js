var _AppSettingActionURLs = {
    AppSetting_GetData: "/Sys/SysConfig/Get"
};
var _tableAppSetting;

$(document).ready(function () {
    initTableAppSetting();
});

function initTableAppSetting() {
    _tableAppSetting = $("#DSConfig").DataTable({
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
            "url": _AppSettingActionURLs.AppSetting_GetData,
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
                "data": "ConfigKey",
                "defaultContent": ""
            },
            {
                "data": "ConfigValue",
                "defaultContent": "",
                "class":"max-width-280",
                "render": function (data, type, row, meta) {
                    if (row['IsFile']) {
                        return row['DataRefFile'];
                    }
                    else {
                        if (data.length > 500) {
                            data = data.substring(0, 498) + '...';
                        }
                        return data;
                    }
                }
            },
            {
                "data": "ConfigDesc",
                "defaultContent": ""
            },
            {
                "data": "ConfigId",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditConfig",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/SysConfig/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteConfig",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/SysConfig/Delete/" + data,
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

function AppSetting_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableAppSetting.ajax.reload(null, false);
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
                        _tableAppSetting.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}