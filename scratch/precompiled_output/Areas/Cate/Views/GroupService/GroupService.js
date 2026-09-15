var _GroupServiceActionURLs = {
    GroupService_GetData: "/Cate/GroupService/Get"
};

var _tableGroupService = null;

$(document).ready(function () {
    if (!_tableGroupService) {
        initTableGroupService();
    }
});

function initTableGroupService() {

    _tableGroupService = $('#tblGroupService').DataTable({
        processing: true,
        serverSide: false,   // ❗ bỏ server side
        paging: false,       // ❗ tree không paging
        searching: false,
        ordering: false,
        ajax: {
            url: _GroupServiceActionURLs.GroupService_GetData,
            type: "POST",
            dataSrc: "data",
            data: function (d) {
                d.search = $('#Keyword').val();
            }
        },
        columns: [
            {
                data: "NameGroup",
                render: function (data, type, row) {

                    if (type !== "display") return data;

                    var level = row.Level || 0;
                    var padding = level * 25;

                    return `
                            <div class="text-left" style="padding-left:${padding}px; font-weight:${level === 0 ? 700 : 400}">
                                ${data}
                            </div>
                        `;
                }
            },
            {
                data: "IsActived",
                render: function (data, type, row) {
                    var html = "";
                    if (row.IsActived) {
                        if (data) {
                            html = '<i class="fa fa-check text-green"></i>';
                        }
                    }
                    return html;
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center ",
                render: function (data, type, row, meta) {

                    var html = '<span class="d-flex justify-content-end">';

                    if (type === "display") {

                        html += _renderButton(
                            true,
                            "EditGroupService",
                            "btn btn-lighter-primary mr-1",
                            "/Cate/GroupService/Edit/" + row.GroupServiceID,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật"
                        );

                        // kiểm tra có child không
                        var hasChild = meta.settings.json.data.some(function (x) {
                            return x.ParentGroupServiceID === row.GroupServiceID;
                        });

                        if (!hasChild) {
                            html += _renderButton(
                                true,
                                "DeleteGroupService",
                                "btn btn-lighter-danger mr-1",
                                "/Cate/GroupService/Delete/" + row.GroupServiceID,
                                '<i class="far fa-trash-alt text-danger text-120"></i>',
                                "Xoá"
                            );
                        }
                    }

                    html += "</span>";

                    return html;
                }
            }
        ]
    });
}

function GroupService_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableGroupService.ajax.reload(null, false);
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
                        _tableGroupService.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}