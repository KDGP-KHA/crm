var _tableContactPersons;

function htmlEncode(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

$(document).ready(function () {
    initTableContactPersons();
    initExportContactPersons();
    ContactPersons_LoadSummary();
});

function getIntOrNull(val) {
    if (val === undefined || val === null || val === "") return null;
    var n = parseInt(val);
    return isNaN(n) ? null : n;
}

function SearchContactPersons() {
    if (_tableContactPersons) {
        _tableContactPersons.ajax.reload();
    }
}

function ResetContactPersons() {
    $("#cpKeyword").val("");
    $("#cpCustomerID").val("");
    $("#cpGender").val("");
    $("#cpStatus").val("");
    $(".cp-metric-card").removeClass("active");
    SearchContactPersons();
}

function ContactPersons_LoadSummary() {
    $.ajax({
        url: "/Cate/ContactPersons/GetSummary",
        type: "POST",
        dataType: "json",
        success: function (res) {
            if (res) {
                $("#kpi_total_contacts").text(res.total || 0);
                $("#kpi_active_contacts").text(res.active || 0);
                $("#kpi_inactive_contacts").text(res.inactive || 0);
                $("#kpi_total_customers").text(res.totalCustomers || 0);
            }
        },
        error: function () {
            // Fallback gracefully
        }
    });
}

function ContactPersons_FilterByMetric(type) {
    $(".cp-metric-card").removeClass("active");
    if (type === "all") {
        $("#kpi_card_total").addClass("active");
        $("#cpStatus").val("");
    } else if (type === "active") {
        $("#kpi_card_active").addClass("active");
        $("#cpStatus").val("1");
    } else if (type === "inactive") {
        $("#kpi_card_inactive").addClass("active");
        $("#cpStatus").val("0");
    } else if (type === "customers") {
        $("#kpi_card_customers").addClass("active");
        $("#cpCustomerID").focus();
        return;
    }
    SearchContactPersons();
}

function initTableContactPersons() {
    _tableContactPersons = $("#DSContactPersons").DataTable({
        responsive: true,
        lengthChange: true,
        processing: true,
        serverSide: true,
        ordering: true,
        order: [[1, "asc"]],
        ajax: {
            url: "/Cate/ContactPersons/Get",
            type: "POST",
            dataType: "JSON",
            data: function (d) {
                d.Keyword = $("#cpKeyword").val();
                d.CustomerID = getIntOrNull($("#cpCustomerID").val());
                d.Gender = getIntOrNull($("#cpGender").val());
                d.Status = getIntOrNull($("#cpStatus").val());
            }
        },
        columns: [
            {
                // Cột STT
                data: null,
                orderable: false,
                className: "text-center text-secondary font-weight-bold",
                render: function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                // Mã NLH
                data: "CodePerson",
                className: "text-center",
                render: function (data) {
                    if (!data) return '<span class="text-muted">—</span>';
                    return '<span class="cp-code-badge">' + htmlEncode(data) + '</span>';
                }
            },
            {
                // Họ tên & Đơn vị & Khách hàng
                data: "FullName",
                render: function (data, type, row) {
                    var name = data ? htmlEncode(data) : "";
                    var initial = name.length > 0 ? name.trim().charAt(0).toUpperCase() : "C";
                    var avatarClasses = ["cp-avatar-blue", "cp-avatar-green", "cp-avatar-amber", "cp-avatar-purple", "cp-avatar-rose"];
                    var colorIndex = (name.charCodeAt(0) || 0) % avatarClasses.length;
                    var avatarClass = avatarClasses[colorIndex];

                    var html = '<div class="cp-user-info">';
                    html += '<div class="cp-avatar ' + avatarClass + '">' + initial + '</div>';
                    html += '<div class="cp-user-details">';
                    html += '<div class="cp-fullname">' + name + '</div>';

                    var workParts = [];
                    if (row.Position) workParts.push(htmlEncode(row.Position));
                    if (row.WorkUnit) workParts.push(htmlEncode(row.WorkUnit));
                    if (workParts.length > 0) {
                        html += '<div class="cp-work-info"><i class="fa fa-briefcase text-muted mr-1"></i>' + workParts.join(' - ') + '</div>';
                    }

                    // Khách hàng liên kết
                    var customerName = row.CustomerName;
                    if (customerName && typeof customerName === 'string' && customerName.trim().length > 0) {
                        var entries = customerName.trim().split(' | ');
                        var chipsHtml = '';
                        var maxShow = 2;
                        entries.forEach(function (item, index) {
                            item = item.trim();
                            if (!item) return;
                            var parts = item.split('::');
                            var company = htmlEncode((parts[0] || '').trim());
                            var position = htmlEncode((parts[1] || '').trim());
                            if (!company) return;
                            var isHidden = index >= maxShow ? ' style="display:none;" class="extra-item"' : '';
                            chipsHtml += '<span class="cp-customer-chip"' + isHidden + ' title="' + (position ? position + ' tại ' + company : company) + '">';
                            chipsHtml += '<i class="fa fa-building"></i> ' + company;
                            chipsHtml += '</span>';
                        });
                        if (chipsHtml) {
                            html += '<div class="mt-1 d-flex flex-wrap align-items-center">' + chipsHtml;
                            if (entries.length > maxShow) {
                                html += '<a href="javascript:void(0);" class="text-primary font-weight-bold ml-1 text-85" onclick="';
                                html += 'var p=this.parentNode;';
                                html += 'var items=p.getElementsByClassName(\'extra-item\');';
                                html += 'for(var i=0;i<items.length;i++){items[i].style.display=\'inline-flex\';}';
                                html += 'this.style.display=\'none\';';
                                html += '">';
                                html += '+' + (entries.length - maxShow) + ' nữa';
                                html += '</a>';
                            }
                            html += '</div>';
                        }
                    }

                    html += '</div></div>';
                    return html;
                }
            },
            {
                // Email
                data: "Email",
                render: function (data) {
                    if (!data) return '<span class="text-muted">—</span>';
                    return '<a href="mailto:' + htmlEncode(data) + '" class="cp-contact-link cp-email-link"><i class="fa fa-envelope mr-1"></i>' + htmlEncode(data) + '</a>';
                }
            },
            {
                // Địa chỉ
                data: "Address",
                render: function (data) {
                    if (!data) return '<span class="text-muted">—</span>';
                    return '<div class="text-secondary text-90"><i class="fa fa-map-marker-alt text-danger mr-1"></i>' + htmlEncode(data) + '</div>';
                }
            },
            {
                // Thông tin liên hệ (Phone, Mobile, Zalo)
                data: null,
                render: function (data, type, row) {
                    var html = '<div class="cp-contact-links">';
                    if (row.Phone) {
                        html += '<a href="tel:' + htmlEncode(row.Phone) + '" class="cp-contact-link cp-phone-link" title="Gọi điện thoại"><i class="fa fa-phone-alt mr-1"></i>' + htmlEncode(row.Phone) + '</a>';
                    }
                    if (row.Mobile) {
                        html += '<a href="tel:' + htmlEncode(row.Mobile) + '" class="cp-contact-link cp-phone-link" title="Gọi di động"><i class="fa fa-mobile-alt mr-1"></i>' + htmlEncode(row.Mobile) + '</a>';
                    }
                    if (row.Zalo) {
                        html += '<span class="cp-contact-link cp-zalo-link" title="Zalo"><i class="fa fa-comment mr-1"></i>' + htmlEncode(row.Zalo) + '</span>';
                    }
                    html += '</div>';
                    return (row.Phone || row.Mobile || row.Zalo) ? html : '<span class="text-muted">—</span>';
                }
            },
            {
                // Trạng thái
                data: "Status",
                className: "text-center",
                render: function (data) {
                    if (data == 1) {
                        return '<span class="cp-badge-status cp-status-active"><span class="cp-pulse-dot dot-active"></span>Đang hoạt động</span>';
                    } else {
                        return '<span class="cp-badge-status cp-status-inactive"><span class="cp-pulse-dot dot-inactive"></span>Ngừng hoạt động</span>';
                    }
                }
            },
            {
                // Thao tác trực tiếp
                data: "ContactPerson_ID",
                orderable: false,
                className: "text-center",
                render: function (data, type, row) {
                    var id = data || row.ContactPerson_ID;
                    var html = '<div class="cp-action-group justify-content-center">';
                    html += _renderButton(true,
                        "EditContactPersons",
                        "cp-btn-action cp-btn-edit",
                        "/Cate/ContactPersons/Edit/" + id,
                        '<i class="far fa-edit"></i>',
                        "Cập nhật", 900);
                    html += _renderButton(true,
                        "DeleteContactPersons",
                        "cp-btn-action cp-btn-delete",
                        "/Cate/ContactPersons/Delete/" + id,
                        '<i class="far fa-trash-alt"></i>',
                        "Xóa");
                    html += '</div>';
                    return html;
                }
            }
        ]
    });
}

function initExportContactPersons() {
    $(document).on("click", "#btnExportContactPersons", function () {
        var url = "/Cate/ContactPersons/Export";
        var params = {
            keyword: $("#cpKeyword").val(),
            customerID: getIntOrNull($("#cpCustomerID").val()),
            gender: getIntOrNull($("#cpGender").val()),
            status: getIntOrNull($("#cpStatus").val())
        };
        window.location = url + "?" + $.param(params);
    });
}

function ContactPersons_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                _tableContactPersons.ajax.reload(null, false);
                ContactPersons_LoadSummary();
                eval(response.message);
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
                        _tableContactPersons.ajax.reload(null, false);
                        ContactPersons_LoadSummary();
                        eval(response.message);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function initContactPersonsImport() {
    var msg = window._cpImportMsg || {};

    // Chọn file
    $(document).on("change", "#cpImportFileInput", function () {
        var label = this.files.length > 0 ? this.files[0].name : "Chọn file...";
        $(this).next(".custom-file-label").text(label);
    });

    // Đọc & kiểm tra file
    $(document).on("click", "#cpBtnReadFile", function () {
        var inputEl = document.getElementById("cpImportFileInput");
        if (!inputEl || !inputEl.files || inputEl.files.length === 0) {
            toastr.warning("Vui lòng chọn file trước khi tiếp tục.");
            return;
        }
        var fd = new FormData();
        fd.append("importFile", inputEl.files[0]);
        var $btn = $(this);
        $("#cpUploadProgress").removeClass("d-none");
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang xử lý...');
        $.ajax({
            url: msg.previewUrl || "/Cate/ContactPersons/ImportPreview",
            type: "POST",
            data: fd,
            processData: false,
            contentType: false,
            success: function (res) {
                $("#cpUploadProgress").addClass("d-none");
                $btn.prop("disabled", false).html('<i class="fa fa-search"></i> Đọc & Kiểm tra file');
                if (!res.status) { toastr.error(res.message); return; }
                cpRenderImportPreview(res, msg);
            },
            error: function () {
                $("#cpUploadProgress").addClass("d-none");
                $btn.prop("disabled", false).html('<i class="fa fa-search"></i> Đọc & Kiểm tra file');
                toastr.error("Lỗi đọc file. Vui lòng thử lại.");
            }
        });
    });

    // Quay lại bước upload
    $(document).on("click", "#cpBtnBack", function () {
        $("#cpStepPreview").addClass("d-none");
        $("#cpStepUpload").removeClass("d-none");
        $("#cpFooterPreview").addClass("d-none");
        $("#cpFooterUpload").removeClass("d-none");
        $("#cpImportFileInput").val("").next(".custom-file-label").text("Chọn file...");
        $("#cpBtnConfirm").prop("disabled", false).html('<i class="fa fa-check"></i> Xác nhận nhập');
        $("#cpBtnExportErrorRows").addClass("d-none");
    });

    // Xác nhận nhập
    $(document).on("click", "#cpBtnConfirm", function () {
        var $btn = $(this);
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang nhập dữ liệu...');
        $.ajax({
            url: msg.confirmUrl || "/Cate/ContactPersons/ImportConfirm",
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
                    toastr.success("Nhập thành công " + res.successCount + " người liên hệ!");
                    _tableContactPersons.ajax.reload(null, false);
                    ContactPersons_LoadSummary();
                }

                if (res.failCount > 0 && res.duplicateRows && res.duplicateRows.length > 0) {
                    var html =
                        '<div class="alert alert-warning py-2 mb-2">' +
                        '<i class="fa fa-exclamation-triangle"></i> ' +
                        '<strong>' + res.failCount + '</strong> dòng bị trùng hoặc thất bại:' +
                        '</div>' +
                        '<table class="table table-sm table-bordered table-hover small">' +
                        '<thead style="background:#fff3cd;">' +
                        '<tr><th>#</th><th>Họ và tên</th><th>Điện thoại</th><th>Lý do</th></tr>' +
                        '</thead><tbody>';

                    $.each(res.duplicateRows, function (i, r) {
                        html += '<tr>' +
                            '<td class="text-center">' + (i + 1) + '</td>' +
                            '<td>' + htmlEncode(r.FullName || r.fullName) + '</td>' +
                            '<td>' + htmlEncode(r.Phone || r.phone) + '</td>' +
                            '<td class="text-danger">' + htmlEncode(r.Reason || r.reason) + '</td>' +
                            '</tr>';
                    });

                    html += '</tbody></table>';
                    $("#cpImportResult").removeClass("d-none").html(html);
                    $("#cpFooterPreview").addClass("d-none");
                    $("#cpFooterDone").removeClass("d-none");

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

    // Xuất dòng lỗi validation
    $(document).on("click", "#cpBtnExportErrorRows", function () {
        var $btn = $(this);
        var cookieName = "cpExportErrorDone_" + Date.now();
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang xuất...');
        var $iframe = $("<iframe>").hide().appendTo("body");
        var exportUrl = (msg.exportErrorUrl || "/Cate/ContactPersons/ExportErrorRows")
            + "?cookieName=" + encodeURIComponent(cookieName);
        $iframe.attr("src", exportUrl);
        var checkTimer = setInterval(function () {
            if (document.cookie.indexOf(cookieName + "=done") !== -1) {
                clearInterval(checkTimer);
                document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                $iframe.remove();
                $btn.prop("disabled", false).html('<i class="fa fa-download"></i> Xuất dòng lỗi');
            }
        }, 500);
        setTimeout(function () {
            clearInterval(checkTimer);
            $iframe.remove();
            $btn.prop("disabled", false).html('<i class="fa fa-download"></i> Xuất dòng lỗi');
        }, 180000);
    });

    // Xuất dòng trùng
    $(document).on("click", "#cpBtnExportDuplicateRowsDone", function () {
        var $btn = $(this);
        var cookieName = "cpExportDupDone_" + Date.now();
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin"></i> Đang xuất...');
        var $iframe = $("<iframe>").hide().appendTo("body");
        $iframe.attr("src", "/Cate/ContactPersons/ExportDuplicateRows?cookieName=" + encodeURIComponent(cookieName));
        var checkTimer = setInterval(function () {
            if (document.cookie.indexOf(cookieName + "=done") !== -1) {
                clearInterval(checkTimer);
                document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                $iframe.remove();
                $btn.prop("disabled", false).html('<i class="fa fa-download"></i> Xuất dòng trùng');
            }
        }, 500);
        setTimeout(function () {
            clearInterval(checkTimer);
            $iframe.remove();
            $btn.prop("disabled", false).html('<i class="fa fa-download"></i> Xuất dòng trùng');
        }, 180000);
    });
}

function cpRenderImportPreview(res, msg) {
    msg = msg || {};
    var s = '<div class="col-md-6"><div class="alert alert-success mb-0 py-2"><i class="fa fa-check-circle"></i> <strong>' + res.totalValid + '</strong> dòng hợp lệ, sẵn sàng nhập</div></div>' +
        '<div class="col-md-6">' + (res.totalError > 0
            ? '<div class="alert alert-danger mb-0 py-2"><i class="fa fa-exclamation-circle"></i> <strong>' + res.totalError + '</strong> dòng có lỗi (sẽ bỏ qua)</div>'
            : '<div class="alert alert-success mb-0 py-2"><i class="fa fa-check"></i> Không có dòng lỗi</div>') + '</div>';
    $("#cpImportSummary").html(s);
    $("#cpCntValid").text(res.totalValid);
    $("#cpCntError").text(res.totalError);

    var vh = "";
    if (res.validRows && res.validRows.length > 0) {
        $.each(res.validRows, function (i, r) {
            vh += "<tr>"
                + "<td class='text-center'>" + parseInt(r.RowNumber || 0) + "</td>"
                + "<td>" + htmlEncode(r.FullName) + "</td>"
                + "<td>" + htmlEncode(r.GenderName) + "</td>"
                + "<td>" + htmlEncode(r.Position) + "</td>"
                + "<td>" + htmlEncode(r.Phone) + "</td>"
                + "<td>" + htmlEncode(r.Email) + "</td>"
                + "<td>" + htmlEncode(r.CustomerShortName) + "</td>"
                + "<td>" + htmlEncode(r.CustomerName) + "</td>"
                + "</tr>";
        });
    } else {
        vh = '<tr><td colspan="8" class="text-center text-muted py-3">Không có dữ liệu hợp lệ</td></tr>';
    }
    $("#cpTbodyValid").html(vh);

    var eh = "";
    if (res.errorRows && res.errorRows.length > 0) {
        $.each(res.errorRows, function (i, r) {
            var errList = r.Errors || r.errors || [];
            var errHtml = "";
            if (Array.isArray(errList) && errList.length > 0) {
                errHtml = '<ul class="mb-0 pl-3">';
                $.each(errList, function (j, e) { errHtml += "<li>" + htmlEncode(e) + "</li>"; });
                errHtml += "</ul>";
            }
            eh += '<tr class="table-danger">'
                + '<td class="text-center">' + parseInt(r.RowNumber || 0) + '</td>'
                + '<td>' + htmlEncode(r.FullName) + '</td>'
                + '<td class="text-danger small">' + errHtml + '</td>'
                + '</tr>';
        });
    } else {
        eh = '<tr><td colspan="3" class="text-center text-muted py-3">Không có dòng lỗi</td></tr>';
    }
    $("#cpTbodyError").html(eh);

    $("#cpStepUpload").addClass("d-none");
    $("#cpStepPreview").removeClass("d-none");
    $("#cpFooterUpload").addClass("d-none");
    $("#cpFooterPreview").removeClass("d-none");

    if (res.totalValid === 0) {
        $("#cpBtnConfirm").prop("disabled", true);
    }
    if (res.totalError > 0) {
        $("#cpBtnExportErrorRows").removeClass("d-none");
        if (res.totalValid === 0) {
            $("#cpImportTabs a[href='#cpTabError']").tab("show");
        }
    } else {
        $("#cpBtnExportErrorRows").addClass("d-none");
    }
}