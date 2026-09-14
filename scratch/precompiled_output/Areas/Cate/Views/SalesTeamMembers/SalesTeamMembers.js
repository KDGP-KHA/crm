var _SalesTeamMembersActionURLs = {
    SalesTeamMembers_GetData: "/Cate/SalesTeamMembers/Get"
};
var _tableSalesTeamMembers;
$(document).ready(function () {
    initTableSalesTeamMembers();
});

function initTableSalesTeamMembers() {
    _tableSalesTeamMembers = $("#DSSalesTeamMembers").DataTable({
        "Responsive": true,
        "SalesTeamMembers": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "paging": false,
        "ajax":
        {
            "url": _SalesTeamMembersActionURLs.SalesTeamMembers_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "BusinessOpportunityID": function () { return businessOpportunityID;  },
            }
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                },            
            },
            {
                "data": "FullName",
                "defaultContent": "",
                "orderable": false,
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    if (type === "display") {
                        return '<b>' + data + '</b>' + '<p>' + row.Employee_Code + '</p>';
                    }
                    return data;
                }
            },
            {
                "data": "TenBoPhan",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "TenChucVu",
                "defaultContent": "",
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "RoleNames",
                "defaultContent": "",
                "orderable": false,
                "className": "text-left",
                "render": function (data, type, row, meta) {
                    return data.split(";").map(v => `<p class="mb-0">${v}</p>`).join("");
                }
            },
            {
                "data": "MemberID",
                "style": "width:150px;",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';

                    if (type === "display") {
                        html += _renderButton(true,
                            "EditSalesTeamMembers",
                            "btn btn-lighter-primary mr-1",
                            "/Cate/SalesTeamMembers/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Thêm vai trò");

                        html += _renderButton(true,
                            "DeleteSalesTeamMembers",
                            "btn btn-lighter-danger mr-1",
                            "/Cate/SalesTeamMembers/Delete/" + data,
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


function SalesTeamMembers_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableSalesTeamMembers.ajax.reload(null, false);
                if (typeof reloadBoMembers === "function") {
                    reloadBoMembers();
                }
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
                        _tableSalesTeamMembers.ajax.reload(null, false);
                        if (typeof reloadBoMembers === "function") {
                            reloadBoMembers();
                        }
                        response.status = undefined;
                        if (_tableBusinessOpportunity) {
                            _tableBusinessOpportunity.ajax.reload(null, false);
                        }
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
