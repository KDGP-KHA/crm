var _TaskGroupActionURLs = {
    TaskGroup_GetData: "/Cate/TaskGroup/Get"
};
var _tableTaskGroup;

$(document).ready(function () {
    if (!_tableTaskGroup) {
        initTableTaskGroup();
    }
});

function initTableTaskGroup() {
    _tableTaskGroup = $("#DSTaskGroup").DataTable({
        "Responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax": {
            "url": _TaskGroupActionURLs.TaskGroup_GetData,
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
                "data": "TaskGroupCode",
                "defaultContent": ""
            },
            {
                "data": "TaskGroupName",
                "defaultContent": ""
            },
            {
                "data": "TaskGroupID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditTaskGroup",
                            "btn btn-lighter-warning mr-1 btn-a-outline-warning",
                            "/Cate/TaskGroup/Edit/" + data,
                            '<i class="far fa-edit text-warning text-120"></i>',
                            "Cập nhật", 600);
                        html += _renderButton(true,
                            "DeleteTaskGroup",
                            "btn btn-lighter-danger mr-1 btn-a-outline-danger",
                            "/Cate/TaskGroup/Delete/" + data,
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

function TaskGroup_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableTaskGroup.ajax.reload(null, false);
                response.status = undefined;
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
                        _tableTaskGroup.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}