var _MenuActionURLs = {
    Menu_GetData: "/Sys/Menu/Get"
};
var _tableMenu;

$(document).ready(function () {
    initTableMenu();
});

function initTableMenu() {
    _tableMenu = $("#TableMenu").DataTable({
        "Responsive": true,
        "language": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "colReorder": true,
        "info": true,
        "autoWidth": false,
        "ajax":
        {
            "url": _MenuActionURLs.Menu_GetData,
            "type": "POST",
            "dataType": "JSON"
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
                "data": "Name",
                "defaultContent": "",
                "orderable": false,
                "className": "text-left",
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
                "data": "Link",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return (data != null)
                        ? (data.length > 30 ? (data.substring(0, 30) + '...') : data)
                        : "";
                }
            },
            {
                "data": "FunctionName",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return (data != null)
                        ? (data.length > 30 ? (data.substring(0, 30) + '...') : data)
                        : "";
                }
            },
            {
                "data": "UseModal",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = "";
                    if (type === "display") {
                        if (data) {
                            html = '<i class="fa fa-check text-green fa-2x"></i>';
                        }
                        return html;
                    }
                    return data
                }
            },
            {
                "data": "MenuId",
                "style": "width:100px;",
                "className": "align-middle",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {

                        html += _renderButton(true,
                            "EditMenu",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/Menu/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteMenu",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/Menu/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xoá");
                    }
                    html += "</span>";
                    return html;
                }
            }
        ],
        "createdRow": function (row, data, dataIndex) {
            $(row).addClass("d-style");
        }
    });
}

function Menu_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableMenu.ajax.reload(null, false);
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
                        _tableMenu.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}