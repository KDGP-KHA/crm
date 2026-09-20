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

// Tour dùng chung: cấu hình bước có thể tái sử dụng cho các màn hình khác.
// Tour dùng chung: cấu hình bước có thể tái sử dụng cho các màn hình khác.
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
        if (markViewed && window._digitalSalesGuide && screenCode) {
            $.post(_digitalSalesGuide.markViewedUrl, { screenCode: screenCode });
        }
    }
    return { start: start, close: close, isOpen: function () { return isOpen; } };
}());

$(function () {
    var defaultPageTour = {
        code: "Cate.DigitalSales.Detail",
        steps: [
            { selector: ".ds-header-title-container", title: "Thông tin hồ sơ", text: "Hiển thị mã hồ sơ, tên khách hàng, mã số thuế, loại dịch vụ và nhân viên phụ trách chính của hồ sơ chuyển đổi số." },
            { selector: ".ds-header-action-bar", title: "Thao tác hồ sơ", text: "Các nút chức năng: Chuyển trạng thái quy trình, Chỉnh sửa thông tin hồ sơ, Làm mới dữ liệu và Mở hướng dẫn sử dụng." },
            { selector: "#containerMetrics", title: "Chỉ số tổng quan", text: "Thẻ chỉ số tổng quan gồm: Tổng giá trị hợp đồng, Số sản phẩm dịch vụ, Tiến độ thực hiện nhiệm vụ và Số lượng trao đổi/hoạt động." },
            { selector: ".nav-tabs", title: "Các khu vực thông tin", text: "Thanh điều hướng chuyển đổi giữa 5 tab: Thông tin tổng quan, Sản phẩm & Doanh thu, Tiến trình & Checklist, Trao đổi và Lịch sử rà soát." }
        ]
    };
    var defaultTabTours = {
        "#tab-overview": {
            code: "Cate.DigitalSales.Detail.Overview",
            steps: [
                { selector: "#tab-overview .col-md-6:first-child .card", title: "Thông tin hồ sơ", text: "Chi tiết các thông tin pháp lý của khách hàng, người liên hệ, cơ hội kinh doanh và nguồn gốc hồ sơ." },
                { selector: "#tab-overview .col-md-6:nth-child(2) .card", title: "Phân công và quản lý", text: "Thông tin nhân sự phụ trách AM, cán bộ hỗ trợ giải pháp, quy trình và tiến trình hiện tại của hồ sơ." },
                { selector: "#sectionMembers", title: "Thành viên tham gia", text: "Danh sách các cán bộ, chuyên viên thuộc đội ngũ phụ trách hồ sơ, hỗ trợ thêm/xóa thành viên tham gia." },
                { selector: "#tab-overview .ds-html-note-view", title: "Mô tả nhu cầu", text: "Nội dung chi tiết về nhu cầu chuyển đổi số của khách hàng, phạm vi yêu cầu và ghi chú quan trọng." },
                { selector: "#sectionAttachments", title: "Tệp đính kèm", text: "Khu vực lưu trữ hồ sơ, tài liệu đề xuất, hợp đồng scan và biên bản liên quan đến chuyển đổi số." }
            ]
        },
        "#tab-products": {
            code: "Cate.DigitalSales.Detail.Products",
            steps: [
                { selector: "#tab-products h6", title: "Danh sách sản phẩm", text: "Thống kê tổng số lượng sản phẩm/dịch vụ số và tổng giá trị hợp đồng của hồ sơ." },
                { selector: "#tab-products .btn-purple", title: "Thêm sản phẩm", text: "Nhấn nút này để thêm mới sản phẩm/dịch vụ số, cấu hình số lượng, đơn giá và giá trị hợp đồng." },
                { selector: "#tab-products .card.bcard", title: "Thông tin sản phẩm và hợp đồng", text: "Danh sách các dịch vụ số đang tư vấn, trạng thái hợp đồng, ngày bắt đầu và kết thúc sử dụng." }
            ]
        },
        "#tab-tracking": {
            code: "Cate.DigitalSales.Detail.Tracking",
            steps: [
                { selector: "#tab-tracking > .d-flex", title: "Tổng quan checklist", text: "Thanh tiến độ tổng thể phản ánh tỷ lệ hoàn thành các nhiệm vụ và tiến trình theo quy trình chuẩn." },
                { selector: "#tblTracking", title: "Danh sách tiến trình", text: "Bảng phân rã từng bước triển khai, người thực hiện, thời hạn hoàn thành và kết quả thực hiện." },
                { selector: "#treeTrackingBody .tree-status-row", title: "Trạng thái và quy trình", text: "Cây phân cấp các giai đoạn quy trình bán hàng giải pháp số và các tiến trình tương ứng." }
            ]
        },
        "#tab-discussions": {
            code: "Cate.DigitalSales.Detail.Discussions",
            steps: [
                { selector: "#tab-discussions .ds-composer-card", title: "Tạo trao đổi", text: "Khung soạn thảo phản hồi, trao đổi nhanh giữa các thành viên phụ trách hồ sơ chuyển đổi số." },
                { selector: "#frmPostDiscussion", title: "Nội dung và tệp đính kèm", text: "Nhập nội dung thảo luận, hỗ trợ định dạng văn bản và đính kèm tài liệu trao đổi." },
                { selector: "#tab-discussions .ds-activity-timeline", title: "Lịch sử hoạt động", text: "Dòng thời gian ghi nhận toàn bộ lịch sử cập nhật, trao đổi, thay đổi trạng thái của hồ sơ." }
            ]
        },
        "#tab-review-history": {
            code: "Cate.DigitalSales.Detail.ReviewHistory",
            steps: [
                { selector: "#tab-review-history .review-history-timeline", title: "Lịch sử rà soát", text: "Trục thời gian ghi nhận các đợt rà soát định kỳ đánh giá tính khả thi và tiến độ hồ sơ." },
                { selector: "#tab-review-history .review-history-batch", title: "Đợt rà soát", text: "Thông tin đợt rà soát, người thực hiện rà soát và thời điểm thực hiện." },
                { selector: "#tab-review-history .review-history-entry", title: "Chi tiết kết quả rà soát", text: "Kết quả đánh giá hồ sơ trong đợt rà soát: đạt, cần khắc phục hoặc các ghi chú cảnh báo rủi ro." }
            ]
        }
    };

    function resolveTour(tourConfig) {
        if (!tourConfig) return null;
        var screenCode = tourConfig.code;
        var configuredSteps = window._digitalSalesGuide && _digitalSalesGuide.steps && _digitalSalesGuide.steps[screenCode];
        if (configuredSteps && Array.isArray(configuredSteps) && configuredSteps.length > 0) {
            var activeSteps = configuredSteps.filter(function (s) { return s.isActive !== false; });
            if (activeSteps.length > 0) {
                return {
                    code: screenCode,
                    steps: activeSteps.map(function (s) {
                        return { selector: s.selector, title: s.title, text: s.text };
                    })
                };
            }
        }
        return tourConfig;
    }

    function getActiveTabTour() {
        var activeHref = $(".nav-tabs a.nav-link.active").attr("href");
        var tabTour = defaultTabTours[activeHref];
        return resolveTour(tabTour) || null;
    }

    function openTour(tour, markViewed) {
        var finalTour = resolveTour(tour);
        if (finalTour) DigitalSalesGuide.start(finalTour.steps, markViewed, finalTour.code);
    }

    function autoStartTabTour(tabTour) {
        if (!tabTour || DigitalSalesGuide.isOpen() || !window._digitalSalesGuide) return;
        var resolved = resolveTour(tabTour);
        if (!resolved) return;
        $.getJSON(_digitalSalesGuide.getStateUrl, { screenCode: resolved.code }).done(function (response) {
            if (response && response.status && !response.isViewed && !DigitalSalesGuide.isOpen()) openTour(resolved, true);
        });
    }

    $("#btnDigitalSalesGuide").on("click", function () {
        openTour(getActiveTabTour() || defaultPageTour, false);
    });

    $(".nav-tabs a[data-toggle='tab']").on("shown.bs.tab", function (event) {
        autoStartTabTour(defaultTabTours[$(event.target).attr("href")]);
    });

    if (window._digitalSalesGuide && _digitalSalesGuide.autoStart) {
        window.setTimeout(function () { openTour(defaultPageTour, true); }, 650);
    }
});

// QUẢN LÝ CẤU HÌNH HƯỚNG DẪN SỬ DỤNG TRÊN WEBSITE
function openGuideConfigModal(screenCode) {
    if (!window._digitalSalesGuide || !_digitalSalesGuide.getConfigUrl) return;
    if (!screenCode) {
        var activeHref = $(".nav-tabs a.nav-link.active").attr("href");
        var tabMap = {
            "#tab-overview": "Cate.DigitalSales.Detail.Overview",
            "#tab-products": "Cate.DigitalSales.Detail.Products",
            "#tab-tracking": "Cate.DigitalSales.Detail.Tracking",
            "#tab-discussions": "Cate.DigitalSales.Detail.Discussions",
            "#tab-review-history": "Cate.DigitalSales.Detail.ReviewHistory"
        };
        screenCode = tabMap[activeHref] || "Cate.DigitalSales.Detail";
    }

    var url = _digitalSalesGuide.getConfigUrl + "?screenCode=" + encodeURIComponent(screenCode);
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
    $modal.on("click", ".guide-btn-up", function () {
        var $row = $(this).closest(".guide-step-row");
        var $prev = $row.prev(".guide-step-row");
        if ($prev.length) {
            $row.insertBefore($prev);
            renumberTbody($row.closest(".guide-steps-tbody"));
        }
    });

    // Di chuyển Xuống
    $modal.on("click", ".guide-btn-down", function () {
        var $row = $(this).closest(".guide-step-row");
        var $next = $row.next(".guide-step-row");
        if ($next.length) {
            $row.insertAfter($next);
            renumberTbody($row.closest(".guide-steps-tbody"));
        }
    });

    // Xóa bước
    $modal.on("click", ".guide-btn-delete", function () {
        var $row = $(this).closest(".guide-step-row");
        var $tbody = $row.closest(".guide-steps-tbody");
        $row.remove();
        if ($tbody.find(".guide-step-row").length === 0) {
            $tbody.html('<tr class="guide-empty-row"><td colspan="6" class="text-center py-4 text-secondary-m2"><i class="fa fa-info-circle text-140 mb-1 opacity-50"></i><p class="mb-0 text-90">Chưa có bước hướng dẫn nào cho khu vực này.</p></td></tr>');
        }
        renumberTbody($tbody);
    });

    // Thêm bước mới
    $modal.on("click", ".guide-btn-add", function () {
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
    $modal.on("click", ".guide-btn-reset", function () {
        var screenCode = $(this).data("screencode");
        if (!window._digitalSalesGuide || !_digitalSalesGuide.resetConfigUrl) return;
        $.post(_digitalSalesGuide.resetConfigUrl, { screenCode: screenCode }, function (response) {
            if (response && response.status) {
                if (!_digitalSalesGuide.steps) _digitalSalesGuide.steps = {};
                _digitalSalesGuide.steps[screenCode] = response.steps || [];
                executeResponseMessage(response.message);

                // Nạp lại modal để thấy danh sách đã reset
                var $activeTab = $modal.find("#guideConfigTabs .nav-link.active");
                var activeCode = $activeTab.data("screencode") || screenCode;
                openGuideConfigModal(activeCode);
            } else {
                executeResponseMessage(response ? response.message : null);
            }
        });
    });

    // Xem thử hướng dẫn
    $modal.on("click", ".guide-btn-preview", function () {
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

    // Lưu cấu hình (Save)
    $modal.on("click", ".guide-save-modal-btn", function () {
        var $activePane = $modal.find(".guide-pane.active");
        var screenCode = $activePane.data("screencode");
        if (!screenCode || !window._digitalSalesGuide || !_digitalSalesGuide.saveConfigUrl) return;

        var steps = [];
        $activePane.find(".guide-step-row").each(function (idx) {
            var $row = $(this);
            var title = $row.find(".guide-input-title").val();
            var text = $row.find(".guide-input-text").val();
            var selector = $row.find(".guide-input-selector").val();
            if (title || text) {
                steps.push({
                    GuideConfigID: parseInt($row.data("step-id")) || 0,
                    ScreenCode: screenCode,
                    StepOrder: idx + 1,
                    Selector: selector || "",
                    Title: title || "",
                    GuideText: text || "",
                    IsActive: $row.find(".guide-input-active").is(":checked")
                });
            }
        });

        var payload = {
            ScreenCode: screenCode,
            Steps: steps
        };

        $.ajax({
            url: _digitalSalesGuide.saveConfigUrl,
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(payload),
            success: function (response) {
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
                executeResponseMessage(null, "Lỗi kết nối khi lưu cấu hình", false);
            }
        });
    });
}


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
        if (message.indexOf("$.aceToaster") !== -1 || message.indexOf("toastr") !== -1 || message.indexOf("eval") !== -1 || message.indexOf("showNotify") !== -1) {
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
        reloadAttachmentsSection(salesId);
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

// Hàm lọc tệp đính kèm theo loại nguồn (Tất cả / Hồ sơ / Checklist / Chuyển trạng thái)
function filterAttachmentCards(type, btn) {
    var $grid = $('#attachGridList');
    if ($grid.length === 0) return;

    if (btn) {
        $('#attachmentFilterContainer .attach-filter-btn').removeClass('active');
        $(btn).addClass('active');
    }

    if (type === 'all') {
        $grid.find('.attach-col-8').show();
    } else {
        $grid.find('.attach-col-8').hide();
        $grid.find('.attach-col-8[data-source="' + type + '"]').fadeIn(150);
    }
}

// Hàm chuyển sang tab Checklist và highlight tiến trình tương ứng
function switchToTrackingTab(trackingId) {
    var $tabLink = $('#tab-tracking-link');
    if ($tabLink.length > 0) {
        $tabLink.tab('show');
    }

    if (trackingId) {
        setTimeout(function () {
            var $target = $('tr[data-node*="_task_' + trackingId + '"], #chk_task_' + trackingId + ', #chk_todo_' + trackingId).closest('tr');
            if ($target.length > 0) {
                // Mở rộng cây cha nếu đang ẩn
                var parentNode = $target.attr('data-parent');
                if (parentNode) {
                    $('tr[data-node="' + parentNode + '"]').each(function () {
                        if ($(this).attr('data-expanded') === 'false') {
                            toggleTreeNode(parentNode);
                        }
                    });
                }
                var rootNode = $target.attr('data-root');
                if (rootNode) {
                    $('tr[data-node="' + rootNode + '"]').each(function () {
                        if ($(this).attr('data-expanded') === 'false') {
                            toggleTreeNode(rootNode);
                        }
                    });
                }

                $target.show();
                if ($target[0] && typeof $target[0].scrollIntoView === 'function') {
                    $target[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                $target.addClass('highlight-tracking-target');
                setTimeout(function () {
                    $target.removeClass('highlight-tracking-target');
                }, 3000);
            }
        }, 300);
    }
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

        // Keyboard navigation and shortcuts
        editor.on('key', function (evt) {
            setTimeout(updateDiscussionWordCount, 50);

            // Ctrl + Enter to submit form
            if (evt.data.domEvent.$.ctrlKey && (evt.data.domEvent.$.keyCode === 13 || evt.data.domEvent.$.which === 13)) {
                evt.cancel();
                $("#frmPostDiscussion").submit();
                return;
            }

            var $dropdown = $("#dsMentionDropdown");
            if ($dropdown.is(":visible")) {
                var keyCode = evt.data.domEvent.$.keyCode || evt.data.domEvent.$.which;

                if (keyCode === 40) { // ArrowDown
                    evt.cancel();
                    moveMentionActiveItem(1);
                    return;
                }
                if (keyCode === 38) { // ArrowUp
                    evt.cancel();
                    moveMentionActiveItem(-1);
                    return;
                }
                if (keyCode === 13 || keyCode === 9) { // Enter or Tab
                    var $active = $("#dsMentionList .ds-mention-item.active");
                    if ($active.length > 0) {
                        evt.cancel();
                        var user = $active.data("user");
                        if (user) {
                            selectMentionUser(user);
                        }
                        return;
                    }
                }
                if (keyCode === 27) { // Escape
                    evt.cancel();
                    hideMentionDropdown();
                    return;
                }
            }
        });

        // Real-time mention detection on editor change or keyup
        editor.on('change', function () {
            updateDiscussionWordCount();
            setTimeout(function () {
                checkAndHandleEditorMention(editor);
            }, 30);
        });

        editor.on('contentDom', function () {
            editor.document.on('keyup', function (evt) {
                var keyCode = evt.data.getKeystroke();
                // Skip arrow/enter/tab/escape which are already handled
                if (keyCode === 40 || keyCode === 38 || keyCode === 13 || keyCode === 9 || keyCode === 27) return;
                checkAndHandleEditorMention(editor);
            });
        });
    });
}

function getMentionQueryFromEditor(editor) {
    try {
        var sel = editor.getSelection();
        if (!sel) return null;
        var ranges = sel.getRanges();
        if (!ranges || !ranges.length) return null;
        var range = ranges[0];
        var node = range.startContainer;
        if (!node || node.type !== CKEDITOR.NODE_TEXT) return null;
        var text = node.getText();
        var offset = range.startOffset;
        var before = text.substring(0, offset);
        var atPos = before.lastIndexOf('@');
        if (atPos === -1) return null;
        var query = before.substring(atPos + 1);
        // If there's any whitespace after '@', mention is finished/cancelled
        if (/[\s\u00A0\t\r\n]/.test(query)) return null;
        return {
            query: query,
            atPos: atPos,
            node: node,
            range: range
        };
    } catch (e) {
        return null;
    }
}

function getCaretOffset(editor) {
    try {
        var sel = editor.getSelection();
        if (!sel) return null;
        var ranges = sel.getRanges();
        if (!ranges || !ranges.length) return null;

        var dummy = editor.document.createElement('span');
        dummy.setText('\u200b');
        ranges[0].cloneRange().insertNode(dummy);
        var rect = dummy.$.getBoundingClientRect();
        dummy.remove();

        var $iframe = $(editor.container.$).find('iframe');
        if ($iframe.length) {
            var iframeOffset = $iframe.offset();
            var $parent = $(editor.container.$).parent();
            var parentOffset = $parent.offset();

            var top = (iframeOffset.top - parentOffset.top) + rect.bottom + 6;
            var left = (iframeOffset.left - parentOffset.left) + rect.left;

            var maxLeft = $parent.width() - 330;
            if (left > maxLeft) left = Math.max(10, maxLeft);
            if (left < 10) left = 10;

            if (top > 160) {
                top = Math.max(5, (iframeOffset.top - parentOffset.top) + rect.top - 240);
            }

            return { top: Math.round(top), left: Math.round(left) };
        }
    } catch (e) { }
    return null;
}

function checkAndHandleEditorMention(editor) {
    var info = getMentionQueryFromEditor(editor);
    if (info !== null) {
        var pos = getCaretOffset(editor);
        showMentionDropdown(info.query, false, pos);
    } else {
        var $dropdown = $("#dsMentionDropdown");
        if ($dropdown.is(":visible") && !$dropdown.data("opened-by-button")) {
            hideMentionDropdown();
        }
    }
}

function moveMentionActiveItem(direction) {
    var $items = $("#dsMentionList .ds-mention-item");
    if (!$items.length) return;
    var $current = $items.filter(".active");
    var curIdx = $items.index($current);
    var nextIdx = curIdx + direction;
    if (nextIdx < 0) nextIdx = $items.length - 1;
    if (nextIdx >= $items.length) nextIdx = 0;
    $items.removeClass("active");
    var $next = $items.eq(nextIdx).addClass("active");
    if ($next.length && $next[0].scrollIntoView) {
        $next[0].scrollIntoView({ block: "nearest" });
    }
}

function triggerMentionDropdown(btn) {
    var $dropdown = $("#dsMentionDropdown");
    if ($dropdown.is(":visible") && $dropdown.data("opened-by-button")) {
        hideMentionDropdown();
        return;
    }
    showMentionDropdown("", true);
}

function showMentionDropdown(query, openedByButton, customPos) {
    var salesId = getEffectiveSalesId();
    loadProjectMembersForMention(salesId, function (members) {
        var $dropdown = $("#dsMentionDropdown");
        var $list = $("#dsMentionList");
        var $searchBox = $("#boxMentionSearch");
        $list.empty();

        $dropdown.data("opened-by-button", !!openedByButton);

        if (openedByButton) {
            $searchBox.removeClass("d-none");
            $dropdown.css({ top: "auto", bottom: "48px", left: "10px" });
        } else {
            $searchBox.addClass("d-none");
            if (customPos) {
                $dropdown.css({ top: customPos.top + "px", left: customPos.left + "px", bottom: "auto" });
            } else {
                $dropdown.css({ top: "45px", left: "15px", bottom: "auto" });
            }
        }

        var filtered = members;
        if (query) {
            var q = query.toLowerCase();
            filtered = members.filter(function (m) {
                return (m.fullName && m.fullName.toLowerCase().indexOf(q) !== -1) ||
                       (m.userName && m.userName.toLowerCase().indexOf(q) !== -1);
            });
        }

        if (filtered.length === 0) {
            $list.html('<div class="p-3 text-muted text-80 text-center"><i class="fa fa-user-slash mr-1 opacity-75"></i>Không tìm thấy nhân sự phù hợp</div>');
        } else {
            filtered.forEach(function (m, idx) {
                var initial = (m.fullName ? m.fullName.trim().charAt(0).toUpperCase() : 'U');
                var $item = $('<div class="ds-mention-item' + (idx === 0 ? ' active' : '') + '">' +
                    '<div class="ds-mention-avatar">' + initial + '</div>' +
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

        // Chỉ focus ô tìm kiếm nếu mở qua click nút "@ Nhắc tên"
        if (openedByButton) {
            setTimeout(function () {
                $("#txtMentionSearch").val(query || '').focus();
            }, 50);
        }
    });
}

function hideMentionDropdown() {
    var $dropdown = $("#dsMentionDropdown");
    $dropdown.hide();
    $dropdown.data("opened-by-button", false);
    $("#txtMentionSearch").val('');
}

function handleMentionSearchInput(val) {
    showMentionDropdown(val, true);
}

function handleMentionSearchKeydown(e) {
    var $dropdown = $("#dsMentionDropdown");
    if (!$dropdown.is(":visible")) return;

    if (e.key === "ArrowDown") {
        e.preventDefault();
        moveMentionActiveItem(1);
        return;
    } else if (e.key === "ArrowUp") {
        e.preventDefault();
        moveMentionActiveItem(-1);
        return;
    } else if (e.key === "Enter") {
        e.preventDefault();
        var $active = $("#dsMentionList .ds-mention-item.active");
        if ($active.length > 0) {
            var user = $active.data("user");
            if (user) {
                selectMentionUser(user);
                return;
            }
        }
    } else if (e.key === "Escape") {
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

        var info = getMentionQueryFromEditor(editor);
        var mentionHtml = '<span class="ds-mention-badge" data-user-id="' + user.userId + '" contenteditable="false">' + user.fullName + '</span>&nbsp;';

        if (info) {
            try {
                var node = info.node;
                var fullText = node.getText();
                var beforeAt = fullText.substring(0, info.atPos);
                var afterCaret = fullText.substring(info.atPos + 1 + info.query.length);

                node.setText(beforeAt);

                var range = editor.createRange();
                range.setStart(node, beforeAt.length);
                range.setEnd(node, beforeAt.length);
                editor.getSelection().selectRanges([range]);

                editor.insertHtml(mentionHtml);

                if (afterCaret.length > 0) {
                    editor.insertText(afterCaret);
                    var sel = editor.getSelection();
                    var r = sel.getRanges()[0];
                    r.setStart(r.startContainer, r.startOffset - afterCaret.length);
                    r.setEnd(r.startContainer, r.startOffset - afterCaret.length);
                    sel.selectRanges([r]);
                }
            } catch (err) {
                editor.insertHtml(mentionHtml);
            }
        } else {
            editor.insertHtml(mentionHtml);
        }
    } else {
        var $textarea = $("#txtDiscussionContent");
        if ($textarea.length > 0) {
            var val = $textarea.val() || "";
            var lastAt = val.lastIndexOf('@');
            if (lastAt !== -1) {
                val = val.substring(0, lastAt);
            }
            $textarea.val(val + user.fullName + " ");
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
