var _BusinessPlanActionURLs = {
    BusinessPlan_GetData: "/Cate/RM_BusinessPlan/Get"
};
var _tableBusinessPlan;
$(document).ready(function () {
    initTableBusinessPlan();
});

function initTableBusinessPlan() {
    _tableBusinessPlan = $("#DSBusinessPlan").DataTable({
        "Responsive": true,
        "BusinessPlan": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _BusinessPlanActionURLs.BusinessPlan_GetData,
            "type": "POST",
            "dataType": "JSON",
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
                "data": "PlanYear",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "DecisionNo",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "DecisionDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data ? moment(data).format("DD/MM/YYYY") : "";
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
                "data": "BusinessPlanID",
                "style": "width:100px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';

                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block"><button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-ellipsis-h text-120"></i></button><div class="dropdown-menu dropdown-menu-right">';
                        html += _renderButton(true,
                            "IndexBusinessOpportunity",
                            "btn btn-lighter-success mr-1 btn-a-outline-success dropdown-item",
                            "/Cate/EmployeeBusinessPlan/EmployeeBusinessPlan/" + data,
                            '<i class="fas fa-business-time text-success text-120 mr-1"></i> Cá nhân',
                            "Cá nhân", 1024);
                        html += _renderButton(true,
                            "EditBusinessPlan",
                            "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item",
                            "/Cate/RM_BusinessPlan/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật',
                            "Cập nhật");
                        html += _renderButton(true,
                            "DeleteBusinessPlan",
                            "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item",
                            "/Cate/RM_BusinessPlan/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá',
                            "Xoá");
                        html += '</div></div>';
                    }
                    html += "</span>";

                    return html;
                }
            }
        ]
    });
}


function BusinessPlan_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableBusinessPlan.ajax.reload(null, false);
                response.status = undefined;
                //$("#ModalContent #modal_" + formId + " form")[0].reset();
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function (data, textStatus, xhr) {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal",
                function () {
                    if (response.status != undefined) {
                        eval(response.message);
                        _tableBusinessPlan.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
