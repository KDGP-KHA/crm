var _expandedNodes = new Set();
var _isFirstLoad = true;
var _InstructActionURLs = {
    Instruct_GetData: "/Sys/Instruct/Get"
};
var _tableInstruct;
$(document).ready(function () {
    if (!_tableInstruct) {
        initTableInstruct();
    }
});
function initTableInstruct() {
    _tableInstruct = $("#DSInstruct").DataTable({
        "responsive": false,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "searching": false,
        "paging": false,
        "ajax": {
            "url": _InstructActionURLs.Instruct_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.search = $('#Keyword').val();
            },
        },
        "columns": [
            {
                "className": "text-left",
                "data": "InstructName",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    let level = row.Level || 0;
                    let indent = level * 28;
                    let InstructID = row.InstructID;
                    let InstructParentID = row.InstructParentID;
                    let icon = row.HasChild
                        ? `<i class="fa fa-caret-right toggle-tree mr-2 text-primary-m1" style="font-size:16px; width:18px; cursor:pointer"
                               data-id="${row.InstructID}"></i>`
                        : `<span class="d-inline-block mr-2" style="width:18px"></span>`;
                    let nodeIcon = row.HasChild
                        ? `<i class="far fa-folder text-primary-m1 mr-2"></i>`
                        : `<i class="far fa-file-alt text-secondary-m1 mr-2"></i>`;
                    let weightClass = row.HasChild ? "font-weight-bold text-dark-m1" : "text-dark-m2";
                    return `
                            <div class="tree-node ml-2 d-flex align-items-center ${weightClass}"
                                 data-id="${InstructID}"
                                 data-parent="${InstructParentID}"
                                 style="padding-left:${indent}px">
                                 ${icon}
                                 ${nodeIcon}
                                 <span>${data}</span>
                            </div>
                        `;
                }
            }, {
                "className": "text-center",
                "data": "IsActive",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = "";
                    if (row.IsActive && data) {
                        html = '<i class="fa fa-check text-green"></i>';
                    }
                    return html;
                }
            },
            {
                "className": "text-center",
                "data": "InstructID",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = '<span>';

                    if (type === "display") {
                        html += _renderButton(
                            true,
                            "EditInstruct",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/Instruct/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật"
                        );
                        if (!row.HasChild) {
                            html += _renderButton(
                                true,
                                "DeleteInstruct",
                                "btn btn-lighter-danger mr-1",
                                "/Sys/Instruct/Delete/" + data,
                                '<i class="far fa-trash-alt text-danger text-120"></i>',
                                "Xoá"
                            );
                        }
                    }

                    html += "</span>";
                    return html;
                }
            }
        ],
        drawCallback: function () {
            bindTree();
        }
    });
}

function Instruct_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableInstruct.ajax.reload(null, false);
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function (data, textStatus, xhr) {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).one("hidden.bs.modal",
                function () {
                    if (response.status != undefined) {
                        eval(response.message);
                        _tableInstruct.ajax.reload(null, false);
                        response.status = undefined;
                    }
                });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function bindTree() {
    if (!_tableInstruct) return;
    const rows = _tableInstruct.rows().nodes();
    const hasKeyword = ($('#Keyword').val() || '').trim().length > 0;

    if (hasKeyword) {
        $(rows).each(function () {
            $(this).show();
            setNodeState(this, true);
        });
        return;
    }

    $(rows).each(function () {
        const rowData = _tableInstruct.row(this).data();
        const isRoot = rowData.InstructParentID === null || rowData.InstructParentID === 0;

        if (isRoot) {
            $(this).show();
        } else {
            $(this).hide();
        }
        setNodeState(this, false);
    });

    _expandedNodes.forEach(id => {
        toggleNode(id, true);

        $(rows).each(function () {
            const rowData = _tableInstruct.row(this).data();
            if (rowData.InstructID === id) {
                setNodeState(this, true);
            }
        });
    });

    _isFirstLoad = false;
}

$('#DSInstruct').on('click', '.toggle-tree', function (e) {
    e.stopPropagation();

    const id = $(this).data('id');
    const tr = $(this).closest('tr');
    const isOpen = tr.hasClass('open');

    if (isOpen) {
        _expandedNodes.delete(id);
        toggleNode(id, false);
    } else {
        _expandedNodes.add(id);
        toggleNode(id, true);
    }
    setNodeState(tr, !isOpen);
});

function toggleNode(parentId, expand) {
    const rows = _tableInstruct.rows().nodes();

    $(rows).each(function () {
        const row = _tableInstruct.row(this).data();
        if (row.InstructParentID == parentId) {
            if (expand) {
                $(this).show();
                const childId = row.InstructID;
                if (_expandedNodes.has(childId)) {
                    setNodeState(this, true);
                    toggleNode(childId, true);
                }
            } else {
                $(this).hide();
                toggleNode(row.InstructID, false);
            }
        }
    });
}

function setNodeState(rowEl, expanded) {
    if (expanded) {
        $(rowEl).addClass('open')
            .find('.toggle-tree')
            .removeClass('fa-caret-right')
            .addClass('fa-caret-down');
    } else {
        $(rowEl).removeClass('open')
            .find('.toggle-tree')
            .removeClass('fa-caret-down')
            .addClass('fa-caret-right');
    }
}

$('#Keyword').on('keypress', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        searchInstructs();
    }
});

function searchInstructs() {
    _tableInstruct.ajax.reload(null, false);
}
