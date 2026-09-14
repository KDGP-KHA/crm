function handleModalProcessSuccess(response, formId, reloadCallbacks = []) {
    const $modal = $("#ModalContent #modal_" + formId);
    if (response.status === undefined) {
        $modal.find("#bodyForm").html(response);
        return;
    }
    const processSuccess = () => {
        eval(response.message);
        reloadCallbacks.forEach(fn => typeof fn === "function" && fn());
        response.status = undefined;
    };
    const isKeepOpen = $modal.find("#chkNotDismissModal").is(":checked");
    if (isKeepOpen) {
        processSuccess();
        const urlAction = $modal.find("form").attr("action");
        $modal.find("#modal-content").load(urlAction, function () {
            _initElement();
        });
    } else {
        $modal.off("hidden.bs.modal").on("hidden.bs.modal", function () {
            processSuccess();
        });
        $modal.modal("hide");
    }
}

function ReviewHistory_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadReviewHistory
    ]);
}

function BusinessOpportunity_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadBoDetail,
        reloadBoHeader
    ]);
}

function reloadBoHeader() {
    $("#boHeaderContainer").load(_urlReloadBoHeader + "?id=" + _boId, function () {
        _initElement && _initElement();
    });
}

function reloadBoDetail() {
    $("#boInfoContainer").load(_urlReloadBoInfo + "?id=" + _boId, function () {
        _initElement && _initElement();
    });
}

function reloadReviewHistory() {
    $("#reviewHistoryContainer").load(_urlReloadReviewHistory + "?objectType=1&id=" + _boId, function () {
        _initElement && _initElement();
    });
}

function reloadBOHistory() {
    $("#boHistoryContainer").load(_urlReloadHistory + "?id=" + _boId, function () {
        _initElement && _initElement();
        var count = $("#boHistoryContainer .exchange-history-timeline > div").length;
        $("#exchangeHistoryCount").text(count);
    });
}

function updateBoMemberCount(count) {
    count = count || 0;
    $("#memberCount").text(count);
    $("#boMemberCount").text(count);
}

function updateBoPlanCount(count) {
    count = count || 0;
    $("#planCount").text(count);
}

function reloadBoMembers() {
    $("#boMembersContainer").load(_urlReloadMembers + "?id=" + _boId, function () {
        _initElement && _initElement();
        var count = $("#boMembersContainer tbody tr").length;
        updateBoMemberCount(count);
    });
}

function reloadBoPlans(boId) {
    boId = boId || _boId;
    $("#boPlansContainer").load(_urlReloadPlans + "?id=" + boId, function () {
        _initElement && _initElement();
        var count = $("#boPlansContainer tbody tr").length;
        updateBoPlanCount(count);
    });
}

$(document).on("hidden.bs.modal", function (e) {
    var modalId = $(e.target).attr("id") || "";
    if (modalId.toLowerCase().indexOf("salesteammember") !== -1) {
        reloadBoMembers();
    }
});

function OpportunityPlan_OnProcessSuccess(response) {
    if (response && response.status !== undefined) {
        try {
            eval(response.message);
        } catch (e) {
            console.warn("eval error:", e);
        }

        if (response.status) {
            // Destroy CKEditor trước khi đóng modal để tránh lỗi instance
            if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances["Content"]) {
                CKEDITOR.instances["Content"].destroy(true);
            }

            // Đóng modal
            $(".modal.show").modal("hide");

            // Reload lại danh sách kế hoạch
            reloadBoPlans(_boId);
        }
    } else {
        var $activeModal = $(".modal.show").last();
        var $modalContent = $activeModal.find("#modal-content");

        if ($modalContent.length) {
            $modalContent.html(response);
        } else {
            $("#bodyForm").html(response);
        }

        if (typeof _initElement === "function") {
            _initElement();
        }

        if ($.validator && $.validator.unobtrusive) {
            var $form = $activeModal.find("form");
            if ($form.length) {
                $form.removeData("validator");
                $form.removeData("unobtrusiveValidation");
                $.validator.unobtrusive.parse($form);
            }
        }

        $activeModal.find(".datetimepicker").datetimepicker({
            format: "DD/MM/YYYY HH:mm",
            locale: "vi"
        });
    }
}
