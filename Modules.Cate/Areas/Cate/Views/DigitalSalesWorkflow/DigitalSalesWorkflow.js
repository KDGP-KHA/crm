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

    var pendingRequests = {
        statuses: null,
        processes: null,
        progresses: null
    };

    function init() {
        // Lấy businessType từ radio/tab ban đầu
        var initialType = $("input[name='tabBusinessType']:checked").val() || 1;
        state.businessType = parseInt(initialType);

        // Auto click first status if exists
        autoSelectFirstStatus();
    }

    function switchBusinessType(type, element, event) {
        if (event) {
            event.preventDefault();
        }

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
        loadStatuses(state.businessType);

        return false;
    }

    function loadStatuses(businessType, targetStatusId) {
        var $container = $("#statusContainer");
        $container.html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-primary"></i></div>');

        abortPendingRequest("statuses");
        pendingRequests.statuses = $.ajax({
            url: urls.getStatuses,
            type: "GET",
            dataType: "html",
            global: false,
            data: { businessType: businessType },
            success: function (html) {
                if (!replacePartial($container, html, "#statusListItems")) {
                    return;
                }

                var targetItem = targetStatusId
                    ? $container.find(".status-item[data-id='" + targetStatusId + "']")
                    : $();
                if (targetItem.length > 0) {
                    selectStatus(targetStatusId, targetItem[0]);
                } else {
                    autoSelectFirstStatus();
                }
            },
            error: function (xhr, statusText) {
                if (statusText !== "abort") {
                    $container.html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách trạng thái</div>');
                }
            },
            complete: function (xhr) {
                if (pendingRequests.statuses === xhr) {
                    pendingRequests.statuses = null;
                }
            }
        });
    }

    function autoSelectFirstStatus() {
        var firstItem = $("#statusContainer .status-item").first();
        if (firstItem.length > 0) {
            selectStatus(firstItem.data("id"), firstItem[0]);
        } else {
            resetProcessColumn();
            resetProgressColumn();
        }
    }

    function selectStatus(statusId, element, event) {
        if (event) {
            if ($(event.target).closest(".action-buttons").length > 0) {
                return false;
            }
            event.preventDefault();
        }

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
        return false;
    }

    function loadProcesses(statusId, targetProcessId) {
        var $container = $("#processContainer");
        $container.html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-info"></i></div>');

        abortPendingRequest("processes");
        pendingRequests.processes = $.ajax({
            url: urls.getProcesses,
            type: "GET",
            dataType: "html",
            global: false,
            data: { statusId: statusId },
            success: function (html) {
                if (state.statusId !== parseInt(statusId)) {
                    return;
                }

                if (!replacePartial($container, html, "#processListItems")) {
                    return;
                }
                if (targetProcessId) {
                    var targetItem = $container.find(".process-item[data-id='" + targetProcessId + "']");
                    if (targetItem.length > 0) {
                        selectProcess(targetProcessId, targetItem[0]);
                        return;
                    }
                }

                autoSelectFirstProcess();
            },
            error: function (xhr, statusText) {
                if (statusText !== "abort") {
                    $container.html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách quy trình</div>');
                }
            },
            complete: function (xhr) {
                if (pendingRequests.processes === xhr) {
                    pendingRequests.processes = null;
                }
            }
        });
    }

    function autoSelectFirstProcess() {
        var firstItem = $("#processContainer .process-item").first();
        if (firstItem.length > 0) {
            selectProcess(firstItem.data("id"), firstItem[0]);
        } else {
            resetProgressColumn();
        }
    }

    function selectProcess(processId, element, event) {
        if (event) {
            if ($(event.target).closest(".action-buttons").length > 0) {
                return false;
            }
            event.preventDefault();
        }

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
        return false;
    }

    function loadProgresses(processId) {
        var $container = $("#progressContainer");
        $container.html('<div class="text-center py-4"><i class="fa fa-spinner fa-spin fa-2x text-success"></i></div>');

        abortPendingRequest("progresses");
        pendingRequests.progresses = $.ajax({
            url: urls.getProgresses,
            type: "GET",
            dataType: "html",
            global: false,
            data: { processId: processId },
            success: function (html) {
                if (state.processId === parseInt(processId)) {
                    replacePartial($container, html, "#progressListItems");
                }
            },
            error: function (xhr, statusText) {
                if (statusText !== "abort") {
                    $container.html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Lỗi tải danh sách tiến trình</div>');
                }
            },
            complete: function (xhr) {
                if (pendingRequests.progresses === xhr) {
                    pendingRequests.progresses = null;
                }
            }
        });
    }

    function replacePartial($container, html, expectedSelector) {
        var $response = $("<div></div>").append($.parseHTML(html, document, true));
        if ($response.find(expectedSelector).length === 0) {
            $container.html('<div class="text-center text-danger py-4"><i class="fa fa-exclamation-triangle mr-1"></i>Dữ liệu trả về không đúng định dạng</div>');
            return false;
        }

        $container.html(html);
        return true;
    }

    function abortPendingRequest(requestName) {
        var request = pendingRequests[requestName];
        if (request && request.readyState !== 4) {
            request.abort();
        }
    }

    function resetProcessColumn() {
        abortPendingRequest("processes");
        $("#lblSelectedStatus").text("(Chưa chọn trạng thái)");
        $("#btnAddProcess").addClass("disabled").attr("disabled", "disabled");
        $("#processContainer").html('<div class="text-center py-5 text-secondary-m2"><i class="fa fa-arrow-left text-160 mb-2 opacity-50"></i><p class="mb-0 text-90">Vui lòng chọn một Trạng thái ở cột bên trái</p></div>');
    }

    function resetProgressColumn() {
        abortPendingRequest("progresses");
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

    function openEditStatus(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/EditStatus/" + id;
        var btn = $('<a data-modal="" data-modal-id="editStatus" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
    }

    function openDeleteStatus(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/DeleteStatus/" + id;
        var btn = $('<a data-modal="" data-modal-id="deleteStatus" data-width="500" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
    }

    function openEditProcess(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/EditProcess/" + id;
        var btn = $('<a data-modal="" data-modal-id="editProcess" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
    }

    function openDeleteProcess(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/DeleteProcess/" + id;
        var btn = $('<a data-modal="" data-modal-id="deleteProcess" data-width="500" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
    }

    function openEditProgress(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/EditProgress/" + id;
        var btn = $('<a data-modal="" data-modal-id="editProgress" data-width="700" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
    }

    function openDeleteProgress(id, event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        var url = "/Cate/DigitalSalesWorkflow/DeleteProgress/" + id;
        var btn = $('<a data-modal="" data-modal-id="deleteProgress" data-width="500" href="' + url + '"></a>');
        $("body").append(btn);
        btn.trigger("click");
        btn.remove();
        return false;
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
                    loadStatuses(response.businessType || state.businessType, targetStatusId);
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
                    loadProcesses(stId, targetProcId);
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
                        state.statusId = 0;
                        state.processId = 0;
                        resetProcessColumn();
                        resetProgressColumn();
                        loadStatuses(response.businessType || state.businessType);
                    } else if (targetType === "Process") {
                        state.processId = 0;
                        resetProgressColumn();
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
        openEditStatus: openEditStatus,
        openDeleteStatus: openDeleteStatus,
        openAddProcess: openAddProcess,
        openEditProcess: openEditProcess,
        openDeleteProcess: openDeleteProcess,
        openAddProgress: openAddProgress,
        openEditProgress: openEditProgress,
        openDeleteProgress: openDeleteProgress,
        onStatusSaveSuccess: onStatusSaveSuccess,
        onProcessSaveSuccess: onProcessSaveSuccess,
        onProgressSaveSuccess: onProgressSaveSuccess,
        onDeleteSuccess: onDeleteSuccess
    };
})();

$(document).ready(function () {
    DigitalSalesWorkflow.init();

    // Bắt sự kiện submit an toàn cho nút #btnSave và #btnConfirm trong modal footer
    $(document).on("click", ".modal #btnSave", function (e) {
        var $modal = $(this).closest(".modal");
        var $form = $modal.find("form");
        if ($form.length > 0) {
            e.preventDefault();
            $form.submit();
        }
    });

    $(document).on("click", ".modal #btnConfirm", function (e) {
        var $modal = $(this).closest(".modal");
        var $form = $modal.find("form");
        if ($form.length > 0) {
            e.preventDefault();
            $form.submit();
        }
    });
});
