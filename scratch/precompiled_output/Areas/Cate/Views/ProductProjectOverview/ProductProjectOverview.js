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
function ProductCost_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProductCost,
        reloadProductProject
    ]);
}

function RevenueReceived_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadRevenueReceived,
        reloadProductProject
    ]);
}

function ProjectMember_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProjectMember,
        reloadProductProject
    ]);
}

function ProjectTask_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProjectTask
    ]);
}

function Contracts_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadContracts
    ]);
}

function LogTask_OnProcessSuccess(response, formId) {
    handleModalProcessSuccess(response, formId, [
        reloadProjectTask
    ]);
}
function ProjectTask_SetReload() { }

function reloadProductCost() {
    $("#productCostContainer").load(_urlReloadProductCost + "?id=" + _productProjectId);
}

function reloadRevenueReceived() {
    $("#revenueReceivedContainer").load(_urlReloadRevenueReceived + "?id=" + _productProjectId);
}

function reloadProjectMember() {
    $("#projectMemberContainer").load(_urlReloadProjectMember + "?id=" + _productProjectId);
}

function reloadProjectTask() {
    $("#projectTaskContainer").load(_urlReloadProjectTask + "?id=" + _productProjectId);
}

function reloadProductProject() {
    $("#productProjectContainer").load(_urlReloadProductProject + "?id=" + _productProjectId);
}

function reloadContracts() {
    $("#contractsContainer").load(_urlReloadContracts + "?id=" + _productProjectId);
}