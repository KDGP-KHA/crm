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

function formatRevenueInMillions(value) {
    if (value == null || isNaN(value)) return '0';
    var num = Number(value);
    if (num === 0) return '0';
    var inMillions = num / 1000000;
    return inMillions.toLocaleString('vi-VN', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
    });
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
                d.StatusIDs = typeof getSelectedStatusIDs === 'function' ? getSelectedStatusIDs() : '';
                d.CustomerID = $("#CustomerID").val() || 0;
                d.ProductServiceID = 0;
                d.EmployeeID = $("#EmployeeID").val() || $("#SearchEmployeeID").val() || "";
                d.DepartmentID = $("#DepartmentID").val() || $("#SearchDepartmentID").val() || "";
                d.FromDate = $("#FromDate").val() || $("#SearchFromDate").val() || "";
                d.ToDate = $("#ToDate").val() || $("#SearchToDate").val() || "";
                d.IsKeyProject = $("#chkFilterKeyProject").is(":checked");
                d.IsFollowed = $("#chkFilterFollowed").is(":checked");
                d.ApplyYear = $("#ApplyYear").val() || "";
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
                    //if (row.ContactPersonName) {
                    //    html += '<div class="sale-subtext text-secondary mt-1"><i class="fa fa-user-circle text-secondary mr-1"></i>' + row.ContactPersonName;
                    //    if (row.ContactPersonPhone) {
                    //        html += ' <span class="text-muted">(' + row.ContactPersonPhone + ')</span>';
                    //    }
                    //    html += '</div>';
                    //}
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
                    var expRev = formatRevenueInMillions(row.TotalExpectedRevenue);
                    var actRev = formatRevenueInMillions(row.TotalActualRevenue);
                    var rawExp = row.TotalExpectedRevenue != null ? Number(row.TotalExpectedRevenue).toLocaleString('vi-VN') + ' đ' : '0 đ';
                    var rawAct = row.TotalActualRevenue != null ? Number(row.TotalActualRevenue).toLocaleString('vi-VN') + ' đ' : '0 đ';

                    var html = '<div class="sale-revenue" title="Dự kiến: ' + rawExp + '">' +
                        '<span class="text-secondary">Dự kiến:</span> <span class="font-weight-bold text-primary">' + expRev + ' tr</span>' +
                        '</div>';
                    html += '<div class="sale-revenue mt-1" title="Thực tế: ' + rawAct + '">' +
                        '<span class="text-secondary">Thực tế:</span> <span class="font-weight-bold text-success">' + actRev + ' tr</span>' +
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
        },
        initComplete: function () {
            $('#tblDigitalSales_wrapper .dataTables_length select').addClass('none-select2');
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

function reloadSalesTable(resetPaging) {
    if (_tableDigitalSales) {
        _tableDigitalSales.ajax.reload(null, resetPaging === false ? false : true);
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
    $("#ApplyYear").val(new Date().getFullYear());

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

    if (typeof loadStatusesByBusinessType === 'function') {
        loadStatusesByBusinessType("");
    }

    // Khôi phục trạng thái checked mặc định cho combobox trạng thái đa chọn
    $('.status-item-row').show();
    var excluded = window._excludedStatusIds || [];
    $('.status-item').each(function () {
        var val = parseInt($(this).val());
        $(this).prop('checked', excluded.indexOf(val) < 0);
    });
    var totalStatus = $('.status-item').length;
    var checkedStatus = $('.status-item:checked').length;
    $('#checkAllStatus').prop('checked', totalStatus > 0 && totalStatus === checkedStatus);
    if (typeof updateStatusDropdownText === 'function') {
        updateStatusDropdownText();
    }

    reloadSalesTable();
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
    var applyYear = $("#SearchDigitalSales #ApplyYear").val() || $("#ApplyYear").val() || "";
    if (applyYear) qs.push("applyYear=" + encodeURIComponent(applyYear));

    var statusIDs = typeof getSelectedStatusIDs === 'function' ? getSelectedStatusIDs() : '';
    if (statusIDs) qs.push("statusIDs=" + encodeURIComponent(statusIDs));

    var url = baseUrl + (qs.length ? "?" + qs.join("&") : "");
    window.location.href = url;
}

// -------------------------------------------------------------
// STATUS MULTISELECT DROPDOWN LOGIC (GIỐNG DỰ ÁN)
// -------------------------------------------------------------
function getSelectedStatusIDs() {
    var $items = $('.status-item:visible');
    if ($items.length === 0) {
        $items = $('.status-item');
    }
    return $items.filter(':checked').map(function () {
        return $(this).val();
    }).get().join(',');
}

function updateStatusDropdownText() {
    var $items = $('.status-item:visible');
    if ($items.length === 0) {
        $items = $('.status-item');
    }
    var total = $items.length;
    var checked = $items.filter(':checked').length;

    if (checked === 0) {
        $('#statusDropdownText').text('Chưa chọn trạng thái');
        return;
    }

    if (checked === total) {
        $('#statusDropdownText').text('Tất cả');
        return;
    }

    $('#statusDropdownText').text(checked + ' trạng thái được chọn');
}

function filterStatusListByBusinessType(bType) {
    if (!bType) {
        $('.status-item-row').show();
    } else {
        $('.status-item-row').each(function () {
            var rowType = $(this).data('businesstype');
            if (rowType == bType) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }
    var $visible = $('.status-item:visible');
    var total = $visible.length;
    var checked = $visible.filter(':checked').length;
    $('#checkAllStatus').prop('checked', total > 0 && total === checked);
    updateStatusDropdownText();
    reloadSalesTable();
}

// Toggle status dropdown menu
$(document).on('click', '#statusDropdownButton', function (e) {
    e.stopPropagation();
    $('#statusDropdownMenu').toggle();
});

// Click ngoài -> đóng menu
$(document).on('click', function () {
    $('#statusDropdownMenu').hide();
});

// Ngăn đóng menu khi click bên trong
$(document).on('click', '#statusDropdownMenu', function (e) {
    e.stopPropagation();
});

// Check all
$(document).on('change', '#checkAllStatus', function () {
    var isChecked = $(this).is(':checked');
    var $items = $('.status-item:visible');
    if ($items.length === 0) {
        $items = $('.status-item');
    }
    $items.prop('checked', isChecked);
    updateStatusDropdownText();
    reloadSalesTable();
});

// Item change
$(document).on('change', '.status-item', function () {
    var $items = $('.status-item:visible');
    if ($items.length === 0) {
        $items = $('.status-item');
    }
    var total = $items.length;
    var checked = $items.filter(':checked').length;
    $('#checkAllStatus').prop('checked', total > 0 && total === checked);
    updateStatusDropdownText();
    reloadSalesTable();
});

// ==========================================
// HƯỚNG DẪN SỬ DỤNG & CẤU HÌNH HƯỚNG DẪN (INDEX)
// ==========================================
function executeResponseMessage(message) {
    if (!message) return;
    try {
        if (typeof message === "string" && (message.indexOf("toastr") !== -1 || message.indexOf("alert") !== -1 || message.indexOf("showNotify") !== -1)) {
            eval(message);
        } else if (typeof toastr !== "undefined") {
            toastr.info(message);
        } else if (typeof $.aceToaster !== "undefined") {
            $.aceToaster.add({ title: "Thông báo", body: message, placement: "tr", className: "bgc-info-d2 text-white" });
        }
    } catch (e) {
        if (typeof toastr !== "undefined") {
            toastr.info(message);
        }
    }
}

var DigitalSalesGuide = (function () {
    var steps = [];
    var current = 0;
    var activeScreenCode = "";
    var $overlay, $tooltip, $activeTarget, isOpen = false;

    function clearHighlight() {
        if ($activeTarget) $activeTarget.removeClass("ds-guide-highlight");
        $activeTarget = null;
    }
    function close() {
        clearHighlight();
        if ($overlay) $overlay.remove();
        if ($tooltip) $tooltip.remove();
        $overlay = $tooltip = null;
        isOpen = false;
        $(window).off("resize.dsGuide scroll.dsGuide");
    }
    function position() {
        if (!$tooltip || !$activeTarget || !$activeTarget.length) return;
        var r = $activeTarget[0].getBoundingClientRect();
        var tooltipWidth = Math.min(380, $(window).width() - 32);
        var top = r.bottom + 14;
        if (top + $tooltip.outerHeight() > $(window).height() - 12) top = Math.max(12, r.top - $tooltip.outerHeight() - 14);
        var left = Math.max(16, Math.min(r.left, $(window).width() - tooltipWidth - 16));
        $tooltip.css({ top: top + "px", left: left + "px", width: tooltipWidth + "px" });
    }
    function showStep(index) {
        current = index;
        clearHighlight();
        var step = steps[current];
        var $target = $(step.selector).filter(":visible").first();
        if (!$target.length) { if (current < steps.length - 1) return showStep(current + 1); close(); return; }
        $activeTarget = $target.addClass("ds-guide-highlight");
        var isLast = current === steps.length - 1;
        var canConfig = window._digitalSalesGuide && _digitalSalesGuide.canConfig;
        var configBtnHtml = canConfig ? '<button type="button" class="btn btn-xs btn-outline-secondary ds-guide-edit-btn" title="Cấu hình hướng dẫn"><i class="fa fa-cog mr-1"></i>Cấu hình</button>' : '';

        $tooltip.find(".ds-guide-body").html(
            '<div class="ds-guide-title">' + step.title + '</div>' +
            '<div class="ds-guide-text">' + step.text + '</div>' +
            '<div class="ds-guide-footer">' +
            '<span class="ds-guide-counter mr-2">' + (current + 1) + '/' + steps.length + '</span>' +
            configBtnHtml +
            '<div>' + (current > 0 ? '<button type="button" class="btn btn-sm btn-light ds-guide-prev">Quay lại</button>' : '') +
            '<button type="button" class="btn btn-sm btn-primary ml-1 ds-guide-next">' + (isLast ? 'Đã hiểu' : 'Tiếp theo') + '</button></div></div>');
        $target[0].scrollIntoView({ behavior: "smooth", block: "center" });
        window.setTimeout(position, 250);
    }
    function start(tourSteps, markViewed, screenCode) {
        if (!tourSteps || !tourSteps.length || isOpen) return;
        steps = tourSteps;
        activeScreenCode = screenCode || "";
        close();
        isOpen = true;
        $overlay = $('<div class="ds-guide-overlay" aria-hidden="true"></div>').appendTo("body");
        $tooltip = $('<section class="ds-guide-tooltip" role="dialog" aria-label="Hướng dẫn sử dụng"><button type="button" class="ds-guide-close" aria-label="Đóng hướng dẫn">&times;</button><div class="ds-guide-body"></div></section>').appendTo("body");
        $tooltip.on("click", ".ds-guide-close", close)
            .on("click", ".ds-guide-prev", function () { showStep(current - 1); })
            .on("click", ".ds-guide-next", function () { if (current >= steps.length - 1) close(); else showStep(current + 1); })
            .on("click", ".ds-guide-edit-btn", function () {
                var sc = activeScreenCode;
                close();
                if (typeof window.openGuideConfigModal === "function") {
                    window.openGuideConfigModal(sc);
                }
            });
        $(window).on("resize.dsGuide scroll.dsGuide", position);
        showStep(0);
    }
    return { start: start, close: close, isOpen: function () { return isOpen; } };
}());

function startDigitalSalesIndexGuide() {
    var defaultIndexSteps = [
        { selector: ".Search.card", title: "Bộ lọc tìm kiếm", text: "Tìm kiếm hồ sơ theo từ khóa, khách hàng, AM chủ trì, trạng thái, thời gian." },
        { selector: "#tblDigitalSales", title: "Danh sách hồ sơ số", text: "Bảng hiển thị các cơ hội kinh doanh và dự án số đã khởi tạo kèm giá trị doanh thu." },
        { selector: "button[onclick='openAddSalesModal();']", title: "Khởi tạo Cơ hội mới", text: "Bấm vào đây để tạo mới một hồ sơ cơ hội kinh doanh sản phẩm dịch vụ số." },
        { selector: "#PageAction .btn-outline-purple", title: "Cấu hình Quy trình", text: "Dành riêng cho Quản trị hệ thống (QTHT) thiết lập quy trình và các tiến trình chuẩn." }
    ];

    var configuredSteps = window._digitalSalesGuide && _digitalSalesGuide.steps && _digitalSalesGuide.steps["Cate.DigitalSales.Index"];
    var tourSteps = defaultIndexSteps;
    if (configuredSteps && Array.isArray(configuredSteps) && configuredSteps.length > 0) {
        var activeSteps = configuredSteps.filter(function (s) { return s.isActive !== false; });
        if (activeSteps.length > 0) {
            tourSteps = activeSteps.map(function (s) {
                return { selector: s.selector, title: s.title, text: s.text };
            });
        }
    }

    DigitalSalesGuide.start(tourSteps, false, "Cate.DigitalSales.Index");
}
window.startDigitalSalesIndexGuide = startDigitalSalesIndexGuide;

function openGuideConfigModal(screenCode) {
    if (!screenCode) screenCode = "Cate.DigitalSales.Index";
    var getConfigUrl = (window._digitalSalesGuide && _digitalSalesGuide.getConfigUrl) || "/Cate/DigitalSales/GetGuideConfigModal";
    var url = getConfigUrl + "?screenCode=" + encodeURIComponent(screenCode);
    $("#modalContainer").load(url, function () {
        var $modal = $("#modal_GuideConfig");
        initGuideConfigModalHandlers($modal);
        $modal.modal("show");
    });
}
window.openGuideConfigModal = openGuideConfigModal;

function initGuideConfigModalHandlers($modal) {
    function renumberTbody($tbody) {
        $tbody.find(".guide-step-row").each(function (idx) {
            var num = idx + 1;
            $(this).find(".guide-order-num").text(num);
            $(this).find(".guide-input-order").val(num);
        });
        var count = $tbody.find(".guide-step-row").length;
        var screenCode = $tbody.data("screencode");
        $('#guideConfigTabs a[data-screencode="' + screenCode + '"] .guide-badge-count').text(count);
    }

    // Di chuyển Lên
    $modal.off("click", ".guide-btn-up").on("click", ".guide-btn-up", function () {
        var $row = $(this).closest(".guide-step-row");
        var $prev = $row.prev(".guide-step-row");
        if ($prev.length) {
            $row.insertBefore($prev);
            renumberTbody($row.closest(".guide-steps-tbody"));
        }
    });

    // Di chuyển Xuống
    $modal.off("click", ".guide-btn-down").on("click", ".guide-btn-down", function () {
        var $row = $(this).closest(".guide-step-row");
        var $next = $row.next(".guide-step-row");
        if ($next.length) {
            $row.insertAfter($next);
            renumberTbody($row.closest(".guide-steps-tbody"));
        }
    });

    // Xóa bước
    $modal.off("click", ".guide-btn-delete").on("click", ".guide-btn-delete", function () {
        var $row = $(this).closest(".guide-step-row");
        var $tbody = $row.closest(".guide-steps-tbody");
        $row.remove();
        if ($tbody.find(".guide-step-row").length === 0) {
            $tbody.html('<tr class="guide-empty-row"><td colspan="6" class="text-center py-4 text-secondary-m2"><i class="fa fa-info-circle text-140 mb-1 opacity-50"></i><p class="mb-0 text-90">Chưa có bước hướng dẫn nào cho khu vực này.</p></td></tr>');
        }
        renumberTbody($tbody);
    });

    // Thêm bước mới
    $modal.off("click", ".guide-btn-add").on("click", ".guide-btn-add", function () {
        var screenCode = $(this).data("screencode");
        var $pane = $modal.find('.guide-pane[data-screencode="' + screenCode + '"]');
        var $tbody = $pane.find(".guide-steps-tbody");
        $tbody.find(".guide-empty-row").remove();
        var nextOrder = $tbody.find(".guide-step-row").length + 1;
        var randomId = "chkActive_" + screenCode.replace(/\./g, "_") + "_" + Date.now();

        var rowHtml = '<tr class="guide-step-row" data-step-id="0">' +
            '<td class="text-center"><div class="d-flex align-items-center justify-content-center">' +
            '<div class="d-flex flex-column mr-1">' +
            '<button type="button" class="btn btn-2xs btn-outline-lightgrey border-0 guide-btn-up p-0" title="Di chuyển lên"><i class="fa fa-chevron-up text-secondary text-70"></i></button>' +
            '<button type="button" class="btn btn-2xs btn-outline-lightgrey border-0 guide-btn-down p-0" title="Di chuyển xuống"><i class="fa fa-chevron-down text-secondary text-70"></i></button>' +
            '</div>' +
            '<span class="guide-order-num font-bolder text-primary-d1">' + nextOrder + '</span>' +
            '<input type="hidden" class="guide-input-order" value="' + nextOrder + '" />' +
            '</div></td>' +
            '<td><input type="text" class="form-control form-control-sm guide-input-selector" placeholder="Bộ chọn CSS..." /></td>' +
            '<td><input type="text" class="form-control form-control-sm guide-input-title font-bold text-primary-d2" placeholder="Tiêu đề bước..." /></td>' +
            '<td><textarea class="form-control form-control-sm guide-input-text" rows="2" placeholder="Nội dung hướng dẫn..."></textarea></td>' +
            '<td class="text-center"><div class="custom-control custom-checkbox custom-checkbox-primary">' +
            '<input type="checkbox" class="custom-control-input guide-input-active" id="' + randomId + '" checked />' +
            '<label class="custom-control-label" for="' + randomId + '"></label>' +
            '</div></td>' +
            '<td class="text-center"><button type="button" class="btn btn-xs btn-outline-danger btn-h-danger border-0 radius-1 guide-btn-delete" title="Xóa bước"><i class="fa fa-trash-alt"></i></button></td>' +
            '</tr>';
        $tbody.append(rowHtml);
        renumberTbody($tbody);
    });

    // Khôi phục mặc định
    $modal.off("click", ".guide-btn-reset").on("click", ".guide-btn-reset", function () {
        var screenCode = $(this).data("screencode");
        var resetConfigUrl = (window._digitalSalesGuide && _digitalSalesGuide.resetConfigUrl) || "/Cate/DigitalSales/ResetGuideConfig";
        $.post(resetConfigUrl, { screenCode: screenCode }, function (response) {
            if (response && response.status) {
                if (!_digitalSalesGuide.steps) _digitalSalesGuide.steps = {};
                _digitalSalesGuide.steps[screenCode] = response.steps || [];
                executeResponseMessage(response.message);

                var $activeTab = $modal.find("#guideConfigTabs .nav-link.active");
                var activeCode = $activeTab.data("screencode") || screenCode;
                openGuideConfigModal(activeCode);
            } else {
                executeResponseMessage(response ? response.message : null);
            }
        });
    });

    // Xem thử hướng dẫn
    $modal.off("click", ".guide-btn-preview").on("click", ".guide-btn-preview", function () {
        var screenCode = $(this).data("screencode");
        var $pane = $modal.find('.guide-pane[data-screencode="' + screenCode + '"]');
        var previewSteps = [];
        $pane.find(".guide-step-row").each(function () {
            var $row = $(this);
            var isActive = $row.find(".guide-input-active").is(":checked");
            if (isActive) {
                previewSteps.push({
                    selector: $row.find(".guide-input-selector").val(),
                    title: $row.find(".guide-input-title").val(),
                    text: $row.find(".guide-input-text").val()
                });
            }
        });
        if (previewSteps.length === 0) return;
        $modal.modal("hide");
        window.setTimeout(function () {
            DigitalSalesGuide.start(previewSteps, false, screenCode);
        }, 400);
    });

    // Lưu cấu hình
    $modal.off("click", "#btnSaveGuideConfig").on("click", "#btnSaveGuideConfig", function () {
        var $btn = $(this);
        $btn.prop("disabled", true).prepend('<i class="fa fa-spinner fa-spin mr-1"></i>');

        var $activeTab = $modal.find("#guideConfigTabs .nav-link.active");
        var screenCode = $activeTab.data("screencode");
        var $pane = $modal.find('.guide-pane[data-screencode="' + screenCode + '"]');

        var stepsToSave = [];
        $pane.find(".guide-step-row").each(function () {
            var $row = $(this);
            var order = parseInt($row.find(".guide-input-order").val(), 10) || 1;
            var selector = ($row.find(".guide-input-selector").val() || "").trim();
            var title = ($row.find(".guide-input-title").val() || "").trim();
            var text = ($row.find(".guide-input-text").val() || "").trim();
            var isActive = $row.find(".guide-input-active").is(":checked");

            if (selector || title || text) {
                stepsToSave.push({
                    StepOrder: order,
                    Selector: selector,
                    Title: title,
                    GuideText: text,
                    IsActive: isActive
                });
            }
        });

        var payload = {
            ScreenCode: screenCode,
            Steps: stepsToSave
        };

        var saveConfigUrl = (window._digitalSalesGuide && _digitalSalesGuide.saveConfigUrl) || "/Cate/DigitalSales/SaveGuideConfig";
        $.ajax({
            url: saveConfigUrl,
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(payload),
            dataType: "json",
            success: function (response) {
                $btn.prop("disabled", false).find(".fa-spinner").remove();
                if (response && response.status) {
                    if (!_digitalSalesGuide.steps) _digitalSalesGuide.steps = {};
                    _digitalSalesGuide.steps[screenCode] = response.steps || [];

                    $modal.modal("hide");
                    $modal.one("hidden.bs.modal", function () {
                        executeResponseMessage(response.message);
                    });
                } else {
                    executeResponseMessage(response ? response.message : null);
                }
            },
            error: function () {
                $btn.prop("disabled", false).find(".fa-spinner").remove();
                if (typeof toastr !== "undefined") {
                    toastr.error("Có lỗi xảy ra khi lưu cấu hình hướng dẫn.");
                }
            }
        });
    });
}

