var _TaskActionURLs = { Task_GetData: "/Cate/Task/Get" };
var _tableTask;

$(document).ready(function () {
    if (!_tableTask) initTableTask();

    $(document).off("click.taskFilter").on("click.taskFilter", "#btnFilter", function () {
        _tableTask.ajax.reload(null, false);
    });

    $(document).off("click.taskClear").on("click.taskClear", "#btnClearFilter", function () {
        $("#filterKeyword").val("");
        $("#filterTaskGroup").val("").trigger("change");
        _tableTask.ajax.reload(null, false);
    });

    $(document).off("keypress.taskFilter").on("keypress.taskFilter", "#filterKeyword", function (e) {
        if (e.which === 13) _tableTask.ajax.reload(null, false);
    });

    $(document).off("select2:select.taskFilter select2:unselect.taskFilter")
        .on("select2:select.taskFilter select2:unselect.taskFilter", "#filterTaskGroup", function () {
            _tableTask.ajax.reload(null, false);
        });
});

function initTableTask() {
    _tableTask = $("#DSTask").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax": {
            "url": _TaskActionURLs.Task_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.filterKeyword = $("#filterKeyword").val();
                d.filterTaskGroup = $("#filterTaskGroup").val();
                return d;
            }
        },
        "columns": [
            {
                "data": "", "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            { "data": "TaskCode", "defaultContent": "" },
            { "data": "TaskName", "defaultContent": "" },
            { "data": "ParentsTask", "defaultContent": "" },
            { "data": "TaskGroupName", "defaultContent": "" },
            {
                "data": "Note", "defaultContent": "",
                "render": function (data) {
                    if (!data) return '';
                    return data.length > 50 ? data.substring(0, 50) + '...' : data;
                }
            },
            {
                "data": "TaskID", "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span>';
                    if (type === "display") {
                        html += _renderButton(true,
                            "EditTask",
                            "btn btn-lighter-primary mr-1 btn-a-outline-primary",
                            "/Cate/Task/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật", 700);
                        html += _renderButton(true,
                            "DeleteTask",
                            "btn btn-lighter-danger mr-1 btn-a-outline-danger",
                            "/Cate/Task/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xoá");
                    }
                    return html + '</span>';
                }
            }
        ]
    });
}

function Task_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            eval(response.message);
            _tableTask.ajax.reload(null, false);
            response.status = undefined;
            var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
            $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () { _initElement(); });
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    _tableTask.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}