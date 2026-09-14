var _tableDigitalSales;
var _digitalSalesUrls = {
    get: "/Cate/DigitalSales/Get",
    add: "/Cate/DigitalSales/Add",
    edit: "/Cate/DigitalSales/Edit",
    delete: "/Cate/DigitalSales/Delete",
    detail: "/Cate/DigitalSales/Detail",
    changeStatusModal: "/Cate/DigitalSales/ChangeStatusModal",
    changeStatus: "/Cate/DigitalSales/ChangeStatus",
    getContactPersons: "/Cate/DigitalSales/GetContactPersons",
    export: "/Cate/DigitalSales/Export"
};

$(document).ready(function () {
    initSearchDatepicker();
    initTableDigitalSales();

    $("#chkFilterKeyProject").on("change", function () {
        reloadSalesTable();
    });

    $("#chkFilterFollowed").on("change", function () {
        reloadSalesTable();
    });
});

function initSearchDatepicker() {
    if ($.fn.datepicker) {
        $('#dpFromDate').datepicker({
            format: 'dd/mm/yyyy',
            autoclose: true,
            todayHighlight: true,
            language: 'vi'
        });
        $('#dpToDate').datepicker({
            format: 'dd/mm/yyyy',
            autoclose: true,
            todayHighlight: true,
            language: 'vi'
        });
    }
}

function initTableDigitalSales() {
    _tableDigitalSales = $("#tblDigitalSales").DataTable({
        responsive: true,
        processing: true,
        serverSide: true,
        ordering: false,
        searching: false,
        pageLength: 20,
        dom: '<"dt-top d-flex justify-content-between align-items-center mb-2 px-3 pt-3"l>t<"dt-bottom d-flex justify-content-between align-items-center px-3 py-3"ip>',
        ajax: {
            url: _digitalSalesUrls.get,
            type: "POST",
            dataType: "JSON",
            data: function (d) {
                d.Keyword = ($("#Keyword").val() || $("#SearchKeyword").val() || "").trim();
                d.BusinessType = $("#BusinessType").val() || $("#SearchBusinessType").val() || "";
                d.StatusID = $("#StatusID").val() || $("#SearchStatusID").val() || "";
                d.CustomerID = $("#CustomerID").val() || 0;
                d.ProductServiceID = 0;
                d.EmployeeID = $("#EmployeeID").val() || $("#SearchEmployeeID").val() || "";
                d.DepartmentID = $("#DepartmentID").val() || $("#SearchDepartmentID").val() || "";
                d.FromDate = $("#FromDate").val() || $("#SearchFromDate").val() || "";
                d.ToDate = $("#ToDate").val() || $("#SearchToDate").val() || "";
                d.IsKeyProject = $("#chkFilterKeyProject").is(":checked");
                d.IsFollowed = $("#chkFilterFollowed").is(":checked");
            }
        },
        columns: [
            {
                data: null,
                className: "text-center align-middle",
                orderable: false,
                render: function (data, type, row, meta) {
                    return meta.row + meta.settings._iDisplayStart + 1;
                }
            },
            {
                data: null,
                className: "align-middle",
                render: function (data, type, row) {
                    var badgeType = row.BusinessType === 2
                        ? '<span class="badge badge-success px-2 py-1 mr-1 sale-badge"><i class="fa fa-project-diagram mr-1"></i>Dự án</span>'
                        : '<span class="badge badge-primary px-2 py-1 mr-1 sale-badge"><i class="fa fa-lightbulb mr-1"></i>Cơ hội</span>';

                    var badgeClass = "badge-secondary";
                    if (row.StatusID === 1) badgeClass = "badge-secondary";
                    else if (row.StatusID === 2) badgeClass = "badge-info";
                    else if (row.StatusID === 3 || row.StatusID === 6) badgeClass = "badge-danger";
                    else if (row.StatusID === 7) badgeClass = "badge-primary";
                    else if (row.StatusID === 8) badgeClass = "badge-success";
                    else badgeClass = "badge-warning text-dark";

                    var badgeStatus = '<span class="badge ' + badgeClass + ' px-2 py-1 sale-badge">' + (row.StatusName || '—') + '</span>';

                    var badgeSpecial = '';
                    if (row.IsKeyProject) {
                        badgeSpecial += '<span class="badge bgc-orange-l3 text-orange-d3 border-1 brc-orange-m2 mr-1 font-bold px-2 py-1 radius-1 shadow-sm sale-badge" title="Dự án trọng điểm"><i class="fa fa-star text-warning mr-1"></i>Trọng điểm</span>';
                    }
                    if (row.IsFollowed) {
                        badgeSpecial += '<span class="badge bgc-pink-l3 text-pink-d2 border-1 brc-pink-m3 mr-1 font-bold px-2 py-1 radius-1 shadow-sm sale-badge" title="Cơ hội/dự án bạn đang quan tâm"><i class="fa fa-bookmark text-danger mr-1"></i>Quan tâm</span>';
                    }

                    // Hàng 1: Loại hình & Trạng thái
                    var html = '<div class="mb-1 d-flex align-items-center flex-wrap">' +
                        badgeType + ' ' + badgeStatus +
                        '</div>';

                    // Hàng 2: Tên cơ hội / Dự án
                    html += '<a href="' + _digitalSalesUrls.detail + '/' + row.DigitalSalesID + '" class="font-weight-bold text-primary d-block sale-title" style="font-size: 15px;" title="Xem chi tiết 360 độ">' +
                        row.Title + '</a>';

                    // Hàng 3: Mã hồ sơ & Các huy hiệu đặc biệt (Trọng điểm, Quan tâm)
                    html += '<div class="mt-1 d-flex align-items-center flex-wrap">' +
                        '<span class="badge bgc-warning-l3 text-warning-d3 border-1 brc-warning-m2 mr-1 font-mono font-bold px-2 py-1 radius-1 shadow-sm sale-badge"><i class="fa fa-hashtag mr-1 opacity-75"></i>' + (row.Code || '—') + '</span>' +
                        badgeSpecial +
                        '</div>';

                    // Hàng 4: Sản phẩm / dịch vụ số đính kèm (nếu có)
                    if (row.ProductServiceNames) {
                        html += '<div class="sale-subtext text-secondary mt-1"><i class="fa fa-tags text-purple mr-1"></i>' + row.ProductServiceNames + '</div>';
                    }
                    return html;
                }
            },
            {
                data: null,
                className: "align-middle",
                render: function (data, type, row) {
                    var html = '';
                    if (row.CustomerName) {
                        html += '<div class="font-weight-bold text-dark-m1 sale-customer"><i class="fa fa-building text-primary-m1 mr-1"></i>' + row.CustomerName + '</div>';
                    } else {
                        html += '<div class="text-muted">—</div>';
                    }
                    if (row.ContactPersonName) {
                        html += '<div class="sale-subtext text-secondary mt-1"><i class="fa fa-user-circle text-secondary mr-1"></i>' + row.ContactPersonName;
                        if (row.ContactPersonPhone) {
                            html += ' <span class="text-muted">(' + row.ContactPersonPhone + ')</span>';
                        }
                        html += '</div>';
                    }
                    return html;
                }
            },
            {
                data: null,
                className: "align-middle",
                render: function (data, type, row) {
                    var html = '';
                    if (row.AssignedEmployeeName) {
                        html += '<div class="font-weight-bold text-dark sale-am"><i class="fa fa-user-tie text-success mr-1"></i>' + row.AssignedEmployeeName + '</div>';
                    } else {
                        html += '<div class="text-muted">—</div>';
                    }
                    if (row.DepartmentName) {
                        html += '<div class="sale-subtext text-muted mt-1"><i class="fa fa-sitemap mr-1"></i>' + row.DepartmentName + '</div>';
                    }
                    return html;
                }
            },
            {
                data: null,
                className: "text-right align-middle",
                render: function (data, type, row) {
                    var expRev = row.TotalExpectedRevenue != null ? Number(row.TotalExpectedRevenue).toLocaleString('vi-VN') : '0';
                    var actRev = row.TotalActualRevenue != null ? Number(row.TotalActualRevenue).toLocaleString('vi-VN') : '0';

                    var html = '<div class="sale-revenue">' +
                        '<span class="text-secondary">Dự kiến:</span> <span class="font-weight-bold text-primary">' + expRev + ' đ</span>' +
                        '</div>';
                    html += '<div class="sale-revenue mt-1">' +
                        '<span class="text-secondary">Thực tế:</span> <span class="font-weight-bold text-success">' + actRev + ' đ</span>' +
                        '</div>';
                    return html;
                }
            },
            {
                data: null,
                className: "text-center align-middle text-nowrap",
                orderable: false,
                render: function (data, type, row) {
                    var html = '<div class="action-buttons">';
                    var hasAction = false;
                    if (row.CanEdit) {
                        hasAction = true;
                        html += '<a href="javascript:void(0);" onclick="openEditSalesModal(' + row.DigitalSalesID + ');" class="btn btn-xs btn-outline-info btn-h-outline-info btn-a-outline-info radius-1 px-2 py-1 mr-1 btn-action" title="Chỉnh sửa">' +
                            '<i class="fa fa-edit mr-1"></i>Sửa</a>';
                    }
                    if (row.CanDelete) {
                        hasAction = true;
                        var safeCode = (row.Code || '').replace(/'/g, "\\'");
                        var safeTitle = (row.Title || '').replace(/'/g, "\\'");
                        html += '<a href="javascript:void(0);" onclick="confirmDeleteSales(' + row.DigitalSalesID + ', \'' + safeCode + '\', \'' + safeTitle + '\');" class="btn btn-xs btn-outline-danger btn-h-outline-danger btn-a-outline-danger radius-1 px-2 py-1 btn-action" title="Xóa">' +
                            '<i class="fa fa-trash-alt mr-1"></i>Xóa</a>';
                    }
                    if (!hasAction) {
                        html += '<span class="text-muted sale-subtext font-italic"><i class="fa fa-lock mr-1"></i>Chỉ xem</span>';
                    }
                    html += '</div>';
                    return html;
                }
            }
        ],
        language: {
            processing: "Đang tải dữ liệu...",
            emptyTable: "Không có dữ liệu phù hợp",
            info: "Hiển thị _START_ đến _END_ trong tổng số _TOTAL_ bản ghi",
            infoEmpty: "Không có bản ghi nào",
            infoFiltered: "(lọc từ _MAX_ bản ghi)",
            lengthMenu: "Hiển thị _MENU_ bản ghi",
            paginate: {
                first: "Đầu",
                previous: "Trước",
                next: "Sau",
                last: "Cuối"
            }
        }
    });
}

function executeResponseMessage(message, defaultText, isSuccess) {
    var msg = message || defaultText;
    if (!msg) return;

    if (typeof msg === "string") {
        var trimmed = msg.trim();
        // Nếu là đoạn mã JavaScript trả về từ server (showNotify, $.aceToaster, toastr, alert, v.v.)
        if (trimmed.indexOf("showNotify") !== -1 ||
            trimmed.indexOf("$.aceToaster") !== -1 ||
            trimmed.indexOf("toastr") !== -1 ||
            trimmed.indexOf("alert(") !== -1 ||
            trimmed.indexOf("eval(") !== -1) {
            try {
                eval(trimmed);
                return;
            } catch (e) {
                console.error("Execute message script error:", e);
            }
        }
    }

    // Nếu là chuỗi text thông báo thông thường:
    if (typeof showNotify === "function") {
        showNotify(
            isSuccess ? "Thành công" : "Cảnh báo",
            isSuccess ? "fa fa-check-circle" : "fa fa-exclamation-triangle",
            msg,
            "",
            "",
            isSuccess ? "success" : "danger",
            "tr"
        );
    } else if (typeof toastr !== "undefined") {
        if (isSuccess) {
            toastr.success(msg);
        } else {
            toastr.error(msg);
        }
    } else if (typeof $.aceToaster !== "undefined") {
        $.aceToaster.add({
            placement: 'tr',
            body: "<div class='p-3'>" + msg + "</div>",
            width: '420px',
            delay: 4000,
            className: isSuccess ? 'bgc-success-d2 text-white' : 'bgc-danger-d2 text-white'
        });
    } else {
        alert(msg);
    }
}

function reloadSalesTable() {
    if (_tableDigitalSales) {
        _tableDigitalSales.ajax.reload(null, false);
    }
}

function loadStatusesByBusinessType(businessType, selectedStatusId) {
    var $status = $('#StatusID, #SearchStatusID');
    var currentVal = selectedStatusId !== undefined ? selectedStatusId : $status.val();

    $status.empty().append('<option value="">-- Chọn trạng thái --</option>');

    $.get('/Cate/DigitalSales/GetStatusesByBusinessType', {
        businessType: businessType ? businessType : ''
    }, function (data) {
        if (data && data.length > 0) {
            var hasCurrentVal = false;
            $.each(data, function (i, item) {
                var isSelected = (currentVal && item.id == currentVal);
                if (isSelected) hasCurrentVal = true;
                $status.append(
                    $('<option>').val(item.id).text(item.name).prop('selected', isSelected)
                );
            });
            if (!hasCurrentVal) {
                $status.val('');
            }
        } else {
            $status.val('');
        }
        $status.trigger("chosen:updated");
        if ($.fn.select2) {
            $status.trigger("change.select2");
        }
        reloadSalesTable();
    });
}

function resetSalesSearch() {
    $("#Keyword, #SearchKeyword").val("");
    $("#BusinessType, #SearchBusinessType").val("");
    $("#BusinessType, #SearchBusinessType").trigger("chosen:updated");
    if ($.fn.select2) {
        $("#BusinessType, #SearchBusinessType").trigger("change.select2");
    }

    $("#DepartmentID, #SearchDepartmentID").val("");
    $("#DepartmentID, #SearchDepartmentID").trigger("chosen:updated");
    if ($.fn.select2) {
        $("#DepartmentID, #SearchDepartmentID").trigger("change.select2");
    }

    $("#FromDate, #SearchFromDate").val("");
    $("#ToDate, #SearchToDate").val("");
    if ($.fn.datepicker) {
        $('#dpFromDate').datepicker('update', '');
        $('#dpToDate').datepicker('update', '');
    }

    $("#chkFilterKeyProject").prop("checked", false);
    $("#chkFilterFollowed").prop("checked", false);

    var $employee = $("#EmployeeID, #SearchEmployeeID");
    $employee.empty().append('<option value="">-- Chọn nhân viên --</option>');
    $.get('/Cate/DigitalSales/GetEmployeesByDepartment', { departmentId: 0 }, function (data) {
        if (data && data.length > 0) {
            $.each(data, function (i, item) {
                $employee.append($('<option>').val(item.Value).text(item.Text));
            });
        }
        $employee.trigger("chosen:updated");
        if ($.fn.select2) {
            $employee.trigger("change.select2");
        }
    });

    loadStatusesByBusinessType("");
}

function DigitalSales_OnProcessSuccess(response, formId) {
    var $modal = $("#modal_" + formId);
    if ($modal.length === 0) {
        $modal = $("#modalContainer .modal.show");
    }
    if ($modal.length === 0) {
        $modal = $(".modal.show");
    }

    // 1. Phục hồi trạng thái nút Lưu và nút Lưu và di chuyển tới chi tiết
    var $btnSave = $modal.find("#btnSave, .modal-footer #btnSave, button[type='submit']");
    $btnSave.prop("disabled", false).html('<i class="fa fa-save mr-1"></i> Lưu');
    var $btnSaveAndDetail = $modal.find("#btnSaveAndDetail");
    $btnSaveAndDetail.prop("disabled", false).html('<i class="fa fa-external-link-alt mr-1"></i> Lưu và di chuyển tới chi tiết');

    if (response && response.status !== undefined) {
        // TRƯỜNG HỢP 1: JSON response
        if (response.status === true) {
            // Chỉ chuyển qua màn hình chi tiết nếu có yêu cầu điều hướng (Lưu và di chuyển tới chi tiết)
            if (response.redirectToDetail && response.id) {
                $modal.off("hidden.bs.modal hide.bs.modal");
                $btnSaveAndDetail.prop("disabled", true).removeClass("btn-primary").addClass("btn-success")
                    .html('<i class="fa fa-check mr-1"></i> Thành công! Đang chuyển đến chi tiết...');
                if (typeof _onWaiting === "function") _onWaiting();
                window.location.href = _digitalSalesUrls.detail + "/" + response.id;
                return;
            }

            // Đối với Sửa (Edit) hoặc thao tác không chuyển trang:
            executeResponseMessage(response.message, "Thao tác thành công!", true);

            // Đóng modal và dọn dẹp backdrop
            $modal.modal("hide");
            $(".modal-backdrop").remove();
            $("body").removeClass("modal-open").css("padding-right", "");

            if (typeof reloadSalesTable === "function") {
                reloadSalesTable();
            }
        } else {
            // Báo lỗi nghiệp vụ
            executeResponseMessage(response.message, "Thao tác thất bại!", false);
        }
    } else {
        // TRƯỜNG HỢP 2: HTML PartialView response do validation lỗi
        var $body = $modal.find("#bodyForm");
        if ($body.length === 0) {
            $body = $("#bodyForm");
        }
        $body.html(response);

        // Khởi tạo lại plugins (select2, datepicker, ckeditor...)
        if (typeof initDigitalSalesFormPlugins === "function") {
            initDigitalSalesFormPlugins();
        }

        // BẮT BUỘC BẬT TOASTR CẢNH BÁO CHO NGƯỜI DÙNG BIẾT
        var $firstError = $body.find(".text-danger:visible").first();
        var warnMsg = ($firstError.length && $firstError.text().trim())
            ? $firstError.text().trim()
            : "Vui lòng kiểm tra và nhập đầy đủ các trường bắt buộc (*)!";
        executeResponseMessage(warnMsg, warnMsg, false);

        // Cuộn hoặc focus vào ô lỗi đầu tiên
        if ($firstError.length > 0) {
            var $targetInput = $firstError.prev().find("input, select, textarea");
            if ($targetInput.length === 0) {
                $targetInput = $firstError.closest(".mb-3").find("input, select, textarea, button");
            }
            if ($targetInput.length > 0) {
                $targetInput.first().focus();
            }
        }

        // Re-bind lại sự kiện click cho nút Lưu
        $modal.find("#btnSave, .modal-footer #btnSave").off("click.digitalsales").on("click.digitalsales", function (e) {
            e.preventDefault();
            $("form#" + formId).submit();
        });
    }
}

function openAddSalesModal() {
    var idModal = "modal_AddDigitalSales";
    var $modal = $("#" + idModal);
    if ($modal.length === 0) {
        var htmlModal = '<div class="modal fade" id="' + idModal + '" data-backdrop="static" tabindex="-1" role="dialog" aria-hidden="true">' +
            '<div class="modal-dialog modal-xl" style="max-width: 1024px;" role="document">' +
            '<div id="modal-content" class="modal-content border-0 shadow-lg radius-2 overflow-hidden"></div>' +
            '</div></div>';
        $("#modalContainer").html(htmlModal);
        $modal = $("#" + idModal);
    }
    if (typeof _onWaiting === "function") _onWaiting();
    $modal.find("#modal-content").load(_digitalSalesUrls.add, function () {
        if (typeof _endWaiting === "function") _endWaiting();
        $modal.modal("show");
    });
}

function openEditSalesModal(id) {
    var idModal = "modal_EditDigitalSales";
    var $modal = $("#" + idModal);
    if ($modal.length === 0) {
        var htmlModal = '<div class="modal fade" id="' + idModal + '" data-backdrop="static" tabindex="-1" role="dialog" aria-hidden="true">' +
            '<div class="modal-dialog modal-xl" style="max-width: 1024px;" role="document">' +
            '<div id="modal-content" class="modal-content border-0 shadow-lg radius-2 overflow-hidden"></div>' +
            '</div></div>';
        $("#modalContainer").html(htmlModal);
        $modal = $("#" + idModal);
    }
    if (typeof _onWaiting === "function") _onWaiting();
    $modal.find("#modal-content").load(_digitalSalesUrls.edit + "/" + id, function () {
        if (typeof _endWaiting === "function") _endWaiting();
        $modal.modal("show");
    });
}

function openChangeStatusModal(id) {
    $.get(_digitalSalesUrls.changeStatusModal + "/" + id, function (html) {
        $("#modalContainer").html(html);
        var $modal = $("#modalChangeStatus");
        if ($.fn.select2) {
            $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
        }
        $modal.modal("show");

        var $form = $("#frmChangeStatus");
        if ($.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($form);
        }

        // Xóa lỗi validation khi người dùng chọn/nhập
        $form.find("#cboNewStatusID").on("change", function () {
            if ($(this).val() && $(this).val() !== "0") {
                var $valMsg = $form.find("[data-valmsg-for='NewStatusID']");
                $valMsg.empty().removeClass("field-validation-error").addClass("field-validation-valid");
                $(this).removeClass("input-validation-error border-danger");
            }
        });

        $form.find("#txtChangeStatusNote").on("input propertychange", function () {
            if (($(this).val() || "").trim()) {
                var $valMsg = $form.find("[data-valmsg-for='Note']");
                $valMsg.empty().removeClass("field-validation-error").addClass("field-validation-valid");
                $(this).removeClass("input-validation-error border-danger");
            }
        });

        $form.off("submit").on("submit", function (e) {
            e.preventDefault();

            // 1. Kiểm tra qua jQuery Unobtrusive Validation nếu có
            if (typeof $form.valid === "function" && !$form.valid()) {
                return false;
            }

            // 2. Fallback kiểm tra hiển thị lỗi trực tiếp vào ValidationMessageFor
            var hasError = false;
            var $statusInput = $form.find("select[name='NewStatusID']");
            var statusVal = $statusInput.val();
            if (!statusVal || statusVal === "0") {
                var $valMsgStatus = $form.find("[data-valmsg-for='NewStatusID']");
                $valMsgStatus.html('<span id="NewStatusID-error">Dữ liệu [Trạng thái mới] bắt buộc nhập</span>')
                             .removeClass("field-validation-valid")
                             .addClass("field-validation-error text-danger text-85 font-weight-bold d-block mt-1");
                $statusInput.addClass("input-validation-error border-danger");
                hasError = true;
            }

            var $noteInput = $form.find("textarea[name='Note']");
            var noteVal = ($noteInput.val() || "").trim();
            if (!noteVal) {
                var reqMsg = "Dữ liệu [Ghi chú / Lý do chuyển] bắt buộc nhập";
                if (typeof App_Message !== "undefined" && App_Message.DigitalSales_Msg_ChangeStatusNoteRequired) {
                    reqMsg = App_Message.DigitalSales_Msg_ChangeStatusNoteRequired;
                }
                var $valMsgNote = $form.find("[data-valmsg-for='Note']");
                $valMsgNote.html('<span id="Note-error">' + reqMsg + '</span>')
                           .removeClass("field-validation-valid")
                           .addClass("field-validation-error text-danger text-85 font-weight-bold d-block mt-1");
                $noteInput.addClass("input-validation-error border-danger");
                if (!hasError) {
                    $noteInput.focus();
                }
                hasError = true;
            }

            if (hasError) {
                return false;
            }

            // Thu thập dữ liệu tiến trình checklist thành chuỗi JSON trước khi gửi
            if (typeof serializeProgressItemsToJson === "function") {
                if (!serializeProgressItemsToJson()) {
                    return false;
                }
            }

            var $btnSubmit = $form.find("button[type='submit']");
            var origBtnHtml = $btnSubmit.html();
            $btnSubmit.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xử lý...');

            var formData = new FormData(this);
            $.ajax({
                url: _digitalSalesUrls.changeStatus,
                type: "POST",
                data: formData,
                contentType: false,
                processData: false,
                success: function (res) {
                    $btnSubmit.prop("disabled", false).html(origBtnHtml);
                    if (typeof res === "string") {
                        $modal.find("#bodyForm").html(res);
                        if (typeof initDigitalSalesChangeStatusForm === "function") initDigitalSalesChangeStatusForm();
                        return;
                    }
                    if (res.status) {
                        executeResponseMessage(res.message, "Chuyển trạng thái thành công!", true);
                        $modal.modal("hide");
                        $(".modal-backdrop").remove();
                        $("body").removeClass("modal-open").css("padding-right", "");
                        reloadSalesTable();
                    } else {
                        executeResponseMessage(res.message, "Không thể chuyển trạng thái!", false);
                    }
                },
                error: function () {
                    $btnSubmit.prop("disabled", false).html(origBtnHtml);
                    executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
                }
            });
        });
    });
}

function deleteSales(id, code, title) {
    confirmDeleteSales(id, code, title);
}

function confirmDeleteSales(id, code, title) {
    var $modal = $('#modalConfirmDeleteSales');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalConfirmDeleteSales" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1065;">' +
            '<div class="modal-dialog modal-dialog-centered" style="max-width: 480px;" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-danger text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-exclamation-triangle mr-1"></i> Xác nhận xóa hồ sơ</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-3 text-center">' +
            '<i class="fa fa-trash-alt fa-3x text-danger mb-3 d-block"></i>' +
            '<p class="text-dark mb-2 font-weight-bold text-105">Bạn có chắc chắn muốn xóa hồ sơ kinh doanh này không?</p>' +
            '<div class="bgc-grey-l4 radius-1 p-2 my-2 text-left border-1 brc-grey-l2" id="delSalesInfoBox">' +
            '<div class="font-bold text-primary-d2 text-95" id="delSalesTitleDisplay"></div>' +
            '<div class="text-85 text-secondary font-mono mt-1" id="delSalesCodeDisplay"></div>' +
            '</div>' +
            '<small class="text-muted text-85 d-block"><i class="fa fa-info-circle text-warning mr-1"></i>Thao tác này sẽ xóa hồ sơ và không thể hoàn tác.</small>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l5 d-flex justify-content-center">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Hủy bỏ</button>' +
            '<button type="button" id="btnConfirmDeleteSalesSubmit" class="btn btn-sm btn-danger radius-1 px-4 font-bold shadow-sm"><i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa</button>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalConfirmDeleteSales');
    }

    if (title || code) {
        $modal.find('#delSalesTitleDisplay').text(title || '').show();
        $modal.find('#delSalesCodeDisplay').text(code ? 'Mã: ' + code : '').show();
        $modal.find('#delSalesInfoBox').show();
    } else {
        $modal.find('#delSalesInfoBox').hide();
    }

    $modal.find('#btnConfirmDeleteSalesSubmit').off('click').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');
        $.ajax({
            url: _digitalSalesUrls.delete,
            type: 'POST',
            data: { id: id },
            success: function (res) {
                $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
                $modal.modal('hide');
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css('padding-right', '');
                if (res.status) {
                    executeResponseMessage(res.message, "Xóa hồ sơ thành công!", true);
                    reloadSalesTable();
                } else {
                    executeResponseMessage(res.message, "Không thể xóa hồ sơ!", false);
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
                executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
            }
        });
    });

    $modal.modal('show');
}

function loadContactPersonsByCustomer(customerId, targetSelector) {
    if (!customerId) {
        $(targetSelector).empty().append('<option value="">-- Chọn người liên hệ --</option>');
        return;
    }
    $.get(_digitalSalesUrls.getContactPersons, { customerId: customerId }, function (items) {
        var $target = $(targetSelector);
        $target.empty().append('<option value="">-- Chọn người liên hệ --</option>');
        if (items && items.length > 0) {
            $.each(items, function (idx, item) {
                $target.append($('<option>', { value: item.id, text: item.name }));
            });
        }
        if ($.fn.select2) {
            $target.trigger("change");
        }
    });
}

// Dọn dẹp modal con tra cứu khách hàng khi modal cha đóng
$(document).on('hidden.bs.modal', '#modal_AddDigitalSales, #modal_EditDigitalSales', function () {
    $('#modalCustomerLookup_Form').modal('hide');
    $('body > #modalCustomerLookup_Form').remove();
});

function exportDigitalSales() {
    var baseUrl = _digitalSalesUrls.export || "/Cate/DigitalSales/Export";
    var keyword = $("#SearchDigitalSales #Keyword").val() || $("#Keyword").val() || "";
    var businessType = $("#SearchDigitalSales #BusinessType").val() || $("#BusinessType").val() || "";
    var statusID = $("#SearchDigitalSales #StatusID").val() || $("#StatusID").val() || "";
    var departmentID = $("#SearchDigitalSales #DepartmentID").val() || $("#DepartmentID").val() || "";
    var employeeID = $("#SearchDigitalSales #EmployeeID").val() || $("#EmployeeID").val() || "";
    var fromDate = $("#SearchDigitalSales #FromDate").val() || $("#FromDate").val() || "";
    var toDate = $("#SearchDigitalSales #ToDate").val() || $("#ToDate").val() || "";
    var customerID = $("#SearchDigitalSales #CustomerID").val() || $("#CustomerID").val() || "";

    var qs = [];
    if (keyword) qs.push("keyword=" + encodeURIComponent(keyword));
    if (businessType) qs.push("businessType=" + encodeURIComponent(businessType));
    if (statusID) qs.push("statusID=" + encodeURIComponent(statusID));
    if (departmentID) qs.push("departmentID=" + encodeURIComponent(departmentID));
    if (employeeID) qs.push("employeeID=" + encodeURIComponent(employeeID));
    if (fromDate) qs.push("fromDate=" + encodeURIComponent(fromDate));
    if (toDate) qs.push("toDate=" + encodeURIComponent(toDate));
    if (customerID) qs.push("customerID=" + encodeURIComponent(customerID));
    if ($("#chkFilterKeyProject").is(":checked")) qs.push("isKeyProject=true");
    if ($("#chkFilterFollowed").is(":checked")) qs.push("isFollowed=true");

    var url = baseUrl + (qs.length ? "?" + qs.join("&") : "");
    window.location.href = url;
}

