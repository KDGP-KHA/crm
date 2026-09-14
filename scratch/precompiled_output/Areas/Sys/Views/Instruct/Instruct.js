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
                    let indent = level * 30; // px
                    let InstructID = row.InstructID;
                    let InstructParentID = row.InstructParentID;
                    let icon = row.HasChild
                        ? `<i class="fa fa-caret-right toggle-tree" style="font-size:18px; width:20px; cursor:pointer"
                               data-id="${row.InstructID}"></i>`
                        : '';
                    return `
                            <div class="tree-node ml-3 d-flex"
                                 data-id="${InstructID}"
                                 data-parent="${InstructParentID}"
                                 style="padding-left:${indent}px">
                                 ${icon}
                                 ${data}
                            </div>
                        `;
                }
            }, {
                "className": "text-left",
                "data": "IsActive",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    var html = "";
                    if (row.IsActive) {
                        if (data) {
                            html = '<i class="fa fa-check text-green"></i>';
                        }
                    }
                    return html;
                }
            },
            {
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
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal",
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
    // Ẩn toàn bộ node con
    $(rows).each(function () {
        const rowData = _tableInstruct.row(this).data();
        const isRoot = rowData.InstructParentID === null;

        if (isRoot) {
            $(this).show();
        } else {
            $(this).hide();
        }
        // reset UI về đóng
        setNodeState(this, false);
    });

    // Mở lại các node đã expand
    _expandedNodes.forEach(id => {
        toggleNode(id, true);

        // sync lại icon
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
                    toggleNode(childId, true); // đệ quy mở tiếp
                }
            } else {
                $(this).hide();
                toggleNode(row.InstructID, false); // đệ quy đóng con cháu
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