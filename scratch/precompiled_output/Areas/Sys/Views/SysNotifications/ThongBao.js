
var _ThongBaoActionURLs = {
    ThongBao_GetData: "/Sys/SysNotifications/Get"
};
var _tableThongBao;

$(document).ready(function () {
    initTableThongBao()
});

function Search() {
    _tableThongBao.ajax.reload(null, false);
}

function initTableThongBao() {
    _tableThongBao = $("#TableThongBao").DataTable({
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
            "url": _ThongBaoActionURLs.ThongBao_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "TuKhoa": function () { return $("#SearchThongBao #TuKhoa").val(); },
                "TuNgay": function () { return $("#SearchThongBao #TuNgay").val(); },
                "DenNgay": function () { return $("#SearchThongBao #DenNgay").val(); }
            }
        },
        "columns": [
            {
                "data": "",
                "className": "text-center",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.row + 1;
                }
            },
            {
                "data": "Notification_Title",
                "defaultContent": "",
                "className": "tl-left"
            },
            {
                "data": "Notification_Content",
                "defaultContent": "",
                "className": "tl-left",
                "render": function (data, type, row, meta) {
                    return stripHtml(data).length > 60 ? stripHtml(data).substring(0, 60) + "..." : data;
                }
            },
            {
                "data": "DateCreated",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    if (data != null) {
                        return moment(data).format("DD/MM/YYYY");
                    } else {
                        return "";
                    }
                }
            },
            {
                "data": "FromDate",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var tuNgay = (row.FromDate != null) ? moment(row.FromDate).format("DD/MM/YYYY HH:mm:ss") : "";
                    var denNgay = (row.ToDate != null) ? moment(row.ToDate).format("DD/MM/YYYY HH:mm:ss") : "";
                    html = `
                        <div class="bgc-info-l3 brc-info-m1 border-none radius-0 border-l-2 py-0 pl-1 pr-3 mb-1 text-sm font-weight-bold" role="alert">
                        <i class="far fa-clock w-3 my-auto text-center"></i>Từ ngày: ${tuNgay}</div>`;
                    if (denNgay != "") {
                        html += `<div class="bgc-danger-l3 brc-danger-m1 border-none radius-0 border-l-2 py-0 pl-1 pr-3 mb-1 text-sm font-weight-bold" role="alert">
                            <i class="far fa-clock w-3 my-auto text-center"></i>Đến ngày: ${denNgay}</div>`;
                    }
                    return html;
                }
            },
            {
                "data": "Sys_Notification_ID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "BienSoanThongBao",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/SysNotifications/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Chỉnh sửa", 1024);
                        html += _renderButton(true,
                            "DeleteThongBao",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/SysNotifications/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xóa");
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

function stripHtml(html) {
    let tmp = document.createElement("DIV");
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || "";
}
function ThongBao_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableThongBao.ajax.reload(null, false);
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
                        _tableThongBao.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function reloadCKEditor() {

    var editor = CKEDITOR.instances['Notification_Content'];
    if (editor) { editor.destroy(true); }
    editor = CKEDITOR.replace('Notification_Content', {
        filebrowserWindowWidth: '1000',
        filebrowserWindowHeight: '750',
    });
}