var _ProjectActionURLs = {
    Project_GetData: "/Cate/Project/Get"
};
var _tableProject;

$(document).ready(function () {
    initTableProject();
});

function Search() {
    _tableProject.ajax.reload(null, true);
}

function initTableProject() {
    _tableProject = $("#DSProject").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": true,
        "order": [[1, "desc"]],
        "ajax": {
            "url": _ProjectActionURLs.Project_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#SearchProject #Keyword").val();
                d.CustomerTypeID = $("#SearchProject #CustomerTypeID").val();
                d.StatusIDs = $('.status-item:checked')
                    .map(function () {
                        return $(this).val();
                    })
                    .get()
                    .join(',');
                d.Year = $("#SearchProject #Year").val();
                d.BoPhanID = $("#SearchProject #BoPhanID").val();
                d.EmployeeID = $("#SearchProject #EmployeeID").val();
                d.SuccessRateFrom = $("#SuccessRateFrom").val();
                d.SuccessRateTo = $("#SuccessRateTo").val();
                return d;
            }
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
                "data": "ProjectName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    return `
                        <div class="font-weight-bold text-primary">
                            <a href="/Cate/ProjectOverview/Index/${row.ProjectID}">
                                ${row.ProjectName}
                            </a>
                        </div>
                    `
                }
            },
            {
                "data": "SuccessRate",
                "defaultContent": "",
                //"render": function (data, type, row, meta) {
                //    return data;
                //}
            },
            {
                "data": "CustomerName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "StartDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
                }
            },
            {
                "data": "StatusName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return '<span class="badge ' + row.StatusClass + ' text-white mr-1"> ' + data + ' </span>';
                }
            },
            {
                "data": "ProjectID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
                        html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';
                        html += '<a class="btn btn-lighter-secondary mr-1 btn-a-outline-secondary dropdown-item" href="/Cate/ProjectOverview/Index/' + data + '">';
                        html += '<i class="far fa-eye text-secondary text-120 mr-1"></i> Xem chi tiết</a>';
                        html += _renderButton(true, "EditProject",
                            "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item",
                            "/Cate/Project/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật', "Cập nhật", 1024);
                        if (row.CanDelete) {
                            html += _renderButton(
                                true,
                                "DeleteProject",
                                "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item",
                                "/Cate/Project/Delete/" + data,
                                '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá',
                                "Xoá"
                            );
                        }
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function Project_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableProject.ajax.reload(null, false);
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function (data, textStatus, xhr) {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    _tableProject.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

$(document).ready(function () {
    initExport();
});

function initExport() {
    $(document).on("click", "#btnExportProject", function () {
        var baseUrl = "/Cate/Project/Export";
        var keyword = $("#SearchProject #Keyword").val() || "";
        var customerTypeID = $("#SearchProject #CustomerTypeID").val() || "";
        var statusID = $("#SearchProject #StatusID").val() || "";

        var qs = [];
        if (keyword) qs.push("keyword=" + encodeURIComponent(keyword));
        if (customerTypeID) qs.push("customerTypeID=" + encodeURIComponent(customerTypeID));
        if (statusID) qs.push("statusID=" + encodeURIComponent(statusID));

        var cookieName = "expProj_" + Date.now();
        qs.push("cookieName=" + cookieName);

        showExportOverlay(true);

        var $iframe = $("<iframe>").hide().appendTo("body");
        $iframe.attr("src", baseUrl + (qs.length ? "?" + qs.join("&") : ""));

        var checkTimer = setInterval(function () {
            if (document.cookie.indexOf(cookieName + "=done") !== -1) {
                clearInterval(checkTimer);
                document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                $iframe.remove();
                showExportOverlay(false);
            }
        }, 500);
        setTimeout(function () {
            clearInterval(checkTimer);
            $iframe.remove();
            showExportOverlay(false);
        }, 300000);
    });
}

function showExportOverlay(show) {
    var $btn = $("#btnExportProject");
    if (show) {
        if ($("#exportProjectOverlay").length === 0) {
            $("body").append(
                '<div id="exportProjectOverlay" style="display:none;position:fixed;top:0;left:0;' +
                'width:100%;height:100%;background:rgba(0,0,0,0.45);z-index:99999;' +
                'align-items:center;justify-content:center;flex-direction:column;">' +
                '<div style="background:#fff;border-radius:10px;padding:32px 40px;text-align:center;' +
                'box-shadow:0 8px 32px rgba(0,0,0,0.18);">' +
                '<i class="fa fa-spinner fa-spin fa-3x text-primary mb-3" style="display:block;"></i>' +
                '<div style="font-size:16px;font-weight:600;color:#1F4E79;margin-bottom:6px;">Đang xuất dữ liệu...</div>' +
                '<div style="font-size:13px;color:#888;">Vui lòng chờ, không đóng trình duyệt</div>' +
                '<div style="font-size:12px;color:#aaa;margin-top:6px;">File bao gồm 4 sheet: Dự án, Sản phẩm, Thành viên, Công việc</div>' +
                '</div></div>'
            );
        }
        $("#exportProjectOverlay").css("display", "flex");
        $btn.prop("disabled", true).find("span").text("Đang xuất...");
    } else {
        $("#exportProjectOverlay").hide();
        $btn.prop("disabled", false).find("span").text("Xuất biểu mẫu");
    }
}