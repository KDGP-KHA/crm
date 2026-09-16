window.CKEDITOR_BASEPATH = "/Contents/Modules/Major/ckeditor4/";
var _detailUrls = {
    editSales: "/Cate/DigitalSales/Edit",
    changeStatusModal: "/Cate/DigitalSales/ChangeStatusModal",
    changeStatus: "/Cate/DigitalSales/ChangeStatus",
    getContactPersons: "/Cate/DigitalSales/GetContactPersons",

    addProductModal: "/Cate/DigitalSales/AddProductModal",
    editProductModal: "/Cate/DigitalSales/EditProductModal",
    saveProduct: "/Cate/DigitalSales/SaveProduct",
    deleteProduct: "/Cate/DigitalSales/DeleteProduct",

    addMemberModal: "/Cate/DigitalSales/AddMemberModal",
    editMemberModal: "/Cate/DigitalSales/EditMemberModal",
    saveMember: "/Cate/DigitalSales/SaveMember",
    deleteMember: "/Cate/DigitalSales/DeleteMember",

    addTrackingModal: "/Cate/DigitalSales/AddTrackingModal",
    editTrackingModal: "/Cate/DigitalSales/EditTrackingModal",
    saveTracking: "/Cate/DigitalSales/SaveTracking",
    updateTrackingStatus: "/Cate/DigitalSales/UpdateTrackingStatus",
    deleteTracking: "/Cate/DigitalSales/DeleteTracking",
    changeProcessModal: "/Cate/DigitalSales/ChangeProcessModal",
    saveChangeProcess: "/Cate/DigitalSales/SaveChangeProcess",
    addTodoModal: "/Cate/DigitalSales/AddTodoModal",
    editTodoModal: "/Cate/DigitalSales/EditTodoModal",
    saveTodo: "/Cate/DigitalSales/SaveTodo",
    confirmTracking: "/Cate/DigitalSales/ConfirmTracking",
    unlockTracking: "/Cate/DigitalSales/UnlockTracking",
    trackingLogsModal: "/Cate/DigitalSales/GetTrackingLogsModal",
    unlockProgressModal: "/Cate/DigitalSales/UnlockProgressModal",
    submitUnlockProgress: "/Cate/DigitalSales/SubmitUnlockProgress",
    reportTrackingModal: "/Cate/DigitalSales/ReportTrackingModal",
    saveTrackingReport: "/Cate/DigitalSales/SaveTrackingReport",
    importProgressModal: "/Cate/DigitalSales/ImportProgressModal",
    previewImportProgress: "/Cate/DigitalSales/PreviewImportProgress",
    confirmImportProgress: "/Cate/DigitalSales/ConfirmImportProgress",
    importTodoModal: "/Cate/DigitalSales/ImportTodoModal",
    previewImportTodo: "/Cate/DigitalSales/PreviewImportTodo",
    confirmImportTodo: "/Cate/DigitalSales/ConfirmImportTodo",

    uploadAttachment: "/Cate/DigitalSales/UploadAttachment",
    deleteAttachment: "/Cate/DigitalSales/DeleteAttachment",
    toggleKeyProject: "/Cate/DigitalSales/ToggleKeyProject",
    toggleFollow: "/Cate/DigitalSales/ToggleFollow",
    statusTimelineModal: "/Cate/DigitalSales/StatusTimelineModal",
    statusTimelineDetailModal: "/Cate/DigitalSales/StatusTimelineDetailModal",

    getMetricsPartial: "/Cate/DigitalSales/GetMetricsPartial",
    getOverviewPartial: "/Cate/DigitalSales/GetOverviewPartial",
    getMembersPartial: "/Cate/DigitalSales/GetMembersPartial",
    getAttachmentsPartial: "/Cate/DigitalSales/GetAttachmentsPartial",
    getProductsPartial: "/Cate/DigitalSales/GetProductsPartial",
    getTrackingPartial: "/Cate/DigitalSales/GetTrackingPartial",
    getTimelinePartial: "/Cate/DigitalSales/GetTimelinePartial",
    getDiscussionsPartial: "/Cate/DigitalSales/GetDiscussionsPartial",
    postDiscussion: "/Cate/DigitalSales/PostDiscussion",
    deleteDiscussion: "/Cate/DigitalSales/DeleteDiscussion",
    getMembersForMention: "/Cate/DigitalSales/GetMembersForMention"
};

function reloadDigitalSalesReviewHistory() {
    $("#reviewHistoryContainer").load(_urlReloadReviewHistory + "?id=" + _currentDigitalSalesId, function () {
        var count = $("#reviewHistoryContainer .review-history-entry").length;
        $("#badgeTabReviewHistory").text(count);
    });
}

function ReviewHistory_OnProcessSuccess(response, formId) {
    var $modal = $("#ModalContent #modal_" + formId);
    if (response.status === undefined) {
        $modal.find("#bodyForm").html(response);
        var $form = $modal.find("form");
        if ($.validator && $.validator.unobtrusive) {
            $form.removeData("validator").removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse($form);
        }
        return;
    }

    if (response.status !== true) {
        executeResponseMessage(response.message, null, false);
        response.status = undefined;
        return;
    }

    $modal.one("hidden.bs.modal", function () {
        executeResponseMessage(response.message, null, true);
        reloadDigitalSalesReviewHistory();
        response.status = undefined;
    });
    $modal.modal("hide");
}

window.ReviewHistory_OnProcessSuccess = ReviewHistory_OnProcessSuccess;

function executeResponseMessage(message, defaultText, isSuccess) {
    if (!message && defaultText) {
        message = defaultText;
    }
    if (message && typeof message === "string") {
        if (message.indexOf("$.aceToaster") !== -1 || message.indexOf("toastr") !== -1 || message.indexOf("eval") !== -1) {
            try {
                eval(message);
                return;
            } catch (e) {
                console.error("Execute message script error:", e);
            }
        }
    }
    if (typeof $.aceToaster !== "undefined") {
        $.aceToaster.add({
            placement: 'tr',
            body: "<div class='p-3'>" + (message || defaultText) + "</div>",
            width: '420px',
            delay: 4000,
            className: isSuccess ? 'bgc-success-d2 text-white' : 'bgc-danger-d2 text-white'
        });
    } else if (typeof toastr !== "undefined") {
        if (isSuccess) {
            toastr.success(message || defaultText);
        } else {
            toastr.error(message || defaultText);
        }
    } else {
        alert(message || defaultText);
    }
}

/* ================= Helper: Loading Overlay & Micro Reloading ================= */
function showSectionLoading($container) {
    if (!$container || $container.length === 0) return;
    $container.addClass("position-relative");
    var $overlay = $container.children(".ds-section-loading-overlay");
    if ($overlay.length === 0) {
        $overlay = $(
            '<div class="ds-section-loading-overlay">' +
            '  <div class="spinner-border text-primary" role="status" style="width: 2.2rem; height: 2.2rem;">' +
            '    <span class="sr-only">Đang tải...</span>' +
            '  </div>' +
            '  <div class="mt-2 text-primary font-weight-bold text-90 shadow-sm px-2 py-1 bg-white radius-1 border-1 brc-grey-l2">' +
            '    <i class="fa fa-sync-alt fa-spin mr-1"></i> Đang cập nhật dữ liệu...' +
            '  </div>' +
            '</div>'
        );
        $container.append($overlay);
    }
    $overlay.stop(true, true).fadeIn(150);
}

function hideSectionLoading($container) {
    if (!$container || $container.length === 0) return;
    $container.children(".ds-section-loading-overlay").stop(true, true).fadeOut(200, function () {
        $(this).remove();
    });
}

function getEffectiveSalesId(salesId) {
    if (salesId) return salesId;
    if (typeof _currentDigitalSalesId !== "undefined" && _currentDigitalSalesId > 0) return _currentDigitalSalesId;
    var match = window.location.pathname.match(/\/Detail\/(\d+)/i);
    return match ? parseInt(match[1]) : 0;
}

function reloadMetricsSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $metrics = $("#containerMetrics");
    showSectionLoading($metrics);
    $.get(_detailUrls.getMetricsPartial, { id: salesId }, function (html) {
        $metrics.html(html);
        hideSectionLoading($metrics);
    }).fail(function () {
        hideSectionLoading($metrics);
    });
}

function reloadProductsSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $products = $("#tab-products");
    showSectionLoading($products);
    reloadMetricsSection(salesId);

    $.get(_detailUrls.getProductsPartial, { id: salesId }, function (html) {
        $products.html(html);
        hideSectionLoading($products);
        var newCount = $products.find("#partialProductsCount").data("count");
        if (newCount !== undefined) {
            $("#badgeTabProducts").text(newCount);
        }
    }).fail(function () {
        hideSectionLoading($products);
    });
}

function DigitalSalesContract_OnProcessSuccess(response, formId) {
    var $modal = $("#ModalContent #modal_" + formId);
    if ($modal.length === 0) {
        $modal = $("#modal_" + formId);
    }
    if ($modal.length === 0) {
        $modal = $(".modal.show").last();
    }
    if (response.status === undefined) {
        $modal.find("#bodyForm").html(response);
        return;
    }

    var complete = function () {
        if (response.message) eval(response.message);
        reloadProductsSection();
        response.status = undefined;
    };

    if ($modal.length > 0) {
        $modal.off("hidden.bs.modal.digitalSalesContract")
            .one("hidden.bs.modal.digitalSalesContract", complete)
            .modal("hide");
    } else {
        complete();
    }
}

function reloadMembersSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $members = $("#sectionMembers");
    if ($members.length === 0) {
        $members = $("#tab-overview");
    }
    showSectionLoading($members);

    $.get(_detailUrls.getMembersPartial, { id: salesId }, function (html) {
        $members.html(html);
        hideSectionLoading($members);
        var newCount = $members.find("#partialMembersCount").data("count");
        if (newCount !== undefined) {
            $("#badgeMemberCount").text(newCount);
        }
    }).fail(function () {
        hideSectionLoading($members);
    });
}

function reloadAttachmentsSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $attachments = $("#sectionAttachments");
    if ($attachments.length === 0) {
        $attachments = $("#tab-overview");
    }
    showSectionLoading($attachments);

    $.get(_detailUrls.getAttachmentsPartial, { id: salesId }, function (html) {
        $attachments.html(html);
        hideSectionLoading($attachments);
    }).fail(function () {
        hideSectionLoading($attachments);
    });
}

function reloadTrackingSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $tracking = $("#tab-tracking");
    showSectionLoading($tracking);
    reloadMetricsSection(salesId);

    $.get(_detailUrls.getTrackingPartial, { id: salesId }, function (html) {
        $tracking.html(html);
        hideSectionLoading($tracking);
        var $prog = $tracking.find("#partialTrackingProgress");
        if ($prog.length) {
            $("#badgeTabTracking").text($prog.data("completed") + "/" + $prog.data("total"));
        }
        reloadDiscussionsSection(salesId);
    }).fail(function () {
        hideSectionLoading($tracking);
    });
}

function updateHeaderFromInfo($info) {
    if (!$info || !$info.length) return;
    var title = $info.data("title");
    var code = $info.data("code");
    var statusName = $info.data("status");
    var businessType = $info.data("business-type");
    updateHeaderInfo(businessType, statusName, title, code);
}

function updateHeaderInfo(businessType, statusName, title, code) {
    if (businessType !== undefined && businessType !== null && businessType !== "") {
        var bType = parseInt(businessType, 10);
        var isProject = bType === 2;
        var $bTypeBadge = $("#headerBusinessType");
        if ($bTypeBadge.length) {
            if (isProject) {
                $bTypeBadge
                    .removeClass("bgc-blue-l2 text-blue-d2 brc-blue-m3")
                    .addClass("bgc-purple-l2 text-purple-d2 border-1 brc-purple-m3")
                    .html('<i class="fa fa-project-diagram mr-1"></i>Dự án');
            } else {
                $bTypeBadge
                    .removeClass("bgc-purple-l2 text-purple-d2 brc-purple-m3")
                    .addClass("bgc-blue-l2 text-blue-d2 border-1 brc-blue-m3")
                    .html('<i class="fa fa-lightbulb mr-1"></i>Cơ hội kinh doanh');
            }
        }
        $("#lblKeyProject").text("Trọng điểm");
        $("#lblFollowSales").text("Đang quan tâm");
    }

    if (statusName) {
        $("#headerStatusName").html('<i class="fa fa-check-circle mr-1"></i>' + statusName);
    }
    if (title) {
        $("#headerTitle").text(title).attr("title", title);
    }
    if (code) {
        $("#headerCode").html('<i class="fa fa-hashtag mr-1 opacity-75"></i>' + code);
    }
}

function reloadStatusAndTimelineSection(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $timeline = $("#tab-timeline");
    var $overview = $("#tab-overview");
    showSectionLoading($timeline);
    showSectionLoading($overview);
    reloadMetricsSection(salesId);
    reloadDiscussionsSection(salesId);

    $.get(_detailUrls.getTimelinePartial, { id: salesId }, function (html) {
        $timeline.html(html);
        hideSectionLoading($timeline);
        var newCount = $timeline.find("#partialTimelineCount").data("count");
        if (newCount !== undefined) {
            $("#badgeTabTimeline").text(newCount);
        }
    }).fail(function () {
        hideSectionLoading($timeline);
    });

    $.get(_detailUrls.getOverviewPartial, { id: salesId }, function (html) {
        $overview.html(html);
        hideSectionLoading($overview);
        var $info = $overview.find("#partialOverviewHeaderInfo");
        if ($info.length) {
            updateHeaderFromInfo($info);
        }
    }).fail(function () {
        hideSectionLoading($overview);
    });
}

function reloadOverviewAndMetrics(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $overview = $("#tab-overview");
    showSectionLoading($overview);
    reloadMetricsSection(salesId);

    $.get(_detailUrls.getOverviewPartial, { id: salesId }, function (html) {
        $overview.html(html);
        hideSectionLoading($overview);
        var $info = $overview.find("#partialOverviewHeaderInfo");
        if ($info.length) {
            updateHeaderFromInfo($info);
        }
    }).fail(function () {
        hideSectionLoading($overview);
    });
}

function refreshAllSections(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    if (typeof toastr !== "undefined") {
        toastr.info("Đang làm mới dữ liệu các phân vùng...");
    }
    reloadOverviewAndMetrics(salesId);
    reloadProductsSection(salesId);
    reloadMembersSection(salesId);
    reloadAttachmentsSection(salesId);
    reloadTrackingSection(salesId);
    reloadStatusAndTimelineSection(salesId);
    reloadDiscussionsSection(salesId);
}

$(document).ready(function () {
    // Keep active tab on reload if anchor hash exists
    var hash = window.location.hash;
    if (hash) {
        $('.nav-tabs a[href="' + hash + '"]').tab('show');
    }
    $('.nav-tabs a').on('shown.bs.tab', function (e) {
        window.location.hash = e.target.hash;
        if (e.target.hash === '#tab-discussions') {
            initDiscussionCKEditor();
        }
    });

    initDiscussionCKEditor();
    initDiscussionEvents();
});

/* ================= 1. Chỉnh sửa thông tin chung ================= */
function openEditSalesModal(id) {
    window.CKEDITOR_BASEPATH = "/Contents/Modules/Major/ckeditor4/";
    var idModal = "modal_EditDigitalSales";
    var htmlModal = '<div class="modal fade" id="' + idModal + '" data-backdrop="static" tabindex="-1" role="dialog" aria-hidden="true">' +
        '<div class="modal-dialog modal-xl" style="max-width: 1024px;" role="document">' +
        '<div id="modal-content" class="modal-content border-0 shadow-lg radius-2 overflow-hidden"></div>' +
        '</div></div>';
    $("#modalContainer").html(htmlModal);
    var $modal = $("#" + idModal);

    function doLoadEdit() {
        if (typeof _onWaiting === "function") _onWaiting();
        $modal.find("#modal-content").load(_detailUrls.editSales + "/" + id, function () {
            if (typeof _endWaiting === "function") _endWaiting();
            $modal.modal("show");
            $modal.off("shown.bs.modal.plugins").on("shown.bs.modal.plugins", function () {
                if (typeof initDigitalSalesFormPlugins === "function") {
                    initDigitalSalesFormPlugins();
                }
            });
        });
    }

    if (typeof CKEDITOR === "undefined") {
        if (typeof _onWaiting === "function") _onWaiting();
        $.getScript("/Contents/Modules/Major/ckeditor4/ckeditor.js", function () {
            if (typeof _endWaiting === "function") _endWaiting();
            doLoadEdit();
        });
    } else {
        doLoadEdit();
    }
}

function DigitalSales_OnProcessSuccess(response, formId) {
    var $modal = $("#modal_" + formId);
    if ($modal.length === 0) {
        $modal = $("#modalContainer .modal.show");
    }
    if ($modal.length === 0) {
        $modal = $(".modal.show");
    }

    var $btnSave = $modal.find("#btnSave, .modal-footer #btnSave, button[type='submit']");
    $btnSave.prop("disabled", false).html('<i class="fa fa-save"></i> Lưu');

    if (response && response.status !== undefined) {
        if (response.status === true) {
            executeResponseMessage(response.message, "Cập nhật thành công!", true);
            $modal.modal("hide");
            $(".modal-backdrop").remove();
            $("body").removeClass("modal-open").css("padding-right", "");
            reloadOverviewAndMetrics(getEffectiveSalesId());
        } else {
            executeResponseMessage(response.message, "Thao tác thất bại!", false);
        }
    } else {
        var $body = $modal.find("#bodyForm");
        if ($body.length === 0) {
            $body = $("#bodyForm");
        }
        $body.html(response);
        if (typeof initDigitalSalesFormPlugins === "function") {
            initDigitalSalesFormPlugins();
        }
        var $firstError = $body.find(".text-danger:visible").first();
        var warnMsg = ($firstError.length && $firstError.text().trim())
            ? $firstError.text().trim()
            : "Vui lòng kiểm tra và nhập đầy đủ các trường bắt buộc (*)!";
        executeResponseMessage(warnMsg, warnMsg, false);
    }
}

/* ================= 2. Chuyển đổi trạng thái (Gatekeeper) ================= */
function openChangeStatusModal(id) {
    $.get(_detailUrls.changeStatusModal + "/" + id, function (html) {
        $("#modalContainer").html(html);
        var $modal = $("#modalChangeStatus");
        var $form = $("#frmChangeStatus");

        if ($.fn.select2) {
            $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
        }

        // Kích hoạt jQuery Unobtrusive Validation cho form nạp động
        if ($.validator && $.validator.unobtrusive) {
            $form.removeData("validator");
            $form.removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse($form);
        }

        $modal.modal("show");

        // Tự động xóa thông báo lỗi khi người dùng tương tác
        $form.find("textarea[name='Note']").on("input propertychange", function () {
            var val = $(this).val().trim();
            if (val) {
                $(this).removeClass("input-validation-error border-danger");
                $form.find("[data-valmsg-for='Note']").empty().removeClass("field-validation-error").addClass("field-validation-valid");
            }
        });

        $form.find("select[name='NewStatusID']").on("change", function () {
            var val = $(this).val();
            if (val && val !== "0") {
                $(this).removeClass("input-validation-error border-danger");
                $form.find("[data-valmsg-for='NewStatusID']").empty().removeClass("field-validation-error").addClass("field-validation-valid");
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
                url: _detailUrls.changeStatus,
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
                        $modal.modal("hide");
                        executeResponseMessage(res.message, "Chuyển trạng thái thành công!", true);
                        if (res.businessType !== undefined) {
                            updateHeaderInfo(res.businessType, res.statusName);
                        }
                        reloadStatusAndTimelineSection(id);
                        reloadOverviewAndMetrics(id);
                        reloadTrackingSection(id);
                        reloadDiscussionsSection(id);
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

/* ================= 3. Sản phẩm / Dịch vụ số (Tab 2) ================= */
function parseDigitalSalesProductNumber(value) {
    var normalized = String(value || "").trim().replace(/\s/g, "");
    if (normalized.indexOf(",") >= 0) {
        normalized = normalized.replace(/\./g, "").replace(",", ".");
    }
    var parsed = parseFloat(normalized);
    return isNaN(parsed) ? 0 : parsed;
}

function formatDigitalSalesProductNumber(value) {
    return Number(value || 0).toLocaleString("vi-VN", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
}

function updateDigitalSalesProductEmptyStates($form) {
    ["cost", "revenue"].forEach(function (type) {
        var hasRows = $form.find('[data-detail-row="' + type + '"]').length > 0;
        $form.find('[data-empty-state="' + type + '"]').toggleClass("d-none", hasRows);
    });
}

function updateDigitalSalesProductFinanceSummary($form) {
    var expected = parseDigitalSalesProductNumber($form.find(".js-expected-revenue").val());
    var revenue = 0;
    var cost = 0;
    $form.find(".js-revenue-amount").each(function () { revenue += parseDigitalSalesProductNumber($(this).val()); });
    $form.find(".js-cost-amount").each(function () { cost += parseDigitalSalesProductNumber($(this).val()); });
    $form.find('[data-finance-summary="expected"]').text(formatDigitalSalesProductNumber(expected));
    $form.find('[data-finance-summary="revenue"]').text(formatDigitalSalesProductNumber(revenue));
    $form.find('[data-finance-summary="cost"]').text(formatDigitalSalesProductNumber(cost));
    $form.find('[data-finance-summary="profit"]').text(formatDigitalSalesProductNumber(revenue - cost));
}

function initDigitalSalesProductControls($scope, $modal) {
    if ($.fn.select2) {
        $scope.find(".product-service-select, .product-detail-select").filter(function () {
            return !$(this).closest("#productDetailTemplates").length;
        }).each(function () {
            var $select = $(this);
            if ($select.data("select2")) $select.select2("destroy");
            $select.select2({ width: "100%", dropdownParent: $modal });
            if ($select.hasClass("product-detail-select-sm")) {
                $select.next(".select2-container").addClass("product-detail-select-container-sm");
            }
        });
    }

    if ($.fn.datepicker) {
        $scope.find(".product-date-input").filter(function () {
            return !$(this).closest("#productDetailTemplates").length;
        }).each(function () {
            var $input = $(this);
            $input.off("click.digitalSalesProductDate");
            try { $input.datepicker("destroy"); } catch (ignore) { }
            $input.datepicker({
                autoclose: true,
                format: "dd/mm/yyyy",
                todayHighlight: true,
                todayBtn: true,
                weekStart: 1,
                language: "vi",
                orientation: "auto"
            });
            $input.on("click.digitalSalesProductDate", function () { $(this).datepicker("show"); });
        });
    }
}

function scrollDigitalSalesProductRowIntoView($row) {
    var row = $row.get(0);
    if (!row) return;
    window.setTimeout(function () {
        try {
            row.scrollIntoView({ behavior: "smooth", block: "nearest" });
        } catch (error) {
            row.scrollIntoView(false);
        }
    }, 0);
}

window.initDigitalSalesProductRuntimeForm = function () {
    var $modal = $("#modalProduct");
    var $form = $("#frmProductModal");
    if (!$form.length) return;

    $form.find("#productDetailTemplates :input").prop("disabled", true);
    initDigitalSalesProductControls($form, $modal);
    if ($.validator && $.validator.unobtrusive) {
        $form.removeData("validator").removeData("unobtrusiveValidation");
        $.validator.unobtrusive.parse($form);
    }

    $form.off("click.productRows", "[data-add-row]").on("click.productRows", "[data-add-row]", function () {
        var type = $(this).data("add-row");
        var token = type + Date.now().toString() + Math.floor(Math.random() * 1000).toString();
        var template = $form.find('#productDetailTemplates [data-template-row="' + type + '"]').prop("outerHTML");
        if (!template) return;
        template = template.replace(/__index__/g, token).replace('data-template-row="' + type + '"', 'data-detail-row="' + type + '"');
        var target = type === "cost" ? "#productCostRows" : "#productRevenueRows";
        var $row = $(template).appendTo($form.find(target));
        $row.find(":input").prop("disabled", false);
        initDigitalSalesProductControls($row, $modal);
        updateDigitalSalesProductEmptyStates($form);
        updateDigitalSalesProductFinanceSummary($form);
        scrollDigitalSalesProductRowIntoView($row);
    });

    $form.off("click.removeProductRow", "[data-remove-row]").on("click.removeProductRow", "[data-remove-row]", function () {
        var $row = $(this).closest("[data-detail-row]");
        $row.find(".product-detail-select").each(function () { if ($(this).data("select2")) $(this).select2("destroy"); });
        $row.remove();
        updateDigitalSalesProductEmptyStates($form);
        updateDigitalSalesProductFinanceSummary($form);
    });

    $form.off("input.productFinance change.productFinance", ".js-expected-revenue, .js-cost-amount, .js-revenue-amount")
        .on("input.productFinance change.productFinance", ".js-expected-revenue, .js-cost-amount, .js-revenue-amount", function () {
            updateDigitalSalesProductFinanceSummary($form);
        });

    $form.off("submit.digitalSales").off("submit.digitalSalesProductRuntime").on("submit.digitalSalesProductRuntime", function (event) {
        event.preventDefault();
        if ($form.valid && !$form.valid()) return false;

        var salesId = Number($form.find('[name="DigitalSalesID"]').val()) || _currentDigitalSalesId;
        var $button = $form.find('button[type="submit"]');
        var originalButtonHtml = $button.data("original-html") || $button.html();
        $button.data("original-html", originalButtonHtml).prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i>Đang lưu...');

        $.post($form.attr("action"), $form.serialize()).done(function (response) {
            $button.prop("disabled", false).html(originalButtonHtml);
            if (typeof response === "string") {
                $("#modalProduct #bodyForm").html(response);
                window.initDigitalSalesProductRuntimeForm();
                var validationMessage = $("#modalProduct .text-danger:visible").first().text();
                if (validationMessage) executeResponseMessage(validationMessage, validationMessage, false);
                return;
            }

            if (response.status) {
                var completed = false;
                var onClosed = function () {
                    if (completed) return;
                    completed = true;
                    executeResponseMessage(response.message, "", true);
                    reloadProductsSection(salesId);
                };
                $modal.one("hidden.bs.modal", onClosed).modal("hide");
                window.setTimeout(onClosed, 500);
            } else {
                executeResponseMessage(response.message, "", false);
            }
        }).fail(function () {
            $button.prop("disabled", false).html(originalButtonHtml);
            executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
        });
        return false;
    });

    updateDigitalSalesProductEmptyStates($form);
    updateDigitalSalesProductFinanceSummary($form);
};

function openAddProductModal(salesId) {
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.addProductModal, { digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            var $modal = $("#modalProduct");
            $modal.one("shown.bs.modal", function () {
                window.initDigitalSalesProductRuntimeForm();
            });
            $modal.modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải form thêm sản phẩm!", "Lỗi tải dữ liệu!", false);
        });
}

function openEditProductModal(id, salesId) {
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.editProductModal, { id: id, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            var $modal = $("#modalProduct");
            $modal.one("shown.bs.modal", function () {
                window.initDigitalSalesProductRuntimeForm();
            });
            $modal.modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải form sửa sản phẩm!", "Lỗi tải dữ liệu!", false);
        });
}

function deleteProductItem(id, salesId) {
    var $modal = $('#modalConfirmDeleteProduct');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalConfirmDeleteProduct" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1065;">' +
            '<div class="modal-dialog modal-dialog-centered" style="max-width: 450px;" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-danger text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-exclamation-triangle mr-1"></i> Xác nhận xóa sản phẩm</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-3 text-center">' +
            '<i class="fa fa-trash-alt fa-3x text-danger mb-3 d-block"></i>' +
            '<p class="text-dark mb-1 font-weight-bold text-105">Bạn có chắc chắn muốn xóa sản phẩm / dịch vụ này không?</p>' +
            '<small class="text-muted text-85">Thao tác này sẽ xóa sản phẩm khỏi hồ sơ và không thể hoàn tác.</small>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l5 d-flex justify-content-center">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Hủy bỏ</button>' +
            '<button type="button" id="btnConfirmDeleteProductSubmit" class="btn btn-sm btn-danger radius-1 px-4 font-bold"><i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa</button>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalConfirmDeleteProduct');
    }

    $modal.find('#btnConfirmDeleteProductSubmit').off('click').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');
        $.post(_detailUrls.deleteProduct, { id: id, salesId: salesId }, function (res) {
            $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
            $modal.modal('hide');
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css('padding-right', '');
            if (res.status) {
                executeResponseMessage(res.message, "Xóa sản phẩm thành công!", true);
                reloadProductsSection(salesId);
            } else {
                executeResponseMessage(res.message, "Không thể xóa sản phẩm!", false);
            }
        });
    });

    $modal.modal('show');
}

/* ================= 4. Thành viên tham gia (Tab 3) ================= */
function openAddMemberModal(salesId) {
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.addMemberModal, { digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            var $modal = $("#modalMember");
            if ($.fn.select2) {
                $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
            }
            $modal.modal("show");
        })
        .fail(function (xhr) {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải danh sách nhân sự tham gia. Vui lòng thử lại sau!", "Lỗi tải dữ liệu!", false);
        });
}

function openEditMemberModal(id, salesId) {
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.editMemberModal, { id: id, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            var $modal = $("#modalEditMember");
            if ($.fn.select2) {
                $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
            }
            $modal.modal("show");
        })
        .fail(function (xhr) {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải thông tin vai trò thành viên. Vui lòng thử lại sau!", "Lỗi tải dữ liệu!", false);
        });
}

function deleteMemberItem(id, salesId) {
    var $modal = $('#modalConfirmDeleteMember');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalConfirmDeleteMember" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1065;">' +
            '<div class="modal-dialog modal-dialog-centered" style="max-width: 450px;" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-danger text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-exclamation-triangle mr-1"></i> Xác nhận xóa thành viên</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-3 text-center">' +
            '<i class="fa fa-user-times fa-3x text-danger mb-3 d-block"></i>' +
            '<p class="text-dark mb-1 font-weight-bold text-105">Bạn có chắc chắn muốn xóa thành viên này khỏi hồ sơ không?</p>' +
            '<small class="text-muted text-85">Nhân sự sẽ không còn quyền truy cập hồ sơ này nữa.</small>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l5 d-flex justify-content-center">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Hủy bỏ</button>' +
            '<button type="button" id="btnConfirmDeleteMemberSubmit" class="btn btn-sm btn-danger radius-1 px-4 font-bold"><i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa</button>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalConfirmDeleteMember');
    }

    $modal.find('#btnConfirmDeleteMemberSubmit').off('click').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');
        $.post(_detailUrls.deleteMember, { id: id, salesId: salesId }, function (res) {
            $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
            $modal.modal('hide');
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css('padding-right', '');
            if (res.status) {
                executeResponseMessage(res.message, "Xóa thành viên thành công!", true);
                reloadMembersSection(salesId);
            } else {
                executeResponseMessage(res.message, "Không thể xóa thành viên!", false);
            }
        });
    });

    $modal.modal('show');
}

/* ================= 5. Tiến trình & Checklist (Tab 4) ================= */
function initTrackingModalBehavior($modal, salesId) {
    if ($.fn.select2) {
        $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
    }
    if ($.fn.datepicker) {
        $modal.find(".date-picker").datepicker({
            format: "dd/mm/yyyy",
            autoclose: true,
            todayHighlight: true
        });
    }

    if (typeof CKEDITOR !== "undefined" && $modal.find("#Tracking_ResultNote").length > 0) {
        if (CKEDITOR.instances["Tracking_ResultNote"]) {
            CKEDITOR.instances["Tracking_ResultNote"].destroy(true);
        }
        CKEDITOR.replace("Tracking_ResultNote", {
            height: 140,
            toolbar: [
                { name: "basicstyles", items: ["Bold", "Italic", "Underline", "Strike"] },
                { name: "paragraph", items: ["NumberedList", "BulletedList", "-", "Outdent", "Indent"] },
                { name: "links", items: ["Link", "Unlink"] }
            ]
        });
    }

    $modal.on("hidden.bs.modal", function () {
        if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances["Tracking_ResultNote"]) {
            CKEDITOR.instances["Tracking_ResultNote"].destroy(true);
        }
    });

    window.calculateTrackingDeadline = function () {
        var startVal = $("#Tracking_StartDate").val();
        var durationVal = parseInt($("#Tracking_DurationDays").val(), 10);
        if (startVal && !isNaN(durationVal) && durationVal > 0) {
            var parts = startVal.split("/");
            if (parts.length === 3) {
                var day = parseInt(parts[0], 10);
                var month = parseInt(parts[1], 10) - 1;
                var year = parseInt(parts[2], 10);
                var startDate = new Date(year, month, day);
                if (!isNaN(startDate.getTime())) {
                    startDate.setDate(startDate.getDate() + durationVal);
                    var d = ("0" + startDate.getDate()).slice(-2);
                    var m = ("0" + (startDate.getMonth() + 1)).slice(-2);
                    var y = startDate.getFullYear();
                    $("#Tracking_Deadline").val(d + "/" + m + "/" + y);
                }
            }
        }
    };

    window.onTrackingStatusChange = function (select) {
        var val = $(select).val();
        if (val === "3") {
            var now = new Date();
            var d = ("0" + now.getDate()).slice(-2);
            var m = ("0" + (now.getMonth() + 1)).slice(-2);
            var y = now.getFullYear();
            $("#Tracking_CompletedDate").val(d + "/" + m + "/" + y);
            $("#groupCompletedDate").show();
        } else {
            $("#Tracking_CompletedDate").val("");
            $("#groupCompletedDate").hide();
        }
    };

    var $form = $("#frmTrackingModal");
    if ($.validator && $.validator.unobtrusive) {
        $.validator.unobtrusive.parse($form);
    }

    $form.find("#txtTrackingTaskName").on("input propertychange", function () {
        if (($(this).val() || "").trim()) {
            var $valMsg = $form.find("[data-valmsg-for='TaskName']");
            $valMsg.empty().removeClass("field-validation-error").addClass("field-validation-valid");
            $(this).removeClass("input-validation-error border-danger");
        }
    });

    $form.off("submit").on("submit", function (e) {
        e.preventDefault();

        if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances["Tracking_ResultNote"]) {
            CKEDITOR.instances["Tracking_ResultNote"].updateElement();
        }

        if (typeof $form.valid === "function" && !$form.valid()) {
            return false;
        }

        var taskVal = ($form.find("#txtTrackingTaskName").val() || "").trim();
        if (!taskVal) {
            var reqMsg = "Dữ liệu [Tên công việc / Đầu việc] bắt buộc nhập";
            if (typeof App_Message !== "undefined" && App_Message.DigitalSales_Msg_TaskNameRequired) {
                reqMsg = App_Message.DigitalSales_Msg_TaskNameRequired;
            }
            var $valMsg = $form.find("[data-valmsg-for='TaskName']");
            $valMsg.html('<span id="TaskName-error">' + reqMsg + '</span>')
                   .removeClass("field-validation-valid")
                   .addClass("field-validation-error text-danger text-85 font-weight-bold d-block mt-1");
            $form.find("#txtTrackingTaskName").addClass("input-validation-error border-danger").focus();
            return false;
        }

        var $btnSubmit = $form.find("button[type='submit']");
        var origBtnHtml = $btnSubmit.html();
        $btnSubmit.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

        var formData = new FormData(this);
        $.ajax({
            url: _detailUrls.saveTracking,
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (res) {
                $btnSubmit.prop("disabled", false).html(origBtnHtml);
                if (typeof res === "string") {
                    $modal.find("#bodyForm").html(res);
                    initTrackingModalBehavior($modal, salesId);
                    return;
                }
                if (res.status) {
                    $modal.modal("hide");
                    executeResponseMessage(res.message, "Lưu tiến trình thành công!", true);
                    reloadTrackingSection(salesId);
                } else {
                    executeResponseMessage(res.message, "Không thể lưu tiến trình!", false);
                }
            },
            error: function () {
                $btnSubmit.prop("disabled", false).html(origBtnHtml);
                executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
            }
        });
    });
}

function openAddTrackingModal(salesId, processId, timelineId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var params = { digitalSalesId: salesId };
    if (processId) params.processId = processId;
    if (timelineId) params.timelineId = timelineId;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.addTrackingModal, params, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalContainer").html(html);
        var $modal = $("#modalTracking");
        initTrackingModalBehavior($modal, salesId);
        $modal.modal("show");
    });
}

function openAddProgressToProcessModal(salesId, processId, processName, timelineId) {
    openAddTrackingModal(salesId, processId, timelineId);
}

function openEditTrackingModal(id, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.editTrackingModal, { id: id, digitalSalesId: salesId }, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalContainer").html(html);
        var $modal = $("#modalTracking");
        initTrackingModalBehavior($modal, salesId);
        $modal.modal("show");
    });
}

function openTrackingLogsModal(trackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!trackingId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.trackingLogsModal, { trackingId: trackingId, digitalSalesId: salesId }, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalTrackingLogsContainer").remove();
        $("body").append('<div id="modalTrackingLogsContainer">' + html + '</div>');
        var $modal = $("#modalTrackingLogs");

        $modal.on("show.bs.modal", function () {
            setTimeout(function () {
                $('.modal-backdrop:last').addClass('tracking-logs-backdrop').css('z-index', 1080);
            }, 0);
        });

        $modal.on("hidden.bs.modal", function () {
            $("#modalTrackingLogsContainer").remove();
            if ($('.modal.show').length > 0) {
                $("body").addClass("modal-open");
            }
        });

        $modal.modal("show");
    }).fail(function () {
        if (typeof _endWaiting === "function") _endWaiting();
        executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
    });
}

function openUnlockProgressModal(trackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!trackingId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.unlockProgressModal, { trackingId: trackingId, digitalSalesId: salesId }, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalContainer").html(html);
        $("#modalUnlockProgress").modal("show");
    }).fail(function () {
        if (typeof _endWaiting === "function") _endWaiting();
        executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
    });
}

function submitUnlockProgressForm(e, trackingId, salesId) {
    if (e && e.preventDefault) e.preventDefault();
    var reason = ($("#txtUnlockReason").val() || "").trim();
    if (!reason) {
        $("#txtUnlockReason_val").removeClass("d-none");
        $("#txtUnlockReason").addClass("border-danger").focus();
        return false;
    }
    $("#txtUnlockReason_val").addClass("d-none");
    $("#txtUnlockReason").removeClass("border-danger");

    var $btn = $("#btnSubmitUnlockProgress");
    var origHtml = $btn.html();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang mở khóa...');

    $.ajax({
        url: _detailUrls.submitUnlockProgress,
        type: "POST",
        data: { trackingId: trackingId, digitalSalesId: salesId, reason: reason },
        success: function (res) {
            $btn.prop("disabled", false).html(origHtml);
            if (res.status) {
                executeResponseMessage(res.message, res.isSubTask ? "Mở khóa công việc thành công!" : "Mở khóa tiến trình thành công!", true);
                var opened = false;
                var openNext = function () {
                    if (!opened) {
                        opened = true;
                        if (res.isSubTask) {
                            openEditTodoModal(res.trackingId, res.digitalSalesId);
                        } else {
                            openEditTrackingModal(res.trackingId, res.digitalSalesId);
                        }
                    }
                };
                $("#modalUnlockProgress").one("hidden.bs.modal", openNext);
                $("#modalUnlockProgress").modal("hide");
                setTimeout(openNext, 400);
            } else {
                executeResponseMessage(res.message, res.isSubTask ? "Không thể mở khóa công việc!" : "Không thể mở khóa tiến trình!", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html(origHtml);
            executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
        }
    });
    return false;
}

function deleteTrackingItem(id, salesId) {
    var $modal = $('#modalConfirmDeleteTracking');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalConfirmDeleteTracking" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1065;">' +
            '<div class="modal-dialog modal-dialog-centered" style="max-width: 450px;" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-danger text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-exclamation-triangle mr-1"></i> Xác nhận xóa công việc</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-3 text-center">' +
            '<i class="fa fa-tasks fa-3x text-danger mb-3 d-block"></i>' +
            '<p class="text-dark mb-1 font-weight-bold text-105">Bạn có chắc chắn muốn xóa tiến trình / checklist này không?</p>' +
            '<small class="text-muted text-85">Thao tác này không thể hoàn tác.</small>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l5 d-flex justify-content-center">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Hủy bỏ</button>' +
            '<button type="button" id="btnConfirmDeleteTrackingSubmit" class="btn btn-sm btn-danger radius-1 px-4 font-bold"><i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa</button>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalConfirmDeleteTracking');
    }

    $modal.find('#btnConfirmDeleteTrackingSubmit').off('click').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');
        $.post(_detailUrls.deleteTracking, { id: id, salesId: salesId }, function (res) {
            $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
            $modal.modal('hide');
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css('padding-right', '');
            if (res.status) {
                executeResponseMessage(res.message, "Xóa tiến trình thành công!", true);
                reloadTrackingSection(salesId);
            } else {
                executeResponseMessage(res.message, "Không thể xóa tiến trình!", false);
            }
        });
    });

    $modal.modal('show');
}

/* ================= 4.1. Cập nhật Quy trình (Cây bút chì) ================= */
function openChangeProcessModal(salesId, statusId, currentProcessId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId || !statusId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.changeProcessModal, { digitalSalesId: salesId, statusId: statusId, currentProcessId: currentProcessId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            $("#changeProcessModal").modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải danh sách quy trình!", "Lỗi kết nối!", false);
        });
}

function submitChangeProcess() {
    var $form = $("#frmChangeProcess");
    var salesId = $("#ChangeProcess_DigitalSalesID").val();
    var statusId = $("#ChangeProcess_StatusID").val();
    var newProcessId = $("input[name='SelectedProcessID']:checked").val();

    if (!newProcessId) {
        executeResponseMessage("Vui lòng chọn một quy trình áp dụng!", "Thông báo", false);
        return;
    }

    var $btn = $("#btnSaveChangeProcess");
    var origHtml = $btn.html();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xử lý...');

    $.post(_detailUrls.saveChangeProcess, { digitalSalesId: salesId, statusId: statusId, newProcessId: newProcessId }, function (res) {
        $btn.prop("disabled", false).html(origHtml);
        if (res.status) {
            $("#changeProcessModal").modal("hide");
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css('padding-right', '');
            executeResponseMessage(res.message, "Cập nhật quy trình thành công!", true);
            reloadTrackingSection(salesId);
        } else {
            executeResponseMessage(res.message, "Không thể cập nhật quy trình!", false);
        }
    }).fail(function () {
        $btn.prop("disabled", false).html(origHtml);
        executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
    });
}

/* ================= 4.2. Quản lý Todo list (Công việc nhỏ bên trong tiến trình) ================= */
function parseVnDate(str) {
    if (!str) return null;
    var p = str.split('/');
    if (p.length !== 3) return null;
    return new Date(parseInt(p[2], 10), parseInt(p[1], 10) - 1, parseInt(p[0], 10));
}

function openAddTodoModal(parentTrackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId || !parentTrackingId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.addTodoModal, { parentTrackingId: parentTrackingId, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            initTodoModalControls();
            $("#todoModal").modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải form thêm việc con!", "Lỗi kết nối!", false);
        });
}

function openEditTodoModal(id, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId || !id) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.editTodoModal, { id: id, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            initTodoModalControls();
            $("#todoModal").modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải form sửa việc con!", "Lỗi kết nối!", false);
        });
}

function initTodoModalControls() {
    var $modal = $("#todoModal");
    if ($.fn.select2) {
        $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
    }
    if ($.fn.datepicker) {
        var maxDeadlineIso = $("#Todo_MaxDeadlineIso").val();
        var endDateOpt = undefined;
        if (maxDeadlineIso) {
            var parts = maxDeadlineIso.split('-');
            if (parts.length === 3) {
                endDateOpt = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            }
        }
        $modal.find("#Todo_StartDate").datepicker({
            format: "dd/mm/yyyy",
            autoclose: true,
            todayHighlight: true
        });
        $modal.find("#Todo_Deadline").datepicker({
            format: "dd/mm/yyyy",
            autoclose: true,
            todayHighlight: true,
            endDate: endDateOpt
        });
    }
}

function submitTodoItem() {
    var $form = $("#frmTodoItem");
    var taskName = ($("#Todo_TaskName").val() || "").trim();
    if (!taskName) {
        $("#Todo_TaskName").addClass("is-invalid border-danger").focus();
        executeResponseMessage("Vui lòng nhập tên công việc con!", "Thiếu thông tin", false);
        return false;
    }
    $("#Todo_TaskName").removeClass("is-invalid border-danger");

    // Validate số ngày thực hiện
    var durationDays = parseInt($("#Todo_DurationDays").val()) || 0;
    if (durationDays < 1) {
        $("#Todo_DurationDays").addClass("is-invalid border-danger").focus();
        executeResponseMessage("Số ngày thực hiện phải >= 1!", "Thiếu thông tin", false);
        return false;
    }
    $("#Todo_DurationDays").removeClass("is-invalid border-danger");

    // RÀNG BUỘC NGHIỆP VỤ: Deadline của Todo <= StartDate của Tiến trình + Tổng ngày của Tiến trình
    var deadlineStr = $("#Todo_Deadline").val();
    var maxDeadlineIso = $("#Todo_MaxDeadlineIso").val();
    if (deadlineStr && maxDeadlineIso) {
        var deadlineDate = parseVnDate(deadlineStr);
        var maxDateParts = maxDeadlineIso.split('-');
        var maxDate = new Date(parseInt(maxDateParts[0], 10), parseInt(maxDateParts[1], 10) - 1, parseInt(maxDateParts[2], 10));
        if (deadlineDate && maxDate && deadlineDate.getTime() > maxDate.getTime()) {
            $("#todoDeadlineError").removeClass("d-none");
            $("#Todo_Deadline").addClass("is-invalid border-danger").focus();
            executeResponseMessage("Hạn xử lý không được lớn hơn hạn tối đa của tiến trình!", "Lỗi ràng buộc thời hạn", false);
            return false;
        }
    }
    $("#todoDeadlineError").addClass("d-none");
    $("#Todo_Deadline").removeClass("is-invalid border-danger");

    var $btn = $("#btnSaveTodoItem");
    var origHtml = $btn.html();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

    // Sử dụng FormData để hỗ trợ file upload
    var formData = new FormData($form[0]);
    var salesId = $("#Todo_DigitalSalesID").val();

    $.ajax({
        url: _detailUrls.saveTodo,
        type: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            $btn.prop("disabled", false).html(origHtml);
            if (res.status) {
                $("#todoModal").modal("hide");
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css('padding-right', '');
                executeResponseMessage(res.message, "Lưu công việc thành công!", true);
                reloadTrackingSection(salesId);
            } else {
                executeResponseMessage(res.message, "Không thể lưu công việc!", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html(origHtml);
            executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
        }
    });
}

function calculateTodoDeadline() {
    var startDateStr = $("#Todo_StartDate").val();
    var durationDays = parseInt($("#Todo_DurationDays").val()) || 0;
    if (!startDateStr || durationDays < 1) return;

    var startDate = parseVnDate(startDateStr);
    if (!startDate) return;

    var deadline = new Date(startDate);
    deadline.setDate(deadline.getDate() + durationDays);

    var dd = ('0' + deadline.getDate()).slice(-2);
    var mm = ('0' + (deadline.getMonth() + 1)).slice(-2);
    var yyyy = deadline.getFullYear();
    var formattedDate = dd + '/' + mm + '/' + yyyy;

    $("#Todo_Deadline").val(formattedDate);
}

/* ================= 4.3. Thao tác Nhanh: Báo cáo | Xác nhận | Mở khóa | Xóa ================= */
function actionConfirmTracking(trackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!trackingId) return;
    $.post(_detailUrls.confirmTracking, { trackingId: trackingId, digitalSalesId: salesId }, function (res) {
        if (res.status) {
            executeResponseMessage(res.message, "Xác nhận hoàn thành thành công!", true);
            reloadTrackingSection(salesId);
        } else {
            executeResponseMessage(res.message, "Không thể xác nhận hoàn thành!", false);
        }
    }).fail(function () {
        executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
    });
}

function actionUnlockTracking(trackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!trackingId) return;
    $.post(_detailUrls.unlockTracking, { trackingId: trackingId, digitalSalesId: salesId }, function (res) {
        if (res.status) {
            executeResponseMessage(res.message, "Mở khóa xác nhận thành công!", true);
            reloadTrackingSection(salesId);
        } else {
            executeResponseMessage(res.message, "Không thể mở khóa xác nhận!", false);
        }
    }).fail(function () {
        executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
    });
}

function openTrackingReportModal(trackingId, salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!trackingId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.reportTrackingModal, { trackingId: trackingId, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            $("#trackingReportModal").modal("show");
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải form báo cáo!", "Lỗi kết nối!", false);
        });
}

function submitTrackingReport() {
    var $form = $("#frmTrackingReport");
    var note = ($("#Report_ResultNote").val() || "").trim();
    if (!note) {
        $("#Report_ResultNote").addClass("is-invalid border-danger").focus();
        executeResponseMessage("Vui lòng nhập nội dung báo cáo kết quả!", "Thiếu thông tin", false);
        return false;
    }
    $("#Report_ResultNote").removeClass("is-invalid border-danger");

    var $btn = $("#btnSaveTrackingReport");
    var origHtml = $btn.html();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

    var formData = new FormData($form[0]);
    var salesId = $("#Report_DigitalSalesID").val();

    $.ajax({
        url: _detailUrls.saveTrackingReport,
        type: "POST",
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            $btn.prop("disabled", false).html(origHtml);
            if (res.status) {
                $("#trackingReportModal").modal("hide");
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css('padding-right', '');
                executeResponseMessage(res.message, "Lưu báo cáo tiến độ thành công!", true);
                reloadTrackingSection(salesId);
            } else {
                executeResponseMessage(res.message, "Không thể lưu báo cáo!", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html(origHtml);
            executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
        }
    });
}

function actionDeleteTracking(trackingId, salesId) {
    deleteTrackingItem(trackingId, salesId);
}

// =========================================================================
// CHỨC NĂNG IMPORT TIẾN TRÌNH & CÔNG VIỆC CON (THEO ĐẶC TẢ)
// =========================================================================
var _progressImportPreviewData = [];
var _todoImportPreviewData = [];

function openImportProgressModal(salesId, processId, timelineId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId || !processId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    var params = { processId: processId, digitalSalesId: salesId };
    if (timelineId) params.timelineId = timelineId;
    $.get(_detailUrls.importProgressModal, params)
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            _progressImportPreviewData = [];
            $("#importProgressModal").modal("show");
            $("#fileProgressImport").on("change", function () {
                var fileName = $(this).val().split("\\").pop();
                $("#lblProgressFileName").text(fileName || "Chọn tệp Excel từ máy tính...");
            });
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải giao diện import tiến trình!", "Lỗi kết nối!", false);
        });
}

function readProgressImportExcel() {
    var fileInput = document.getElementById("fileProgressImport");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        executeResponseMessage("Vui lòng chọn tệp Excel (.xlsx hoặc .xls) trước khi đọc dữ liệu!", "Cảnh báo", false);
        return;
    }

    var $modal = $("#importProgressModal");
    var downloadLink = $modal.find("a[href*='processId']").attr("href");
    var procId = downloadLink ? getDetailUrlParam(downloadLink, "processId") : "0";

    var formData = new FormData();
    formData.append("importFile", fileInput.files[0]);
    formData.append("processId", procId);
    formData.append("digitalSalesId", _currentDigitalSalesId);

    var $btn = $("#btnReadProgressExcel");
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang đọc file...');

    $.ajax({
        url: _detailUrls.previewImportProgress,
        type: "POST",
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            $btn.prop("disabled", false).html('<i class="fa fa-search mr-1"></i> Đọc dữ liệu từ file');
            if (res && res.status) {
                _progressImportPreviewData = res.rows || [];
                renderProgressImportPreview(res);
            } else {
                executeResponseMessage(res && res.message ? res.message : "Đọc tệp thất bại!", "Thông báo", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html('<i class="fa fa-search mr-1"></i> Đọc dữ liệu từ file');
            executeResponseMessage("Lỗi kết nối khi đọc tệp Excel!", "Lỗi!", false);
        }
    });
}

function renderProgressImportPreview(data) {
    var rows = data.rows || [];
    $("#boxProgressPreview").removeClass("d-none");
    $("#badgeProgressTotalRows").text(data.total + " dòng");
    $("#badgeProgressValidRows").text(data.validCount + " hợp lệ");
    $("#badgeProgressErrorRows").text(data.errorCount + " lỗi");

    var html = "";
    if (rows.length === 0) {
        html = '<tr><td colspan="8" class="text-center text-muted py-3">Không có dữ liệu</td></tr>';
    } else {
        $.each(rows, function (idx, item) {
            var isValid = item.IsValid;
            var trClass = isValid ? "" : "bgc-danger-l4 text-danger-d2";
            var errBadge = isValid 
                ? '<span class="badge badge-success px-2 py-1"><i class="fa fa-check mr-1"></i>Hợp lệ</span>'
                : '<span class="text-danger font-bold"><i class="fa fa-exclamation-triangle mr-1"></i>' + (item.ErrorMessage || "Lỗi") + '</span>';

            html += '<tr class="' + trClass + '">' +
                '<td class="text-center">' + item.RowIndex + '</td>' +
                '<td class="font-bold">' + (item.TaskName || "") + '</td>' +
                '<td>' + (item.AssignedUserName || "—") + '</td>' +
                '<td class="text-center">' + (item.StartDateStr || "") + '</td>' +
                '<td class="text-center">' + (item.DurationDays || 3) + ' ngày</td>' +
                '<td class="text-center">' + (item.DeadlineStr || (item.Deadline ? formatDetailDateVN(item.Deadline) : "—")) + '</td>' +
                '<td>' + (item.Note || "") + '</td>' +
                '<td>' + errBadge + '</td>' +
                '</tr>';
        });
    }

    $("#tbodyProgressPreview").html(html);

    if (data.validCount > 0) {
        $("#btnConfirmProgressImport").removeClass("d-none").html('<i class="fa fa-check mr-1"></i> Xác nhận Import (' + data.validCount + ' dòng hợp lệ)');
        $("#lblProgressFooterNote").html('Sẵn sàng import <strong>' + data.validCount + '</strong> tiến trình hợp lệ vào quy trình.');
    } else {
        $("#btnConfirmProgressImport").addClass("d-none");
        $("#lblProgressFooterNote").html('<span class="text-danger font-bold">Tệp không có dòng nào hợp lệ để import. Vui lòng kiểm tra lại cột lỗi!</span>');
    }
}

function clearProgressImportPreview() {
    _progressImportPreviewData = [];
    $("#fileProgressImport").val("");
    $("#lblProgressFileName").text("Chọn tệp Excel từ máy tính...");
    $("#tbodyProgressPreview").empty();
    $("#boxProgressPreview").addClass("d-none");
    $("#btnConfirmProgressImport").addClass("d-none");
    $("#lblProgressFooterNote").text("Đã xóa dữ liệu xem trước. Vui lòng chọn tệp mới.");
}

function executeConfirmProgressImport() {
    var validRows = [];
    $.each(_progressImportPreviewData, function (_, r) {
        if (r.IsValid) validRows.push(r);
    });

    if (validRows.length === 0) {
        executeResponseMessage("Không có dữ liệu hợp lệ nào để import!", "Cảnh báo", false);
        return;
    }

    var $btn = $("#btnConfirmProgressImport");
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

    var downloadLink = $("#importProgressModal").find("a[href*='processId']").attr("href");
    var procId = downloadLink ? getDetailUrlParam(downloadLink, "processId") : "0";
    var timelineId = $("#importProgress_TimelineID").val() || $("#importProgressModal").data("timeline-id");

    $.ajax({
        url: _detailUrls.confirmImportProgress,
        type: "POST",
        data: {
            processId: parseInt(procId),
            digitalSalesId: _currentDigitalSalesId,
            validDataJson: JSON.stringify(validRows),
            timelineId: timelineId ? parseInt(timelineId) : null
        },
        success: function (res) {
            $btn.prop("disabled", false).html('<i class="fa fa-check mr-1"></i> Xác nhận dữ liệu Import');
            if (res && res.status) {
                executeResponseMessage(res.message || "Import thành công!", "Thành công!", true);
                $("#importProgressModal").modal("hide");
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css('padding-right', '');
                reloadTrackingSection(_currentDigitalSalesId);
            } else {
                executeResponseMessage(res && res.message ? res.message : "Import thất bại!", "Lỗi", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html('<i class="fa fa-check mr-1"></i> Xác nhận dữ liệu Import');
            executeResponseMessage("Lỗi kết nối khi lưu dữ liệu import!", "Lỗi", false);
        }
    });
}

function openImportTodoModal(salesId, processId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId || !processId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.importTodoModal, { processId: processId, digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            _todoImportPreviewData = [];
            $("#importTodoModal").modal("show");
            $("#fileTodoImport").on("change", function () {
                var fileName = $(this).val().split("\\").pop();
                $("#lblTodoFileName").text(fileName || "Chọn tệp Excel từ máy tính...");
            });
        })
        .fail(function () {
            if (typeof _endWaiting === "function") _endWaiting();
            executeResponseMessage("Không thể tải giao diện import công việc con!", "Lỗi kết nối!", false);
        });
}

function readTodoImportExcel() {
    var fileInput = document.getElementById("fileTodoImport");
    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        executeResponseMessage("Vui lòng chọn tệp Excel (.xlsx hoặc .xls) trước khi đọc dữ liệu!", "Cảnh báo", false);
        return;
    }

    var $modal = $("#importTodoModal");
    var downloadLink = $modal.find("a[href*='processId']").attr("href");
    var procId = downloadLink ? getDetailUrlParam(downloadLink, "processId") : "0";

    var formData = new FormData();
    formData.append("importFile", fileInput.files[0]);
    formData.append("processId", procId);
    formData.append("digitalSalesId", _currentDigitalSalesId);

    var $btn = $("#btnReadTodoExcel");
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang đọc file...');

    $.ajax({
        url: _detailUrls.previewImportTodo,
        type: "POST",
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            $btn.prop("disabled", false).html('<i class="fa fa-search mr-1"></i> Đọc dữ liệu từ file');
            if (res && res.status) {
                _todoImportPreviewData = res.rows || [];
                renderTodoImportPreview(res);
            } else {
                executeResponseMessage(res && res.message ? res.message : "Đọc tệp thất bại!", "Thông báo", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html('<i class="fa fa-search mr-1"></i> Đọc dữ liệu từ file');
            executeResponseMessage("Lỗi kết nối khi đọc tệp Excel!", "Lỗi!", false);
        }
    });
}

function renderTodoImportPreview(data) {
    var rows = data.rows || [];
    $("#boxTodoPreview").removeClass("d-none");
    $("#badgeTodoTotalRows").text(data.total + " dòng");
    $("#badgeTodoValidRows").text(data.validCount + " hợp lệ");
    $("#badgeTodoErrorRows").text(data.errorCount + " lỗi");

    var html = "";
    if (rows.length === 0) {
        html = '<tr><td colspan="11" class="text-center text-muted py-3">Không có dữ liệu</td></tr>';
    } else {
        $.each(rows, function (idx, item) {
            var isValid = item.IsValid;
            var trClass = isValid ? "" : "bgc-danger-l4 text-danger-d2";
            var errBadge = isValid 
                ? '<span class="badge badge-success px-2 py-1"><i class="fa fa-check mr-1"></i>Hợp lệ</span>'
                : '<span class="text-danger font-bold"><i class="fa fa-exclamation-triangle mr-1"></i>' + (item.ErrorMessage || "Lỗi") + '</span>';

            var statusBadge = '<span class="badge badge-secondary px-2 py-05 text-80 radius-1">Chưa thực hiện</span>';
            if (item.Status === 2) {
                statusBadge = '<span class="badge badge-warning px-2 py-05 text-80 radius-1">Đang thực hiện</span>';
            } else if (item.Status === 3) {
                statusBadge = '<span class="badge badge-success px-2 py-05 text-80 radius-1">Đã xong</span>';
            }

            html += '<tr class="' + trClass + '">' +
                '<td class="text-center">' + item.RowIndex + '</td>' +
                '<td class="text-center font-bold text-primary">' + (item.TrackingCode || "—") + '</td>' +
                '<td>' + (item.ParentTaskName || '<span class="text-danger font-italic">Không tìm thấy</span>') + '</td>' +
                '<td class="font-bold">' + (item.TaskName || "") + '</td>' +
                '<td>' + (item.AssignedUserName || "—") + '</td>' +
                '<td class="text-center">' + statusBadge + '</td>' +
                '<td class="text-center">' + (item.StartDateStr || "") + '</td>' +
                '<td class="text-center font-bold">' + (item.DurationDays ? item.DurationDays + ' ngày' : "—") + '</td>' +
                '<td class="text-center font-bold">' + (item.DeadlineStr || "") + '</td>' +
                '<td>' + (item.Note || "") + '</td>' +
                '<td>' + errBadge + '</td>' +
                '</tr>';
        });
    }

    $("#tbodyTodoPreview").html(html);

    if (data.validCount > 0) {
        $("#btnConfirmTodoImport").removeClass("d-none").html('<i class="fa fa-check mr-1"></i> Xác nhận Import (' + data.validCount + ' việc hợp lệ)');
        $("#lblTodoFooterNote").html('Sẵn sàng import <strong>' + data.validCount + '</strong> công việc con hợp lệ vào tiến trình.');
    } else {
        $("#btnConfirmTodoImport").addClass("d-none");
        $("#lblTodoFooterNote").html('<span class="text-danger font-bold">Tệp không có công việc nào hợp lệ để import. Vui lòng kiểm tra lại cột lỗi!</span>');
    }
}

function clearTodoImportPreview() {
    _todoImportPreviewData = [];
    $("#fileTodoImport").val("");
    $("#lblTodoFileName").text("Chọn tệp Excel từ máy tính...");
    $("#tbodyTodoPreview").empty();
    $("#boxTodoPreview").addClass("d-none");
    $("#btnConfirmTodoImport").addClass("d-none");
    $("#lblTodoFooterNote").text("Đã xóa dữ liệu xem trước. Vui lòng chọn tệp mới.");
}

function executeConfirmTodoImport() {
    var validRows = [];
    $.each(_todoImportPreviewData, function (_, r) {
        if (r.IsValid) validRows.push(r);
    });

    if (validRows.length === 0) {
        executeResponseMessage("Không có dữ liệu hợp lệ nào để import!", "Cảnh báo", false);
        return;
    }

    var $btn = $("#btnConfirmTodoImport");
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

    var downloadLink = $("#importTodoModal").find("a[href*='processId']").attr("href");
    var procId = downloadLink ? getDetailUrlParam(downloadLink, "processId") : "0";

    $.ajax({
        url: _detailUrls.confirmImportTodo,
        type: "POST",
        data: {
            processId: parseInt(procId),
            digitalSalesId: _currentDigitalSalesId,
            validDataJson: JSON.stringify(validRows)
        },
        success: function (res) {
            $btn.prop("disabled", false).html('<i class="fa fa-check mr-1"></i> Xác nhận dữ liệu Import');
            if (res && res.status) {
                executeResponseMessage(res.message || "Import thành công!", "Thành công!", true);
                $("#importTodoModal").modal("hide");
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css('padding-right', '');
                reloadTrackingSection(_currentDigitalSalesId);
            } else {
                executeResponseMessage(res && res.message ? res.message : "Import thất bại!", "Lỗi", false);
            }
        },
        error: function () {
            $btn.prop("disabled", false).html('<i class="fa fa-check mr-1"></i> Xác nhận dữ liệu Import');
            executeResponseMessage("Lỗi kết nối khi lưu dữ liệu import!", "Lỗi", false);
        }
    });
}

function getDetailUrlParam(url, param) {
    if (!url) return "";
    var regex = new RegExp("[?&]" + param + "(=([^&#]*)|&|#|$)");
    var results = regex.exec(url);
    if (!results || !results[2]) return "";
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function formatDetailDateVN(dateVal) {
    if (!dateVal) return "";
    var d;
    if (typeof dateVal === "string") {
        if (dateVal.indexOf("/Date(") !== -1) {
            var m = dateVal.match(/\/Date\((\d+)\)\//);
            if (m) {
                d = new Date(parseInt(m[1], 10));
            }
        } else if (/^\d{2}\/\d{2}\/\d{4}$/.test(dateVal)) {
            return dateVal;
        }
    }
    if (!d) {
        d = new Date(dateVal);
    }
    if (isNaN(d.getTime())) return dateVal;
    var day = ("0" + d.getDate()).slice(-2);
    var month = ("0" + (d.getMonth() + 1)).slice(-2);
    var year = d.getFullYear();
    return day + "/" + month + "/" + year;
}

function loadContactPersonsByCustomer(customerId, targetSelector) {
    if (!customerId) {
        $(targetSelector).empty().append('<option value="">-- Chọn người liên hệ --</option>');
        return;
    }
    $.get(_detailUrls.getContactPersons, { customerId: customerId }, function (items) {
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

/* ================= 6. Quản lý tệp đính kèm (Attachments) ================= */
function previewImageDirect(src, title) {
    var $modal = $('#modalImagePreview');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalImagePreview" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1070;">' +
            '<div class="modal-dialog modal-lg modal-dialog-centered" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden bg-dark">' +
            '<div class="modal-header bgc-dark text-white py-2 px-3 border-b-1 brc-grey-d1">' +
            '<h6 class="modal-title font-bold text-white mb-0 text-truncate" id="imgPreviewTitle"><i class="fa fa-image mr-1"></i> Xem ảnh</h6>' +
            '<button type="button" class="close text-white opacity-75 btn-h-opacity-1" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-2 text-center bgc-black-tp9 d-flex flex-column align-items-center justify-content-center position-relative" style="min-height: 320px; max-height: 80vh; overflow: auto;">' +
            '<div id="imgPreviewLoading" class="text-center py-5">' +
            '<i class="fa fa-spinner fa-spin fa-2x text-white mb-2"></i>' +
            '<div class="text-white-tp2 text-85 font-italic">Đang tải hình ảnh...</div>' +
            '</div>' +
            '<div id="imgPreviewError" class="text-center py-4 px-3" style="display: none;">' +
            '<div class="w-6 h-6 radius-round bgc-danger-l3 text-danger d-inline-flex align-items-center justify-content-center mb-3" style="width: 52px; height: 52px; border-radius: 50%;">' +
            '<i class="fa fa-exclamation-triangle fa-2x"></i>' +
            '</div>' +
            '<h6 class="text-white font-weight-bold mb-2">Không thể tải hình ảnh</h6>' +
            '<p class="text-white-tp3 text-85 mb-3">Tệp tin không tồn tại hoặc đã bị xóa trên máy chủ.</p>' +
            '<div class="d-flex justify-content-center" style="gap: 8px;">' +
            '<a id="btnErrorDownload" href="#" class="btn btn-sm btn-primary radius-1 px-3 font-bold"><i class="fa fa-download mr-1"></i> Tải về tệp</a>' +
            '<button type="button" class="btn btn-sm btn-outline-light radius-1 px-3" data-dismiss="modal">Đóng</button>' +
            '</div>' +
            '</div>' +
            '<img id="imgPreviewSource" src="" class="img-fluid radius-1 shadow" style="max-height: 75vh; object-fit: contain; display: none;" alt="" />' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-dark border-t-1 brc-grey-d1 d-flex justify-content-between">' +
            '<span class="text-white-tp2 text-85 font-italic text-truncate mr-2" id="imgPreviewFileName" style="max-width: 50%;"></span>' +
            '<div>' +
            '<a id="btnDownloadPreviewImage" href="#" class="btn btn-sm btn-primary radius-1 px-3 font-bold"><i class="fa fa-download mr-1"></i> Tải về</a>' +
            '<button type="button" class="btn btn-sm btn-secondary radius-1 px-3 ml-2" data-dismiss="modal">Đóng</button>' +
            '</div>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalImagePreview');
    }

    var cleanSrc = (src || '').trim();
    var viewUrl = cleanSrc;
    var downloadUrl = cleanSrc;

    if (cleanSrc.indexOf('/Cate/DigitalSales/ViewAttachment') !== 0) {
        viewUrl = '/Cate/DigitalSales/ViewAttachment?filePath=' + encodeURIComponent(cleanSrc);
    }
    if (cleanSrc.indexOf('/Cate/DigitalSales/DownloadAttachment') !== 0) {
        downloadUrl = '/Cate/DigitalSales/DownloadAttachment?filePath=' + encodeURIComponent(cleanSrc);
    }

    $modal.find('#imgPreviewTitle').html('<i class="fa fa-image mr-1"></i> ' + (title || 'Xem ảnh'));
    $modal.find('#imgPreviewFileName').text(title || '');
    $modal.find('#btnDownloadPreviewImage').attr('href', downloadUrl);
    $modal.find('#btnErrorDownload').attr('href', downloadUrl);

    $modal.find('#imgPreviewLoading').show();
    $modal.find('#imgPreviewError').hide();

    var $img = $modal.find('#imgPreviewSource');
    $img.hide().removeAttr('src');
    $img.off('load.preview error.preview');

    $img.on('load.preview', function () {
        $modal.find('#imgPreviewLoading').hide();
        $modal.find('#imgPreviewError').hide();
        $img.fadeIn(150);
    });

    $img.on('error.preview', function () {
        $modal.find('#imgPreviewLoading').hide();
        $img.hide();
        $modal.find('#imgPreviewError').fadeIn(150);
    });

    $img.attr('src', viewUrl);
    $modal.modal('show');
}

function openUploadAttachmentModal(salesId) {
    var $modal = $('#modalUploadAttachment');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalUploadAttachment" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1060;">' +
            '<div class="modal-dialog modal-md modal-dialog-centered" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-primary text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-cloud-upload-alt mr-1"></i> Tải lên tệp đính kèm mới</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<form id="frmUploadAttachment" enctype="multipart/form-data">' +
            '<div class="modal-body p-3">' +
            '<input type="hidden" name="id" id="uploadSalesId" value="" />' +
            '<div class="form-group mb-3">' +
            '<label class="font-bold text-secondary-d2 mb-1 d-block text-90">Chọn tệp tài liệu / hình ảnh:</label>' +
            '<input type="file" name="fileUpload" id="fileUploadInput" multiple="multiple" class="form-control-file border p-2 rounded bgc-grey-l5" required />' +
            '<small class="text-muted text-85 mt-1 d-block"><i class="fa fa-info-circle text-info mr-1"></i>Có thể chọn cùng lúc nhiều tệp (hình ảnh, Word, Excel, PDF, Zip...).</small>' +
            '</div>' +
            '<div id="selectedFilesList" class="p-2 bgc-grey-l4 radius-1 text-85 text-secondary" style="display: none; max-height: 120px; overflow-y: auto;"></div>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l4">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Đóng</button>' +
            '<button type="submit" id="btnSubmitUpload" class="btn btn-sm btn-primary radius-1 px-4 font-bold"><i class="fa fa-cloud-upload-alt mr-1"></i> Tải lên ngay</button>' +
            '</div>' +
            '</form>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalUploadAttachment');

        $modal.find('#fileUploadInput').on('change', function () {
            var files = this.files;
            var $list = $modal.find('#selectedFilesList');
            if (files && files.length > 0) {
                var html = '<strong>' + files.length + ' tệp đã chọn:</strong><ul class="mb-0 pl-3 mt-1">';
                for (var i = 0; i < files.length; i++) {
                    html += '<li>' + files[i].name + ' (' + (files[i].size / 1024).toFixed(1) + ' KB)</li>';
                }
                html += '</ul>';
                $list.html(html).show();
            } else {
                $list.empty().hide();
            }
        });

        $modal.find('#frmUploadAttachment').on('submit', function (e) {
            e.preventDefault();
            var $btn = $modal.find('#btnSubmitUpload');
            var formData = new FormData(this);
            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang tải lên...');

            $.ajax({
                url: _detailUrls.uploadAttachment,
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                success: function (res) {
                    $btn.prop('disabled', false).html('<i class="fa fa-cloud-upload-alt mr-1"></i> Tải lên ngay');
                    if (res.status) {
                        $modal.modal('hide');
                        executeResponseMessage(res.message, "Tải lên tệp thành công!", true);
                        reloadAttachmentsSection(salesId);
                    } else {
                        executeResponseMessage(res.message, "Tải lên tệp thất bại!", false);
                    }
                },
                error: function () {
                    $btn.prop('disabled', false).html('<i class="fa fa-cloud-upload-alt mr-1"></i> Tải lên ngay');
                    executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
                }
            });
        });
    }

    $modal.find('#uploadSalesId').val(salesId);
    $modal.find('#fileUploadInput').val('');
    $modal.find('#selectedFilesList').empty().hide();
    $modal.modal('show');
}

function confirmDeleteAttachment(salesId, filePath, fileName) {
    var $modal = $('#modalConfirmDeleteAttachment');
    if ($modal.length === 0) {
        var modalHtml = '<div class="modal fade" id="modalConfirmDeleteAttachment" tabindex="-1" role="dialog" aria-hidden="true" style="z-index: 1065;">' +
            '<div class="modal-dialog modal-dialog-centered" style="max-width: 450px;" role="document">' +
            '<div class="modal-content border-0 shadow-lg radius-2 overflow-hidden">' +
            '<div class="modal-header bgc-danger text-white py-2 px-3">' +
            '<h6 class="modal-title font-bold text-white mb-0"><i class="fa fa-exclamation-triangle mr-1"></i> Xác nhận xóa tệp</h6>' +
            '<button type="button" class="close text-white" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body p-3 text-center">' +
            '<i class="fa fa-trash-alt fa-3x text-danger mb-3 d-block"></i>' +
            '<p class="text-dark mb-1 font-weight-bold">Bạn có chắc chắn muốn xóa tệp này khỏi hồ sơ không?</p>' +
            '<p class="text-secondary-d2 font-mono text-90 px-2 py-1 bgc-grey-l4 radius-1 text-truncate" id="delAttachmentFileName"></p>' +
            '<small class="text-muted text-85">Thao tác này sẽ gỡ tệp khỏi hồ sơ và không thể hoàn tác.</small>' +
            '</div>' +
            '<div class="modal-footer py-2 bgc-grey-l4 d-flex justify-content-center">' +
            '<button type="button" class="btn btn-sm btn-outline-secondary radius-1 px-3" data-dismiss="modal"><i class="fa fa-times mr-1"></i> Hủy bỏ</button>' +
            '<button type="button" id="btnConfirmDeleteAttachmentSubmit" class="btn btn-sm btn-danger radius-1 px-4 font-bold"><i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa</button>' +
            '</div>' +
            '</div></div></div>';
        $('body').append(modalHtml);
        $modal = $('#modalConfirmDeleteAttachment');
    }

    $modal.find('#delAttachmentFileName').text(fileName || filePath).attr('title', fileName || filePath);
    $modal.find('#btnConfirmDeleteAttachmentSubmit').off('click').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');
        $.ajax({
            url: _detailUrls.deleteAttachment,
            type: 'POST',
            data: { id: salesId, filePath: filePath },
            success: function (res) {
                $btn.prop('disabled', false).html('<i class="fa fa-trash-alt mr-1"></i> Đồng ý xóa');
                if (res.status) {
                    $modal.modal('hide');
                    executeResponseMessage(res.message, "Xóa tệp đính kèm thành công!", true);
                    reloadAttachmentsSection(salesId);
                } else {
                    executeResponseMessage(res.message, "Xóa tệp thất bại!", false);
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

function applyKeyProjectUI(isChecked) {
    var $badge = $('#badgeKeyProject');
    if (isChecked) {
        $badge.removeClass('d-none');
    } else {
        $badge.addClass('d-none');
    }
}

function applyFollowSalesUI(isChecked) {
    var $badge = $('#badgeFollowed');
    if (isChecked) {
        $badge.removeClass('d-none');
    } else {
        $badge.addClass('d-none');
    }
}

var _isKeyProjectToggling = false;
function toggleKeyProject(salesId, isChecked) {
    var $chk = $('#chkIsKeyProject');
    if (_isKeyProjectToggling) return;
    _isKeyProjectToggling = true;

    // 1. Phản hồi giao diện tức thì 0ms (Optimistic UI) không cần chờ đợi hay loading
    applyKeyProjectUI(isChecked);

    // 2. Chạy ngầm dưới background (global: false không kích hoạt preloader/spinner hay reload)
    $.ajax({
        url: _detailUrls.toggleKeyProject,
        type: "POST",
        data: { id: salesId, isKeyProject: isChecked },
        dataType: "JSON",
        global: false,
        success: function (res) {
            _isKeyProjectToggling = false;
            if (res && res.status) {
                executeResponseMessage(res.message, isChecked ? "Đã đánh dấu là Dự án trọng điểm!" : "Đã bỏ đánh dấu Dự án trọng điểm.", true);
            } else {
                // Revert lại trạng thái nếu server từ chối hoặc có lỗi nghiệp vụ
                $chk.prop('checked', !isChecked);
                applyKeyProjectUI(!isChecked);
                executeResponseMessage(res ? res.message : "Thao tác không thành công!", null, false);
            }
        },
        error: function () {
            _isKeyProjectToggling = false;
            // Revert lại trạng thái nếu lỗi kết nối
            $chk.prop('checked', !isChecked);
            applyKeyProjectUI(!isChecked);
            executeResponseMessage("Lỗi kết nối máy chủ, vui lòng thử lại!", null, false);
        }
    });
}

var _isFollowToggling = false;
function toggleFollowSales(salesId, isChecked) {
    var $chk = $('#chkIsFollowed');
    if (_isFollowToggling) return;
    _isFollowToggling = true;

    // 1. Phản hồi giao diện tức thì 0ms (Optimistic UI)
    applyFollowSalesUI(isChecked);

    // 2. Chạy ngầm dưới background
    $.ajax({
        url: _detailUrls.toggleFollow,
        type: "POST",
        data: { id: salesId, isFollowed: isChecked },
        dataType: "JSON",
        global: false,
        success: function (res) {
            _isFollowToggling = false;
            if (res && res.status) {
                executeResponseMessage(res.message, isChecked ? "Đã lưu vào danh sách quan tâm!" : "Đã bỏ quan tâm dự án.", true);
            } else {
                $chk.prop('checked', !isChecked);
                applyFollowSalesUI(!isChecked);
                executeResponseMessage(res ? res.message : "Thao tác không thành công!", null, false);
            }
        },
        error: function () {
            _isFollowToggling = false;
            $chk.prop('checked', !isChecked);
            applyFollowSalesUI(!isChecked);
            executeResponseMessage("Lỗi kết nối máy chủ, vui lòng thử lại!", null, false);
        }
    });
}

/* ================= 5. Trao đổi chung & Hoạt động (Discussion & Collaboration Feed) ================= */
function reloadDiscussionsSection(salesId, filterType) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    var $discussions = $("#tab-discussions");
    showSectionLoading($discussions);

    var params = { id: salesId };
    if (filterType !== undefined && filterType !== null && filterType !== "") {
        params.activityType = filterType;
    }

    // Cleanly destroy CKEditor before re-rendering HTML
    if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
        try {
            CKEDITOR.instances['txtDiscussionContent'].destroy(true);
        } catch (e) { }
    }

    $.get(_detailUrls.getDiscussionsPartial, params, function (html) {
        $discussions.html(html);
        hideSectionLoading($discussions);
        var newCount = $discussions.find("#partialDiscussionsCount").data("count");
        if (newCount !== undefined) {
            $("#badgeTabDiscussions").text(newCount);
        }
        initDiscussionCKEditor();
        initDiscussionEvents();
    }).fail(function () {
        hideSectionLoading($discussions);
    });
}

function filterDiscussions(salesId, filterType) {
    reloadDiscussionsSection(salesId, filterType);
}

var _discussionSelectedFiles = [];

function handleDiscussionFileSelect(input) {
    if (!input || !input.files || input.files.length === 0) return;
    for (var i = 0; i < input.files.length; i++) {
        _discussionSelectedFiles.push(input.files[i]);
    }
    renderDiscussionSelectedFiles();
    input.value = "";
}

function renderDiscussionSelectedFiles() {
    var $container = $("#discussionSelectedFilesContainer");
    var $list = $("#discussionSelectedFilesList");
    var $count = $("#discussionSelectedFilesCount");

    if (!_discussionSelectedFiles || _discussionSelectedFiles.length === 0) {
        $container.addClass("d-none");
        $list.empty();
        $count.text("0");
        return;
    }

    $container.removeClass("d-none");
    $count.text(_discussionSelectedFiles.length);
    $list.empty();

    _discussionSelectedFiles.forEach(function (file, index) {
        var sizeText = file.size > 1048576 
            ? (file.size / 1048576).toFixed(1) + " MB" 
            : (file.size / 1024).toFixed(0) + " KB";
        var fileName = file.name || "";
        var ext = fileName.lastIndexOf('.') >= 0 ? fileName.substring(fileName.lastIndexOf('.')).toLowerCase() : "";
        var iconClass = "fa-file text-secondary";
        if ([".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"].indexOf(ext) >= 0) {
            iconClass = "fa-file-image text-info";
        } else if (ext === ".pdf") {
            iconClass = "fa-file-pdf text-danger";
        } else if (ext === ".doc" || ext === ".docx") {
            iconClass = "fa-file-word text-primary";
        } else if (ext === ".xls" || ext === ".xlsx" || ext === ".csv") {
            iconClass = "fa-file-excel text-success";
        } else if (ext === ".ppt" || ext === ".pptx") {
            iconClass = "fa-file-powerpoint text-warning";
        } else if (ext === ".zip" || ext === ".rar" || ext === ".7z") {
            iconClass = "fa-file-archive text-warning";
        }

        var $chip = $('<div class="ds-file-tag">' +
            '<i class="fa ' + iconClass + ' mr-1"></i>' +
            '<span class="text-truncate" style="max-width: 220px;" title="' + fileName + '">' + fileName + ' (' + sizeText + ')</span>' +
            '<i class="fa fa-times text-danger ml-1" title="Bỏ tệp này" onclick="removeDiscussionSelectedFile(' + index + ');"></i>' +
            '</div>');
        $list.append($chip);
    });
}

function removeDiscussionSelectedFile(index) {
    if (index >= 0 && index < _discussionSelectedFiles.length) {
        _discussionSelectedFiles.splice(index, 1);
        renderDiscussionSelectedFiles();
    }
}

var _projectMembersCache = null;

function loadProjectMembersForMention(salesId, callback) {
    if (_projectMembersCache && _projectMembersCache.salesId === salesId) {
        if (callback) callback(_projectMembersCache.data);
        return;
    }
    $.get(_detailUrls.getMembersForMention, { id: salesId }, function (res) {
        if (res && res.status && res.data) {
            _projectMembersCache = { salesId: salesId, data: res.data };
            if (callback) callback(res.data);
        }
    });
}

function initDiscussionCKEditor() {
    if (typeof CKEDITOR === "undefined") return;
    if ($("#txtDiscussionContent").length === 0) return;

    if (CKEDITOR.instances['txtDiscussionContent']) {
        try {
            CKEDITOR.instances['txtDiscussionContent'].destroy(true);
        } catch (e) { }
    }

    window.CKEDITOR_BASEPATH = "/Contents/Modules/Major/ckeditor4/";

    var editor = CKEDITOR.replace('txtDiscussionContent', {
        customConfig: '',
        height: 120,
        allowedContent: true,
        extraAllowedContent: 'span(*)[*]; img[*]; table[*]; tr[*]; td[*]; th[*]; p[*]; a[*]; b[*]; strong[*]; i[*]; u[*]; s[*]',
        autoParagraph: true,
        enterMode: 1, // CKEDITOR.ENTER_P
        shiftEnterMode: 2, // CKEDITOR.ENTER_BR
        entities: false,
        basicEntities: false,
        entities_latin: false,
        entities_greek: false,
        entities_processNumerical: false,
        fillEmptyBlocks: false,
        toolbar: [
            { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike'] },
            { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Blockquote'] },
            { name: 'insert', items: ['Table', 'Link', 'Unlink'] },
            { name: 'styles', items: ['Format'] },
            { name: 'tools', items: ['Maximize', 'RemoveFormat'] }
        ]
    });

    editor.on('instanceReady', function () {
        updateDiscussionWordCount();

        editor.on('change', function () {
            updateDiscussionWordCount();
        });

        editor.on('paste', function () {
            setTimeout(updateDiscussionWordCount, 100);
        });

        editor.on('key', function (evt) {
            setTimeout(updateDiscussionWordCount, 50);

            // Ctrl + Enter to submit form
            if (evt.data.domEvent.$.ctrlKey && (evt.data.domEvent.$.keyCode === 13 || evt.data.domEvent.$.which === 13)) {
                evt.cancel();
                $("#frmPostDiscussion").submit();
                return;
            }

            // '@' key to trigger mention
            var key = evt.data.domEvent.$.key;
            if (key === '@' || (evt.data.domEvent.$.shiftKey && (evt.data.domEvent.$.keyCode === 50 || evt.data.domEvent.$.which === 50))) {
                if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
                    var ed = CKEDITOR.instances['txtDiscussionContent'];
                    var sel = ed.getSelection();
                    if (sel) {
                        try {
                            window._mentionBookmarks = sel.createBookmarks(true);
                        } catch (e) { }
                    }
                }
                setTimeout(function () {
                    showMentionDropdown("");
                }, 100);
            }
        });
    });
}

function triggerMentionDropdown() {
    showMentionDropdown("");
}

function showMentionDropdown(query) {
    var salesId = getEffectiveSalesId();
    loadProjectMembersForMention(salesId, function (members) {
        var $dropdown = $("#dsMentionDropdown");
        var $list = $("#dsMentionList");
        $list.empty();

        var filtered = members;
        if (query) {
            var q = query.toLowerCase();
            filtered = members.filter(function (m) {
                return (m.fullName && m.fullName.toLowerCase().indexOf(q) !== -1) ||
                       (m.userName && m.userName.toLowerCase().indexOf(q) !== -1);
            });
        }

        if (filtered.length === 0) {
            $list.html('<div class="p-2 text-muted text-80 text-center">Không tìm thấy nhân sự phù hợp</div>');
        } else {
            filtered.forEach(function (m, idx) {
                var $item = $('<div class="ds-mention-item' + (idx === 0 ? ' active' : '') + '">' +
                    '<div class="w-3 h-3 radius-round bgc-primary-l3 text-primary d-flex align-items-center justify-content-center mr-2 font-bold text-80" style="width: 26px; height: 26px; border-radius: 50%;">' +
                    (m.fullName ? m.fullName.charAt(0).toUpperCase() : 'U') +
                    '</div>' +
                    '<div class="min-width-0 flex-grow-1">' +
                    '<div class="font-weight-bold text-85 text-dark text-truncate">' + m.fullName + '</div>' +
                    '<div class="text-75 text-secondary text-truncate">' + (m.roleTitle || m.userName) + '</div>' +
                    '</div>' +
                    '</div>');

                $item.data('user', m);
                $item.on('mousedown', function (e) {
                    e.preventDefault();
                    selectMentionUser(m);
                }).on('click', function (e) {
                    e.preventDefault();
                    selectMentionUser(m);
                });
                $list.append($item);
            });
        }

        $dropdown.show();
        setTimeout(function () {
            $("#txtMentionSearch").focus();
        }, 50);
    });
}

function hideMentionDropdown() {
    $("#dsMentionDropdown").hide();
    $("#txtMentionSearch").val('');
}

function handleMentionSearchInput(val) {
    showMentionDropdown(val);
}

function handleMentionSearchKeydown(e) {
    var $dropdown = $("#dsMentionDropdown");
    if (!$dropdown.is(":visible")) return;

    var $items = $("#dsMentionList .ds-mention-item");
    if ($items.length > 0) {
        var $current = $items.filter(".active");
        var currentIndex = $items.index($current);

        if (e.key === "ArrowDown") {
            e.preventDefault();
            var nextIndex = currentIndex < $items.length - 1 ? currentIndex + 1 : 0;
            $items.removeClass("active");
            var $next = $items.eq(nextIndex).addClass("active");
            if ($next.length && $next[0].scrollIntoView) {
                $next[0].scrollIntoView({ block: "nearest" });
            }
            return;
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            var prevIndex = currentIndex > 0 ? currentIndex - 1 : $items.length - 1;
            $items.removeClass("active");
            var $prev = $items.eq(prevIndex).addClass("active");
            if ($prev.length && $prev[0].scrollIntoView) {
                $prev[0].scrollIntoView({ block: "nearest" });
            }
            return;
        } else if (e.key === "Enter") {
            e.preventDefault();
            if ($current.length > 0) {
                var user = $current.data("user");
                if (user) {
                    selectMentionUser(user);
                    return;
                }
            }
        }
    }

    if (e.key === "Escape") {
        e.preventDefault();
        hideMentionDropdown();
        if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
            CKEDITOR.instances['txtDiscussionContent'].focus();
        }
    }
}

function selectMentionUser(user) {
    if (!user) return;

    if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
        var editor = CKEDITOR.instances['txtDiscussionContent'];
        editor.focus();

        if (window._mentionBookmarks) {
            try {
                editor.getSelection().selectBookmarks(window._mentionBookmarks);
                window._mentionBookmarks = null;
            } catch (e) { }
        }

        // 1. Tự động xóa ký tự '@' người dùng đã gõ trước đó để kích hoạt dropdown
        try {
            var selection = editor.getSelection();
            if (selection) {
                var ranges = selection.getRanges();
                if (ranges && ranges.length > 0) {
                    var range = ranges[0];
                    var startNode = range.startContainer;
                    if (startNode && startNode.type === CKEDITOR.NODE_TEXT) {
                        var textVal = startNode.getText();
                        var offset = range.startOffset;
                        if (offset > 0 && textVal.charAt(offset - 1) === '@') {
                            var updatedText = textVal.substring(0, offset - 1) + textVal.substring(offset);
                            startNode.setText(updatedText);
                            range.setStart(startNode, offset - 1);
                            range.setEnd(startNode, offset - 1);
                            selection.selectRanges([range]);
                        }
                    }
                }
            }
        } catch (err) {
            console.error("Error removing @ before mention:", err);
        }

        // 2. Chèn tag hiển thị sạch, không dư thừa ký tự/icon @ vì bản thân badge đã là tag hiển thị
        var mentionHtml = '<span class="ds-mention-badge" data-user-id="' + user.userId + '">' + user.fullName + '</span>&nbsp;';
        editor.insertHtml(mentionHtml);
    } else {
        var $textarea = $("#txtDiscussionContent");
        if ($textarea.length > 0) {
            var val = $textarea.val() || "";
            // Xóa ký tự '@' cuối cùng nếu vừa gõ
            if (val.trimEnd().endsWith('@')) {
                var lastAt = val.lastIndexOf('@');
                val = val.substring(0, lastAt);
            }
            $textarea.val(val + " " + user.fullName + " ");
        }
    }

    // Track mentioned user IDs
    var $ids = $("#hdnMentionedUserIds");
    var $names = $("#hdnMentionedNames");
    var currentIds = $ids.val() ? $ids.val().split(",") : [];
    var currentNames = $names.val() ? $names.val().split(",") : [];

    if (currentIds.indexOf(user.userId.toString()) === -1) {
        currentIds.push(user.userId);
        currentNames.push(user.fullName);
    }
    $ids.val(currentIds.join(","));
    $names.val(currentNames.join(","));

    hideMentionDropdown();
}

function getDiscussionPlainText() {
    var content = "";
    if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
        content = CKEDITOR.instances['txtDiscussionContent'].getData();
    } else {
        content = $("#txtDiscussionContent").val() || "";
    }
    return $("<div>").html(content).text().trim();
}

function countWordsInText(text) {
    if (!text) return 0;
    var trimmed = text.trim();
    if (!trimmed) return 0;
    var words = trimmed.split(/\s+/).filter(function (w) { return w.length > 0; });
    return words.length;
}

function updateDiscussionWordCount() {
    var plainText = getDiscussionPlainText();
    var wordCount = countWordsInText(plainText);
    var $badge = $("#discussionWordCountBadge");
    var $countSpan = $("#discussionCurrentWords");

    if ($countSpan.length > 0) {
        $countSpan.text(wordCount);
    }

    if ($badge.length > 0) {
        if (wordCount > 500) {
            $badge.removeClass("bgc-grey-l4 text-secondary-d1 brc-grey-l2 font-normal")
                  .addClass("bgc-danger-l3 text-danger-d2 brc-danger-m2 font-bold");
        } else {
            $badge.removeClass("bgc-danger-l3 text-danger-d2 brc-danger-m2 font-bold")
                  .addClass("bgc-grey-l4 text-secondary-d1 brc-grey-l2 font-normal");
        }
    }
    return wordCount;
}

function initDiscussionEvents() {
    $(document).off("click.dsMention").on("click.dsMention", function (e) {
        if (!$(e.target).closest("#dsMentionDropdown, #txtDiscussionContent, .ds-mention-item, button[onclick*='triggerMentionDropdown']").length) {
            hideMentionDropdown();
        }
    });

    $(document).off("input.dsWordCount propertychange.dsWordCount paste.dsWordCount", "#txtDiscussionContent")
               .on("input.dsWordCount propertychange.dsWordCount paste.dsWordCount", "#txtDiscussionContent", function () {
        updateDiscussionWordCount();
    });
}

var _isSubmittingDiscussion = false;
function submitDiscussionForm(e, salesId) {
    if (e && e.preventDefault) e.preventDefault();
    if (_isSubmittingDiscussion) return;

    var content = "";
    if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
        content = CKEDITOR.instances['txtDiscussionContent'].getData();
    } else {
        content = $("#txtDiscussionContent").val();
    }

    // Làm sạch hoàn toàn ký tự @ đứng trước hoặc trong thẻ tag mention trước khi gửi
    if (content) {
        content = content.replace(/@(?:\s|&nbsp;|<[^>]+>)*(<span\s+class=["']ds-mention-badge["'])/gi, '$1');
        content = content.replace(/(<span\s+class=["']ds-mention-badge["'][^>]*>)(?:\s|&nbsp;)*@(?:\s|&nbsp;)*/gi, '$1');
        content = content.replace(/<i\s+class=["']fa\s+fa-at[^'"]*["']\s*><\/i>/gi, '');
    }

    var plainText = $("<div>").html(content).text().trim();
    if (!content || !plainText) {
        if (typeof toastr !== "undefined") {
            toastr.warning("Vui lòng nhập nội dung trao đổi!");
        }
        if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
            CKEDITOR.instances['txtDiscussionContent'].focus();
        } else {
            $("#txtDiscussionContent").focus();
        }
        return;
    }

    var wordCount = countWordsInText(plainText);
    if (wordCount > 500) {
        if (typeof toastr !== "undefined") {
            toastr.warning("Nội dung trao đổi không được vượt quá 500 từ (hiện tại: " + wordCount + " từ)!");
        }
        if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
            CKEDITOR.instances['txtDiscussionContent'].focus();
        } else {
            $("#txtDiscussionContent").focus();
        }
        return;
    }

    _isSubmittingDiscussion = true;
    var $btn = $("#btnSubmitDiscussion");
    var origHtml = $btn.html();
    $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang gửi...');

    var formData = new FormData();
    formData.append("digitalSalesId", salesId);
    formData.append("content", content.trim());
    formData.append("mentionedUserIds", $("#hdnMentionedUserIds").val());
    formData.append("mentionedNames", $("#hdnMentionedNames").val());

    if (_discussionSelectedFiles && _discussionSelectedFiles.length > 0) {
        for (var i = 0; i < _discussionSelectedFiles.length; i++) {
            formData.append("files", _discussionSelectedFiles[i]);
        }
    }

    $.ajax({
        url: _detailUrls.postDiscussion,
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        dataType: "JSON",
        success: function (res) {
            _isSubmittingDiscussion = false;
            $btn.prop("disabled", false).html(origHtml);

            if (res && res.status) {
                executeResponseMessage(res.message, "Đã gửi trao đổi thành công!", true);
                if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances['txtDiscussionContent']) {
                    CKEDITOR.instances['txtDiscussionContent'].setData('');
                }
                $("#txtDiscussionContent").val('');
                $("#hdnMentionedUserIds").val('');
                $("#hdnMentionedNames").val('');
                updateDiscussionWordCount();
                _discussionSelectedFiles = [];
                renderDiscussionSelectedFiles();
                reloadDiscussionsSection(salesId);
            } else {
                executeResponseMessage(res ? res.message : "Gửi trao đổi không thành công!", null, false);
            }
        },
        error: function () {
            _isSubmittingDiscussion = false;
            $btn.prop("disabled", false).html(origHtml);
            executeResponseMessage("Lỗi kết nối máy chủ, vui lòng thử lại!", null, false);
        }
    });
}

function deleteDiscussionItem(activityId, salesId) {
    if (!activityId) return;

    var $modal = $('<div class="modal fade" tabindex="-1" role="dialog">' +
        '<div class="modal-dialog modal-dialog-centered" role="document" style="max-width: 420px;">' +
        '<div class="modal-content border-0 shadow-lg radius-2">' +
        '<div class="modal-body text-center p-4">' +
        '<div class="w-5 h-5 radius-round bgc-danger-l3 text-danger d-inline-flex align-items-center justify-content-center mb-3" style="width: 48px; height: 48px; border-radius: 50%;">' +
        '<i class="fa fa-trash-alt fa-2x"></i>' +
        '</div>' +
        '<h5 class="text-dark font-weight-bold mb-2">Xác nhận xóa trao đổi</h5>' +
        '<p class="text-secondary text-90 mb-3">Bạn có chắc chắn muốn xóa bài trao đổi này không? Thao tác này không thể hoàn tác.</p>' +
        '<div class="d-flex justify-content-center" style="gap: 8px;">' +
        '<button type="button" class="btn btn-sm btn-light border-1 brc-grey-l1 px-3" data-dismiss="modal">Hủy bỏ</button>' +
        '<button type="button" class="btn btn-sm btn-danger px-3 font-bold" id="btnConfirmDeleteDiscussion">Đồng ý xóa</button>' +
        '</div>' +
        '</div>' +
        '</div>' +
        '</div>' +
        '</div>');

    $modal.on('hidden.bs.modal', function () {
        $(this).remove();
    });

    $modal.find('#btnConfirmDeleteDiscussion').on('click', function () {
        var $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang xóa...');

        $.ajax({
            url: _detailUrls.deleteDiscussion,
            type: "POST",
            data: { activityId: activityId },
            dataType: "JSON",
            success: function (res) {
                $modal.modal('hide');
                if (res && res.status) {
                    executeResponseMessage(res.message, "Đã xóa trao đổi thành công!", true);
                    reloadDiscussionsSection(salesId);
                } else {
                    executeResponseMessage(res ? res.message : "Xóa trao đổi thất bại!", null, false);
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('Đồng ý xóa');
                executeResponseMessage("Lỗi kết nối máy chủ!", null, false);
            }
        });
    });

    $modal.modal('show');
}

/* ==========================================================================
   STATUS TIMELINE & TIMELINE DETAIL MODAL
   ========================================================================== */
function openStatusTimelineModal(salesId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.statusTimelineModal, { digitalSalesId: salesId }, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalStatusTimelineContainer").remove();
        $("body").append('<div id="modalStatusTimelineContainer">' + html + '</div>');
        var $modal = $("#modalStatusTimeline");

        $modal.on("show.bs.modal", function () {
            setTimeout(function () {
                $('.modal-backdrop:last').addClass('timeline-backdrop').css('z-index', 1050);
            }, 0);
        });

        $modal.on("hidden.bs.modal", function () {
            $("#modalStatusTimelineContainer").remove();
            if ($('.modal.show').length > 0) {
                $("body").addClass("modal-open");
            }
        });

        $modal.modal("show");
    }).fail(function () {
        if (typeof _endWaiting === "function") _endWaiting();
        executeResponseMessage("Lỗi kết nối máy chủ khi tải Timeline!", "Lỗi", false);
    });
}

function openStatusTimelineDetailModal(salesId, timelineId) {
    salesId = getEffectiveSalesId(salesId);
    if (!salesId) return;
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.statusTimelineDetailModal, { digitalSalesId: salesId, timelineId: timelineId }, function (html) {
        if (typeof _endWaiting === "function") _endWaiting();
        $("#modalStatusTimelineDetailContainer").remove();
        $("body").append('<div id="modalStatusTimelineDetailContainer">' + html + '</div>');
        var $modal = $("#modalStatusTimelineDetail");

        $modal.on("show.bs.modal", function () {
            setTimeout(function () {
                $('.modal-backdrop:last').addClass('timeline-detail-backdrop').css('z-index', 1065);
            }, 0);
        });

        $modal.on("hidden.bs.modal", function () {
            $("#modalStatusTimelineDetailContainer").remove();
            if ($('.modal.show').length > 0) {
                $("body").addClass("modal-open");
            }
        });

        $modal.modal("show");
    }).fail(function () {
        if (typeof _endWaiting === "function") _endWaiting();
        executeResponseMessage("Lỗi kết nối máy chủ khi tải chi tiết Trạng thái!", "Lỗi", false);
    });
}

function toggleTimelineAttachmentFiles(timelineId) {
    var $el = $("#timelineFiles_" + timelineId);
    if ($el.length) {
        $el.toggleClass("show");
    }
}
