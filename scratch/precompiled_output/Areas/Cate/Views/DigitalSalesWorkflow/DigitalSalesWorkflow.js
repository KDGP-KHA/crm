var DigitalSalesWorkflow = (function () {
    var state = {
        businessType: 1,
        statusId: 0,
        statusName: "",
        processId: 0,
        processName: ""
    };

    var urls = {
        getStatuses: "/Cate/DigitalSalesWorkflow/GetStatuses",
        addStatus: "/Cate/DigitalSalesWorkflow/AddStatus",
        getProcesses: "/Cate/DigitalSalesWorkflow/GetProcesses",
        addProcess: "/Cate/DigitalSalesWorkflow/AddProcess",
        getProgresses: "/Cate/DigitalSalesWorkflow/GetProgresses",
        addProgress: "/Cate/DigitalSalesWorkflow/AddProgress"
    };

    function init() {
        // Lấy businessType từ radio/tab ban đầu
        var initialType = $("input[name='tabBusinessType']:checked").val() || 1;
        state.businessType = parseInt(initialType);

        // Auto click first status if exists
        autoSelectFirstStatus();
    }

    function switchBusinessType(type, element) {
        state.businessType = parseInt(type);
        state.statusId = 0;
        state.statusName = "";
        state.processId = 0;
        state.processName = "";

        if (element) {
            $("#workflowBusinessTypeTabs .nav-link").removeClass("active");
            $(element).addClass("active");
        }

        resetProcessColumn();
        resetProgressColumn();

        $("#statusContainer").html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-primary"></i></div>');

        $.ajax({
            url: urls.getStatuses,
            type: "GET",
            data: { businessType: state.businessType },
            success: function (html) {
                $("#statusContainer").html(html);
                autoSelectFirstStatus();
            },
            error: function () {
                $("#statusContainer").html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách trạng thái</div>');
            }
        });
    }

    function autoSelectFirstStatus() {
        var firstItem = $("#statusContainer .status-item").first();
        if (firstItem.length > 0) {
            firstItem.trigger("click");
        } else {
            resetProcessColumn();
            resetProgressColumn();
        }
    }

    function selectStatus(statusId, element) {
        state.statusId = parseInt(statusId);
        state.statusName = $(element).attr("data-name") || "";
        state.processId = 0;
        state.processName = "";

        // Highlight active item với Ace Admin tokens
        $("#statusContainer .status-item").removeClass("active bgc-primary-l3 text-primary-d2 shadow-sm font-bold");
        $(element).addClass("active bgc-primary-l3 text-primary-d2 shadow-sm font-bold");

        // Update process column header
        $("#lblSelectedStatus").html('<i class="fa fa-tag text-blue mr-1"></i>' + state.statusName);
        $("#btnAddProcess").removeClass("disabled").removeAttr("disabled");

        // Reset progress column
        resetProgressColumn();

        // Load processes
        loadProcesses(state.statusId);
    }

    function loadProcesses(statusId, callback) {
        $("#processContainer").html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-info"></i></div>');

        $.ajax({
            url: urls.getProcesses,
            type: "GET",
            data: { statusId: statusId },
            success: function (html) {
                $("#processContainer").html(html);
                if (typeof callback === "function") {
                    callback();
                } else {
                    autoSelectFirstProcess();
                }
            },
            error: function () {
                $("#processContainer").html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách quy trình</div>');
            }
        });
    }

    function autoSelectFirstProcess() {
        var firstItem = $("#processContainer .process-item").first();
        if (firstItem.length > 0) {
            firstItem.trigger("click");
        } else {
            resetProgressColumn();
        }
    }

    function selectProcess(processId, element) {
        state.processId = parseInt(processId);
        state.processName = $(element).attr("data-name") || "";

        // Highlight active item với Ace Admin tokens
        $("#processContainer .process-item").removeClass("active bgc-blue-l3 text-blue-d2 shadow-sm font-bold");
        $(element).addClass("active bgc-blue-l3 text-blue-d2 shadow-sm font-bold");

        // Update progress column header
        $("#lblSelectedProcess").html('<i class="fa fa-project-diagram text-purple mr-1"></i>' + state.processName);
        $("#btnAddProgress").removeClass("disabled").removeAttr("disabled");

        // Load progresses
        loadProgresses(state.processId);
    }

    function loadProgresses(processId) {
        $("#progressContainer").html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-success"></i></div>');

        $.ajax({
            url: urls.getProgresses,
            type: "GET",
            data: { processId: processId },
            success: function (html) {
                $("#progressContainer").html(html);
            },
            error: function () {
                $("#progressContainer").html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách tiến trình</div>');
            }
        });
    }

    function resetProcessColumn() {
        $("#lblSelectedStatus").text("(Chưa chọn trạng thái)");
        $("#btnAddProcess").addClass("disabled").attr("disabled", "disabled");
        $("#processContainer").html('<div class="text-center py-5 text-secondary-m2"><i class="fa fa-arrow-left text-160 mb-2 opacity-50"></i><p class="mb-0 text-90">Vui lòng chọn một Trạng thái ở cột bên trái</p></div>');
    }

    function resetProgressColumn() {
        $("#lblSelectedProcess").text("(Chưa chọn quy trình)");
        $("#btnAddProgress").addClass("disabled").attr("disabled", "disabled");
        $("#progressContainer").html('<div class="text-center py-5 text-secondary-m2"><i class="fa fa-arrow-left text-160 mb-2 opacity-50"></i><p class="mb-0 text-90">Vui lòng chọn một Quy trình ở cột giữa</p></div>');
    }

    function openAddStatus() {
        var url = urls.addStatus + "?businessType=" + state.businessType;
        var btn = $('<a data-modal="" data-modal-id="addStatus" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
    }

    function openAddProcess() {
        if (!state.statusId) {
            if (typeof toastr !== "undefined") {
                toastr.warning("Vui lòng chọn một trạng thái trước khi thêm quy trình.");
            } else {
                alert("Vui lòng chọn một trạng thái trước khi thêm quy trình.");
            }
            return;
        }
        var url = urls.addProcess + "?statusId=" + state.statusId;
        var btn = $('<a data-modal="" data-modal-id="addProcess" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
    }

    function openAddProgress() {
        if (!state.processId) {
            if (typeof toastr !== "undefined") {
                toastr.warning("Vui lòng chọn một quy trình trước khi thêm tiến trình.");
            } else {
                alert("Vui lòng chọn một quy trình trước khi thêm tiến trình.");
            }
            return;
        }
        var url = urls.addProgress + "?processId=" + state.processId;
        var btn = $('<a data-modal="" data-modal-id="addProgress" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
    }

    function renderValidationResponse(response) {
        if (typeof response !== "string") {
            return false;
        }

        var activeModal = $(".modal.show").last();
        var bodyForm = activeModal.find("#bodyForm");
        if (bodyForm.length === 0) {
            return false;
        }

        bodyForm.html(response);

        if (typeof _initElement === "function") {
            _initElement();
        }

        var form = bodyForm.closest("form");
        if ($.validator && $.validator.unobtrusive && form.length > 0) {
            form.removeData("validator");
            form.removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse(form);
        }

        return true;
    }

    // Modal Callback Handlers tuân thủ hidden.bs.modal để tránh kẹt backdrop
    function onStatusSaveSuccess(response) {
        if (renderValidationResponse(response)) {
            return;
        }

        if (response.status || response.success) {
            var activeModal = $(".modal.show");
            if (activeModal.length > 0) {
                activeModal.modal("hide");
                activeModal.one("hidden.bs.modal", function () {
                    if (response.message) {
                        eval(response.message);
                    }
                    var targetStatusId = response.statusId || state.statusId;
                    $.ajax({
                        url: urls.getStatuses,
                        type: "GET",
                        data: { businessType: response.businessType || state.businessType },
                        success: function (html) {
                            $("#statusContainer").html(html);
                            var targetItem = $("#statusContainer .status-item[data-id='" + targetStatusId + "']");
                            if (targetItem.length > 0) {
                                targetItem.trigger("click");
                            } else {
                                autoSelectFirstStatus();
                            }
                        }
                    });
                });
            } else {
                if (response.message) {
                    eval(response.message);
                }
            }
        } else {
            if (response.message) {
                eval(response.message);
            }
        }
    }

    function onProcessSaveSuccess(response) {
        if (renderValidationResponse(response)) {
            return;
        }

        if (response.status || response.success) {
            var activeModal = $(".modal.show");
            if (activeModal.length > 0) {
                activeModal.modal("hide");
                activeModal.one("hidden.bs.modal", function () {
                    if (response.message) {
                        eval(response.message);
                    }
                    var stId = response.statusId || state.statusId;
                    var targetProcId = response.processId || state.processId;
                    loadProcesses(stId, function () {
                        var targetItem = $("#processContainer .process-item[data-id='" + targetProcId + "']");
                        if (targetItem.length > 0) {
                            targetItem.trigger("click");
                        } else {
                            autoSelectFirstProcess();
                        }
                    });
                });
            } else {
                if (response.message) {
                    eval(response.message);
                }
            }
        } else {
            if (response.message) {
                eval(response.message);
            }
        }
    }

    function onProgressSaveSuccess(response) {
        if (renderValidationResponse(response)) {
            return;
        }

        if (response.status || response.success) {
            var activeModal = $(".modal.show");
            if (activeModal.length > 0) {
                activeModal.modal("hide");
                activeModal.one("hidden.bs.modal", function () {
                    if (response.message) {
                        eval(response.message);
                    }
                    var procId = response.processId || state.processId;
                    loadProgresses(procId);
                });
            } else {
                if (response.message) {
                    eval(response.message);
                }
            }
        } else {
            if (response.message) {
                eval(response.message);
            }
        }
    }

    function onDeleteSuccess(response, targetType) {
        if (response.status || response.success) {
            var activeModal = $(".modal.show");
            if (activeModal.length > 0) {
                activeModal.modal("hide");
                activeModal.one("hidden.bs.modal", function () {
                    if (response.message) {
                        eval(response.message);
                    }
                    if (targetType === "Status") {
                        switchBusinessType(state.businessType);
                    } else if (targetType === "Process") {
                        loadProcesses(state.statusId);
                    } else if (targetType === "Progress") {
                        loadProgresses(state.processId);
                    }
                });
            } else {
                if (response.message) {
                    eval(response.message);
                }
            }
        } else {
            if (response.message) {
                eval(response.message);
            }
        }
    }

    return {
        init: init,
        switchBusinessType: switchBusinessType,
        selectStatus: selectStatus,
        selectProcess: selectProcess,
        openAddStatus: openAddStatus,
        openAddProcess: openAddProcess,
        openAddProgress: openAddProgress,
        onStatusSaveSuccess: onStatusSaveSuccess,
        onProcessSaveSuccess: onProcessSaveSuccess,
        onProgressSaveSuccess: onProgressSaveSuccess,
        onDeleteSuccess: onDeleteSuccess
    };
})();

$(document).ready(function () {
    DigitalSalesWorkflow.init();
});
