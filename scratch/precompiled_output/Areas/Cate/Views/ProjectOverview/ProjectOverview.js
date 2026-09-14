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

function Project_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProject
    ]);
}

function ProductProject_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProductProject,
        reloadProject
    ]);
}

function Contracts_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadContracts,
    ]);
}

function reloadReviewHistory() {
    $("#reviewHistoryContainer").load(_urlReloadReviewHistory + "?objectType=2&id=" + _projectId);
}

function reloadProject() {
    $("#projectContainer").load(_urlReloadProject + "?id=" + _projectId);
}

function reloadProductProject() {
    $("#productProjectContainer").load(_urlReloadProductProject + "?id=" + _projectId);
}

function reloadContracts() {
    $("#contractsContainer").load(_urlReloadContracts + "?id=" + _projectId);
}

// ========== RevenueAllocation ==========

function reloadRevenueAllocation() {
    // Không cần reload gì thêm
}

function RevenueAllocation_OnProcessSuccess(response, formId) {
    var $modal = $("#" + formId).closest(".modal");

    if (response && response.status !== undefined) {
        if (response.status) {
            $modal.modal("hide");
            $modal.on("hidden.bs.modal", function () {
                if (response && response.status !== undefined) {
                    eval(response.message);
                    response.status = undefined;
                }
            });
        } else {
            eval(response.message);
            $("#btnSaveAllocation")
                .prop("disabled", false)
                .html('<i class="fa fa-save"></i>&nbsp;Lưu');
        }

        return;
    }

    $modal.find("#bodyForm").html(response);
}

function RevenueAllocation_Init() {
    function calcTotal() {
        var total = 0;
        $(".allocation-rate-input").each(function () {
            var v = parseFloat($(this).val()) || 0;
            total += v;
        });
        total = Math.round(total * 100) / 100;
        $("#totalRateDisplay").text(total);

        if (total > 100) {
            $("#totalRateDisplay").closest("small").addClass("text-danger").removeClass("text-muted");
            $("#allocationWarning").removeClass("d-none");
            $("#btnSaveAllocation").prop("disabled", true);
        } else {
            $("#totalRateDisplay").closest("small").removeClass("text-danger").addClass("text-muted");
            $("#allocationWarning").addClass("d-none");
            $("#btnSaveAllocation").prop("disabled", false);
        }
    }

    $(document).off("input blur", ".allocation-rate-input").on("input blur", ".allocation-rate-input", function () {
        var $input = $(this);
        var val = parseFloat($input.val());
        var $err = $input.closest("td").find(".invalid-feedback-rate");

        if ($(document.activeElement).is($input) === false) {
            if (!isNaN(val)) {
                if (val < 0) {
                    $input.val(0);
                    val = 0;
                }

                if (val > 100) {
                    $input.val(100);
                    val = 100;
                }
            }
        }

        if (!isNaN(val) && (val < 0 || val > 100)) {
            $input.addClass("is-invalid");
            $err.show();
        } else {
            $input.removeClass("is-invalid");
            $err.hide();
        }

        calcTotal();
    });

    $(document).off("keydown", ".allocation-rate-input").on("keydown", ".allocation-rate-input", function (e) {
        var allowed = ["Backspace", "Delete", "Tab", "ArrowLeft", "ArrowRight", "Home", "End", "."];
        if (allowed.indexOf(e.key) !== -1 || e.ctrlKey || e.metaKey) {
            return;
        }

        if (!/^\d$/.test(e.key)) {
            e.preventDefault();
        }
    });

    $(document).off("input", ".allocation-rate-input").on("input", ".allocation-rate-input", calcTotal);
    calcTotal();

    $("#formRevenueAllocation").off("submit").on("submit", function (e) {
        e.preventDefault();

        if ($(".allocation-rate-input.is-invalid").length > 0 || $("#btnSaveAllocation").prop("disabled")) {
            return false;
        }

        var $btn = $("#btnSaveAllocation");
        $btn.prop("disabled", true).html('<i class="fa fa-spinner fa-spin mr-1"></i> Đang lưu...');

        $.ajax({
            url: $("#formRevenueAllocation").data("url"),
            type: "POST",
            data: $("#formRevenueAllocation").serialize(),
            success: function (response) {
                RevenueAllocation_OnProcessSuccess(response, "formRevenueAllocation");
            },
            error: function (xhr) {
                $btn.prop("disabled", false).html('<i class="fa fa-save"></i>&nbsp;Lưu');
                toastr && toastr.error("Có lỗi xảy ra (HTTP " + xhr.status + "), vui lòng thử lại.");
            }
        });

        return false;
    });

    $("#btnSaveAllocation").off("click").on("click", function () {
        $("#formRevenueAllocation").trigger("submit");
    });
}
