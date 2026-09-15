var _ImportEmployeeBusinessPlanActionURLs = {
    ImportEmployeeBusinessPlan_GetData: "/Cate/EmployeeBusinessPlan/GetDSImport"
};
var _tableImportEmployeeBusinessPlan;

$(document).ready(function () {
    initTableImportEmployeeBusinessPlan()
});

function initTableImportEmployeeBusinessPlan() {
    _tableImportEmployeeBusinessPlan = $("#DSImportEmployeeBusinessPlan").DataTable({
        "Responsive": true,
        "language": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "pageLength": 50,
        "ajax":
        {
            "url": _ImportEmployeeBusinessPlanActionURLs.ImportEmployeeBusinessPlan_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                Key: function () { return $("#box #Key").val(); },
            }
        },
        "columns": [
            {
                "data": "",
                "className": "text-center",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.row + 1;
                }
            },
            {
                "data": "TenBoPhan",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "FullName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "RevenueTarget",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "ResponsibilityRevenue",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "Note",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "Message",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return '<span style="color: red;">' + data + '</span>';
                }
            }
        ]
    });
}

function ImportEmployeeBusinessPlan_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        $("#ModalContent #modal_" + formId).modal("hide");
        $("#ModalContent #modal_" + formId).on("hidden.bs.modal",
            function () {
                if (response.status != undefined) {
                    eval(response.message);
                    _tableImportEmployeeBusinessPlan.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}