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

    uploadAttachment: "/Cate/DigitalSales/UploadAttachment",
    deleteAttachment: "/Cate/DigitalSales/DeleteAttachment",
    toggleKeyProject: "/Cate/DigitalSales/ToggleKeyProject",
    toggleFollow: "/Cate/DigitalSales/ToggleFollow",

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
        $("#lblKeyProject").text(isProject ? "Dự án trọng điểm" : "Cơ hội trọng điểm");
        $("#lblFollowSales").text(isProject ? "Quan tâm dự án" : "Quan tâm cơ hội");
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
        if ($.fn.select2) {
            $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
        }
        $modal.modal("show");

        $("#frmChangeStatus").on("submit", function (e) {
            e.preventDefault();
            var $form = $(this);
            var $noteInput = $form.find("textarea[name='Note']");
            var noteVal = $noteInput.val();
            if (!noteVal || !noteVal.trim()) {
                var reqMsg = "Vui lòng nhập lý do chuyển trạng thái!";
                if (typeof App_Message !== "undefined" && App_Message.DigitalSales_Msg_ChangeStatusNoteRequired) {
                    reqMsg = App_Message.DigitalSales_Msg_ChangeStatusNoteRequired;
                }
                executeResponseMessage(reqMsg, "Thông tin bắt buộc!", false);
                $noteInput.focus().addClass("border-danger");
                return false;
            }
            $noteInput.removeClass("border-danger");

            var formData = new FormData(this);
            $.ajax({
                url: _detailUrls.changeStatus,
                type: "POST",
                data: formData,
                contentType: false,
                processData: false,
                success: function (res) {
                    if (res.status) {
                        $modal.modal("hide");
                        executeResponseMessage(res.message, "Chuyển trạng thái thành công!", true);
                        if (res.businessType !== undefined) {
                            updateHeaderInfo(res.businessType, res.statusName);
                        }
                        reloadStatusAndTimelineSection(id);
                        reloadOverviewAndMetrics(id);
                        reloadTrackingSection(id);
                    } else {
                        executeResponseMessage(res.message, "Không thể chuyển trạng thái!", false);
                    }
                },
                error: function () {
                    executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
                }
            });
        });
    });
}

/* ================= 3. Sản phẩm / Dịch vụ số (Tab 2) ================= */
function openAddProductModal(salesId) {
    if (typeof _onWaiting === "function") _onWaiting();
    $.get(_detailUrls.addProductModal, { digitalSalesId: salesId })
        .done(function (html) {
            if (typeof _endWaiting === "function") _endWaiting();
            $("#modalContainer").html(html);
            var $modal = $("#modalProduct");
            if ($.fn.select2) {
                $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
            }
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
            if ($.fn.select2) {
                $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
            }
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
function openAddTrackingModal(salesId) {
    $.get(_detailUrls.addTrackingModal, { digitalSalesId: salesId }, function (html) {
        $("#modalContainer").html(html);
        var $modal = $("#modalTracking");
        if ($.fn.select2) {
            $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
        }
        $modal.modal("show");

        $("#frmTrackingModal").on("submit", function (e) {
            e.preventDefault();
            var formData = new FormData(this);
            $.ajax({
                url: _detailUrls.saveTracking,
                type: "POST",
                data: formData,
                contentType: false,
                processData: false,
                success: function (res) {
                    if (res.status) {
                        $modal.modal("hide");
                        executeResponseMessage(res.message, "Lưu tiến trình thành công!", true);
                        reloadTrackingSection(salesId);
                    } else {
                        executeResponseMessage(res.message, "Không thể lưu tiến trình!", false);
                    }
                },
                error: function () {
                    executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
                }
            });
        });
    });
}

function openEditTrackingModal(id, salesId) {
    $.get(_detailUrls.editTrackingModal, { id: id, digitalSalesId: salesId }, function (html) {
        $("#modalContainer").html(html);
        var $modal = $("#modalTracking");
        if ($.fn.select2) {
            $modal.find(".select2").select2({ width: "100%", dropdownParent: $modal });
        }
        $modal.modal("show");

        $("#frmTrackingModal").on("submit", function (e) {
            e.preventDefault();
            var formData = new FormData(this);
            $.ajax({
                url: _detailUrls.saveTracking,
                type: "POST",
                data: formData,
                contentType: false,
                processData: false,
                success: function (res) {
                    if (res.status) {
                        $modal.modal("hide");
                        executeResponseMessage(res.message, "Cập nhật tiến trình thành công!", true);
                        reloadTrackingSection(salesId);
                    } else {
                        executeResponseMessage(res.message, "Không thể cập nhật tiến trình!", false);
                    }
                },
                error: function () {
                    executeResponseMessage("Lỗi kết nối máy chủ!", "Lỗi kết nối máy chủ!", false);
                }
            });
        });
    });
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
        var mentionHtml = '&nbsp;<span class="ds-mention-badge" data-user-id="' + user.userId + '"><i class="fa fa-at mr-1"></i>' + user.fullName + '</span>&nbsp;';
        editor.insertHtml(mentionHtml);
    } else {
        var $textarea = $("#txtDiscussionContent");
        if ($textarea.length > 0) {
            $textarea.val($textarea.val() + " @" + user.fullName + " ");
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
