var _MessageActionURLs = {
    Message_GetData: "/Sys/Message/Get"
};
var _tableMessage;

$(document).ready(function() {
    initTableMessage();
});

function initTableMessage() {
    _tableMessage = $("#TableMessage").DataTable({
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
            "url": _MessageActionURLs.Message_GetData,
            "type": "POST",
            "dataType": "JSON"
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "render": function(data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "LangCode",
                "defaultContent": ""
            },
            {
                "data": "LabelKey",
                "defaultContent": ""
            },
            {
                "data": "Message",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var truncatedMessage = data.length > 50 ? data.substring(0, 50) + "..." : data;
                    return truncatedMessage;
                }
            },
            {
                "data": "LangCode",
                "style": "width:100px;",
                "orderable": false,
                "render": function(data, type, row, meta) {
                    var key = data + row.LabelKey;
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditMessage",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/Message/Edit/" + key,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(true,
                            "DeleteMessage",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/Message/Delete/" + key,
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

function Message_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableMessage.ajax.reload(null, false);
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
                function() {
                    if (response.status != undefined) {
                        eval(response.message);
                        _tableMessage.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}