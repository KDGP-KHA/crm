// ─── URLs ───────────────────────────────────────────────────────────────────
var _CustomerActionURLs = {
    Customer_GetData: "/Cate/Customer/Get"
};
var _tableCustomer;
$.ajaxSetup({
    headers: { "X-Requested-With": "XMLHttpRequest" }
});

// ─── Document Ready ──────────────────────────────────────────────────────────
$(document).ready(function () {
    if (!_tableCustomer) {
        initTableCustomer();
    }
    initExport();
    initImport();
});

// ─── DataTable ───────────────────────────────────────────────────────────────
function initTableCustomer() {
    _tableCustomer = $("#DSCustomer").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax": {
            "url": _CustomerActionURLs.Customer_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#Keyword").val();
                d.CustomerTypeID = $("#CustomerTypeID").val();
                d.CustomerStatusID = $("#CustomerStatusID").val();
                return d;
            }
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
                "data": "ShortName",
                "defaultContent": "",
                "render": function (data) { return data; }
            },
            {
                "data": "CustomerName",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data) { return data; }
            },
            {
                "data": "CustomerTypeName",
                "defaultContent": "",
                "render": function (data) { return data; }
            },
            {
                "data": "Email",
                "defaultContent": "",
                "render": function (data) { return data; }
            },
            {
                "data": "Phone",
                "defaultContent": "",
                "render": function (data) { return data; }
            },
            {
                "data": "StatusName",
                "defaultContent": "",
                "render": function (data, type, row) {
                    if (!data) return '';
                    return '<span class="badge ' + row.StatusClass + ' text-white mr-1">' + _escHtml(data) + '</span>';
                }
            },
            {
                "data": "CustomerID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row) {
                    var html = '<span class="">';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
                        html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';
                        html += _renderButton(true, "IndexAnniversary", "btn btn-lighter-warning mr-1 btn-a-outline-warning dropdown-item", "/Cate/Customer/IndexAnniversary/" + data, '<i class="fas fa-birthday-cake text-warning text-120 mr-1"></i> Ngày kỷ niệm', "Ngày kỷ niệm", 1024);
                        html += _renderButton(false, "IndexBusinessOpportunity", "btn btn-lighter-success mr-1 btn-a-outline-success dropdown-item", "/Cate/RM_BusinessOpportunity?id=" + data, '<i class="fas fa-business-time text-success text-120 mr-1"></i> Cơ hội kinh doanh', "Cơ hội kinh doanh", 1024);
                        html += _renderButton(true, "EditCustomer", "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item", "/Cate/Customer/Edit/" + data, '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật', "Cập nhật", 1024);
                        html += _renderButton(true, "DeleteCustomer", "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item", "/Cate/Customer/Delete/" + data, '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá', "Xoá");
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function refreshDataTable() {
    _tableCustomer.ajax.reload(null, false);
}

// ─── Framework Callback ──────────────────────────────────────────────────────
function Customer_OnProcessSuccess(response, formId) {
    if (response.status !== undefined) {
        var $modal = $("#ModalContent #modal_" + formId);
        var isKeepOpen = $modal.find("#chkNotDismissModal").is(":checked");
        eval(response.message);
        refreshDataTable();
        if (response.status === false) {
            return;
        }
        if (isKeepOpen) {
            var urlAction = $modal.find("form").attr("action");
            $modal.find("#modal-content").load(urlAction, function () {
                _initElement();
            });

        } else {
            $modal.one("hidden.bs.modal", function () {}).modal("hide");
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

// ─── Export ──────────────────────────────────────────────────────────────────
function initExport() {
    $(document).on("click", "#btnExportCustomer", function () {
        var baseUrl = "/Cate/Customer/Export";
        var keyword = $('input[name="Keyword"]').val() || "";
        var typeID = $('select[name="CustomerTypeID"]').val() || "";
        var statusID = $('select[name="CustomerStatusID"]').val() || "";
        var qs = [];
        if (keyword) qs.push("keyword=" + encodeURIComponent(keyword));
        if (typeID) qs.push("customerTypeID=" + encodeURIComponent(typeID));
        if (statusID) qs.push("customerStatusID=" + encodeURIComponent(statusID));
        var cookieName = "exportDone_" + Date.now();
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
        }, 180000);
    });
}

function showExportOverlay(show) {
    var $btn = $("#btnExportCustomer");
    if (show) {
        if ($("#exportOverlay").length === 0) {
            $("body").append(
                '<div id="exportOverlay" style="display:none;position:fixed;top:0;left:0;' +
                'width:100%;height:100%;background:rgba(0,0,0,0.45);z-index:99999;' +
                'align-items:center;justify-content:center;flex-direction:column;">' +
                '<div style="background:#fff;border-radius:10px;padding:32px 40px;text-align:center;' +
                'box-shadow:0 8px 32px rgba(0,0,0,0.18);">' +
                '<i class="fa fa-spinner fa-spin fa-3x text-primary mb-3" style="display:block;"></i>' +
                '<div style="font-size:16px;font-weight:600;color:#1F4E79;margin-bottom:6px;">Đang xuất dữ liệu...</div>' +
                '<div style="font-size:13px;color:#888;">Vui lòng chờ, không đóng trình duyệt</div>' +
                '</div></div>'
            );
        }
        $("#exportOverlay").css("display", "flex");
        $btn.prop("disabled", true).find("span").text("Đang xuất...");
    } else {
        $("#exportOverlay").hide();
        $btn.prop("disabled", false).find("span").text("Xuất biểu mẫu");
    }
}

// ─── Import ──────────────────────────────────────────────────────────────────
function initImport() {

    $(document).on("change", "#importFileInput", function () {
        var label = this.files.length > 0 ? this.files[0].name : "Chọn file...";
        $(this).next(".custom-file-label").text(label);
    });

    // Đọc & kiểm tra file
    $(document).on("click", "#btnReadFile", function () {
        var inputEl = document.getElementById("importFileInput");
        if (!inputEl || !inputEl.files || inputEl.files.length === 0) {
            toastr.warning("Vui lòng chọn file trước khi tiếp tục.");
            return;
        }
        var fd = new FormData();
        fd.append("importFile", inputEl.files[0]);
        var $btn = $(this);
        $("#uploadProgress").removeClass("d-none");
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang xử lý...');
        $.ajax({
            url: "/Cate/Customer/ImportPreview",
            type: "POST",
            data: fd,
            processData: false,
            contentType: false,
            success: function (res) {
                $("#uploadProgress").addClass("d-none");
                $btn.prop("disabled", false).html('<i class="fa fa-search"></i> Đọc & Kiểm tra file');
                if (!res.status) { toastr.error(res.message); return; }
                renderImportPreview(res);
            },
            error: function (xhr) {
                $("#uploadProgress").addClass("d-none");
                $btn.prop("disabled", false).html('<i class="fa fa-search"></i> Đọc & Kiểm tra file');
                toastr.error("Lỗi " + xhr.status + ": Không thể đọc file. Vui lòng thử lại.");
            }
        });
    });

    // Quay lại bước upload
    $(document).on("click", "#btnBackUpload", function () {
        $("#stepPreview").addClass("d-none");
        $("#stepUpload").removeClass("d-none");
        $("#footerPreview").addClass("d-none");
        $("#footerUpload").removeClass("d-none");
        $("#importFileInput").val("").next(".custom-file-label").text("Chọn file...");
        $("#btnConfirmImport").prop("disabled", false).html('<i class="fa fa-check"></i> Xác nhận nhập');
        $("#btnExportErrorRows").addClass("d-none");
    });

    // Xác nhận nhập
    $(document).on("click", "#btnConfirmImport", function () {
        var $btn = $(this);
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang nhập dữ liệu...');
        $.ajax({
            url: "/Cate/Customer/ImportConfirm",
            type: "POST",
            dataType: "json",
            dataFilter: function (data) {
                var res = JSON.parse(data);
                delete res.status;
                return JSON.stringify(res);
            },
            success: function (res) {
                $btn.prop("disabled", false).html('<i class="fa fa-check"></i> Xác nhận nhập');
                if (res.successCount > 0) {
                    toastr.success("Nhập thành công " + res.successCount + " khách hàng!");
                    refreshDataTable();
                }
                if (res.failCount > 0 && res.duplicateRows && res.duplicateRows.length > 0) {
                    var html =
                        '<div class="alert alert-warning py-2 mb-2">' +
                        '<i class="fa fa-exclamation-triangle"></i> ' +
                        '<strong>' + res.failCount + '</strong> dòng bị trùng MST hoặc tên khách hàng:' +
                        '</div>' +
                        '<table class="table table-sm table-bordered table-hover small">' +
                        '<thead style="background:#fff3cd;">' +
                        '<tr><th>#</th><th>Tên khách hàng</th><th>Mã số thuế</th><th>Lý do</th></tr>' +
                        '</thead><tbody>';
                    $.each(res.duplicateRows, function (i, r) {
                        html += '<tr>' +
                            '<td class="text-center">' + (i + 1) + '</td>' +
                            '<td>' + _escHtml(r.CustomerName || r.customerName) + '</td>' +
                            '<td>' + _escHtml(r.TaxCode || r.taxCode) + '</td>' +
                            '<td class="text-danger">' + _escHtml(r.Reason || r.reason) + '</td>' +
                            '</tr>';
                    });
                    html += '</tbody></table>';
                    $("#importResult").removeClass("d-none").html(html);
                    $("#footerPreview").addClass("d-none");
                    $("#footerDone").removeClass("d-none");
                } else if (res.successCount > 0) {    
                    setTimeout(function () {
                        $("#ModalContent .modal:visible").modal("hide");
                    }, 1500);
                }
            },
            error: function () {
                toastr.error("Có lỗi xảy ra trong quá trình nhập dữ liệu. Vui lòng thử lại.");
                $btn.prop("disabled", false).html('<i class="fa fa-check"></i> Xác nhận nhập');
            }
        });
    });

    $(document).on("click", "#btnExportErrorRows", function () {
        _exportFile($(this), "/Cate/Customer/ExportErrorRows", "exportErrorDone_", "Xuất dòng lỗi");
    });
    $(document).on("click", "#btnExportDuplicateRowsDone", function () {
        _exportFile($(this), "/Cate/Customer/ExportDuplicateRows", "exportDupDone_", "Xuất dòng trùng");
    });
}

function _exportFile($btn, url, cookiePrefix, label) {
    var cookieName = cookiePrefix + Date.now();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang xuất...');
    var $iframe = $("<iframe>").hide().appendTo("body");
    $iframe.attr("src", url + "?cookieName=" + encodeURIComponent(cookieName));
    var checkTimer = setInterval(function () {
        if (document.cookie.indexOf(cookieName + "=done") !== -1) {
            clearInterval(checkTimer);
            document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            $iframe.remove();
            $btn.prop("disabled", false).html('<i class="fa fa-download"></i> ' + label);
        }
    }, 500);
    setTimeout(function () {
        clearInterval(checkTimer);
        $iframe.remove();
        $btn.prop("disabled", false).html('<i class="fa fa-download"></i> ' + label);
    }, 180000);
}

// ─── Render preview sau khi đọc file ─────────────────────────────────────────
function renderImportPreview(res) {
    var s = '<div class="col-md-6"><div class="alert alert-success mb-0 py-2">' +
        '<i class="fa fa-check-circle"></i> <strong>' + res.totalValid + '</strong> dòng hợp lệ, sẵn sàng nhập</div></div>' +
        '<div class="col-md-6">';
    if (res.totalError > 0)
        s += '<div class="alert alert-danger mb-0 py-2"><i class="fa fa-exclamation-circle"></i> <strong>' + res.totalError + '</strong> dòng có lỗi (sẽ bỏ qua)</div>';
    else
        s += '<div class="alert alert-success mb-0 py-2"><i class="fa fa-check"></i> Không có dòng lỗi</div>';
    s += '</div>';
    $("#importSummary").html(s);
    $("#cntValid").text(res.totalValid);
    $("#cntError").text(res.totalError);

    var vh = "";
    if (res.validRows && res.validRows.length > 0) {
        $.each(res.validRows, function (i, r) {
            var statusName = r.StatusName || r.statusName || "";
            var statusCls = r.StatusClass || r.statusClass || "badge-secondary";
            vh += "<tr>" +
                '<td class="text-center">' + (r.RowNumber || r.rowNumber || "") + "</td>" +
                "<td>" + _escHtml(r.CustomerName || r.customerName) + "</td>" +
                "<td>" + _escHtml(r.ShortName || r.shortName) + "</td>" +
                "<td>" + _escHtml(r.TaxCode || r.taxCode) + "</td>" +
                "<td>" + _escHtml(r.CustomerTypeName || r.customerTypeName) + "</td>" +
                "<td>" + (statusName ? '<span class="badge ' + statusCls + '">' + _escHtml(statusName) + "</span>" : "") + "</td>" +
                "<td>" + _escHtml(r.Phone || r.phone) + "</td>" +
                "<td>" + _escHtml(r.AddressCus || r.addressCus) + "</td>" +
                "</tr>";
        });
    } else {
        vh = '<tr><td colspan="8" class="text-center text-muted py-3">Không có dữ liệu hợp lệ</td></tr>';
    }
    $("#tbodyValid").html(vh);

    var eh = "";
    if (res.errorRows && res.errorRows.length > 0) {
        $.each(res.errorRows, function (i, r) {
            var errList = r.Errors || r.errors || [];
            var errHtml = "";
            if (Array.isArray(errList) && errList.length > 0) {
                errHtml = '<ul class="mb-0 pl-3">';
                $.each(errList, function (j, e) { errHtml += "<li>" + _escHtml(e) + "</li>"; });
                errHtml += "</ul>";
            } else if (r.ErrorMessage || r.errorMessage) {
                errHtml = '<i class="fa fa-times-circle text-danger"></i> ' + _escHtml(r.ErrorMessage || r.errorMessage);
            }
            var statusName = r.StatusName || r.statusName || "";
            var statusCls = r.StatusClass || r.statusClass || "badge-secondary";
            eh += '<tr class="table-danger">' +
                '<td class="text-center">' + (r.RowNumber || r.rowNumber || "") + "</td>" +
                "<td>" + _escHtml(r.CustomerName || r.customerName) + "</td>" +
                "<td>" + _escHtml(r.ShortName || r.shortName) + "</td>" +
                "<td>" + _escHtml(r.TaxCode || r.taxCode) + "</td>" +
                "<td>" + _escHtml(r.CustomerTypeName || r.customerTypeName) + "</td>" +
                "<td>" + (statusName ? '<span class="badge ' + statusCls + '">' + _escHtml(statusName) + "</span>" : "") + "</td>" +
                "<td>" + _escHtml(r.Phone || r.phone) + "</td>" +
                "<td>" + _escHtml(r.AddressCus || r.addressCus) + "</td>" +
                '<td class="text-danger small">' + errHtml + "</td>" +
                "</tr>";
        });
    } else {
        eh = '<tr><td colspan="9" class="text-center text-muted py-3">Không có dòng lỗi</td></tr>';
    }
    $("#tbodyError").html(eh);

    $("#stepUpload").addClass("d-none");
    $("#stepPreview").removeClass("d-none");
    $("#footerUpload").addClass("d-none");
    $("#footerPreview").removeClass("d-none");

    if (res.totalValid === 0) {
        $("#btnConfirmImport").prop("disabled", true);
    }
    if (res.totalError > 0) {
        $("#btnExportErrorRows").removeClass("d-none");
        if (res.totalValid === 0) {
            $("#importTabs a[href='#tabError']").tab("show");
        }
    } else {
        $("#btnExportErrorRows").addClass("d-none");
    }
}

// ─── Utility ─────────────────────────────────────────────────────────────────
function _escHtml(str) {
    if (!str) return "";
    return $("<div>").text(str).html();
}