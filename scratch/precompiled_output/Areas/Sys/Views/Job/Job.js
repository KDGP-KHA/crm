
var _JobActionURLs = {
    Job_GetData: "/Sys/Job/Get"
};
var _tableJob;

$(document).ready(function () {
    initTableJob();
});

function initTableJob() {
    _tableJob = $("#DSJob").DataTable({
        "Responsive": true,
        "language": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "columnDefs": [
            { targets: [0, 1, 4, 5, 6], visible: true },
            { targets: "_all", visible: false }
        ],
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _JobActionURLs.Job_GetData,
            "type": "POST",
            "dataType": "JSON"
        },
        "columns": [
            {
                "data": "",
                "class": "text-center",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "JobName",
                "defaultContent": ""
            },
            {
                "data": "JobDescription",
                "defaultContent": ""
            },
            {
                "data": "CronExpression",
                "defaultContent": ""
            },
            {
                "data": "JobLibrary",
                "defaultContent": ""
            },
            {
                "data": "IsActive",
                "class": "text-center",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    //var html = '<h4><span class="label label-danger">Tạm dừng</span></h4>';
                    var html = '<i class="fas fa-ban text-danger fa-2x"></i>';
                    if (data) {
                        html = '<i class="fas fa-check text-green fa-2x"></i>'
                        //html = '<h4><span class="label label-success">Hoạt động</span></h4>';
                    }
                    return html;
                }
            },
            {
                "data": "JobId",
                "style": "width:80px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        var text = row.IsActive ? "Ngưng hoạt động" : "Kích hoạt lại";
                        var iconButton = row.IsActive
                            ? '<i class="fas fa-ban text-120"></i>'
                            : '<i class="fas fa-ban text-120"></i>';
                        var classButton = row.IsActive
                            ? "btn btn-outline-warning mr-1"
                            : "btn btn-outline-primary mr-1";

                        html += _renderButton(true,
                            "ExecJob",
                            "btn btn-outline-green mr-1",
                            "/Sys/Job/ExecNow/" + data,
                            '<i class="fas fa-play-circle text-120"></i>',
                            "Thực thi Job",
                            800);

                        html += _renderButton(true,
                            "ChangeStatusJob",
                            classButton,
                            "/Sys/Job/ChangeStatus/" + data,
                            iconButton,
                            text,
                            800);

                        html += _renderButton(true,
                            "EditJob",
                            "btn btn-outline-primary mr-1",
                            "/Sys/Job/Edit/" + data,
                            '<i class="far fa-edit text-120"></i>',
                            "Cập nhật",
                            800);

                        html += _renderButton(true,
                            "DeleteJob",
                            "btn btn-outline-danger mr-1",
                            "/Sys/Job/Delete/" + data,
                            '<i class="far fa-trash-alt text-120"></i>',
                            "Xoá",
                            800);
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function Job_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableJob.ajax.reload(null, false);
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
                        _tableJob.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}