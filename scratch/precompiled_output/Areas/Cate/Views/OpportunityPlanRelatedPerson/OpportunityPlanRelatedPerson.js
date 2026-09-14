var _RelatedPersonActionURLs = {
    RelatedPerson_GetData: "/Cate/OpportunityPlanRelatedPerson/Get"
};
var _tableRelatedPerson;

$(document).ready(function () {
    initTableRelatedPerson();
});

function initTableRelatedPerson() {
    if (!$("#DSOpportunityPlanRelatedPerson").length) return;
    if ($.fn.DataTable.isDataTable("#DSOpportunityPlanRelatedPerson")) {
        _tableRelatedPerson = $("#DSOpportunityPlanRelatedPerson").DataTable();
        return;
    }

    _tableRelatedPerson = $("#DSOpportunityPlanRelatedPerson").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "paging": false,
        "ajax": {
            "url": _RelatedPersonActionURLs.RelatedPerson_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "OpportunityPlanID": function () { return opportunityPlanID; }
            }
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "FullName",
                "defaultContent": "",
                "orderable": false,
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    if (type === "display") {
                        return '<b>' + (data || '') + '</b><p>' + (row.Username || '') + '</p>';
                    }
                    return data;
                }
            },
            {
                "data": "TenBoPhan",
                "defaultContent": "",
                "className": "text-left"
            },
            {
                "data": "TenChucVu",
                "defaultContent": "",
                "className": "text-left"
            },
            {
                "data": "Id",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        var fullName = encodeURIComponent(row.FullName || row.Username || "");
                        html += _renderButton(true,
                            "DeleteOpportunityPlanRelatedPerson",
                            "btn btn-lighter-danger mr-1",
                            "/Cate/OpportunityPlanRelatedPerson/Delete/" + data + "?fullName=" + fullName,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xoá");
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function OpportunityPlanRelatedPerson_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                reloadOpportunityPlanRelatedPersonContext();
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    reloadOpportunityPlanRelatedPersonContext();
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function reloadOpportunityPlanRelatedPersonContext() {
    if (_tableRelatedPerson && _tableRelatedPerson.ajax) {
        _tableRelatedPerson.ajax.reload(null, false);
    }

    if (typeof loadPlanDashboard === "function") {
        loadPlanDashboard();
    }

    if (typeof reloadBoPlans === "function" && typeof _boId !== "undefined") {
        reloadBoPlans(_boId);
    }
}
