var treeGrid = null;

$(document).ready(function () {
    ej.base.registerLicense('Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXted3ZWR2VdV0ZyXEVWYEo=');
    ej.treegrid.TreeGrid.Inject(ej.treegrid.ExcelExport);
    ej.base.setCulture('vi');
    ej.base.L10n.load({
        'vi': {
            'treegrid': {
                EmptyRecord: 'Không có dữ liệu'
            },
            'pager': {
                currentPageInfo: '{0} / {1} trang',
                totalItemsInfo: '({0} bản ghi)',
                firstPageTooltip: 'Trang đầu',
                lastPageTooltip: 'Trang cuối',
                nextPageTooltip: 'Trang sau',
                previousPageTooltip: 'Trang trước'
            }
        }
    });
    SearchEmployeeBusiness();
});

function initTreeGrid(data) {
    if (treeGrid) {
        treeGrid.destroy();
        treeGrid = null;
    }
    $('#TreeGrid').html('');
    treeGrid = new ej.treegrid.TreeGrid({
        dataSource: data,
        idMapping: 'BoPhan_ID',
        parentIdMapping: 'BoPhanCha_ID',
        treeColumnIndex: 1,
        allowSorting: false,
        allowPaging: true,
        allowExcelExport: true,
        pageSettings: { pageSize: 50 },
        columns: [
            { field: 'BoPhan_ID', headerText: 'ID', visible: false },
            { field: 'TenBoPhan', headerText: 'Bộ phận' },
            {
                field: 'RevenueTarget',
                headerText: 'Doanh thu thực hiện (triệu đồng)',
                textAlign: 'Right',
                width: 300,
                format: 'N0'
            },
            {
                field: 'ResponsibilityRevenue',
                headerText: 'Doanh thu chịu trách nhiệm (triệu đồng)',
                textAlign: 'Right',
                width: 300,
                format: 'N0'
            },
            {
                headerText: 'Hành động',
                width: 120,
                textAlign: 'Center',
                template: function (data) {
                    let html = '<span>';
                    if (!data.EmployeeBusinessPlanID) return "";

                    html += '<div class="dropdown d-inline-block">';
                    html += '<button class="btn btn-xs btn-lighter-primary dropdown-toggle action-btn" type="button" data-toggle="dropdown">';
                    html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                    html += '<div class="dropdown-menu dropdown-menu-right" style="z-index: 9999 !important">';

                    html += _renderButton(true,
                        "EditEmployeeBusinessPlan",
                        "dropdown-item",
                        "/Cate/EmployeeBusinessPlan/Edit/" + data.EmployeeBusinessPlanID,
                        '<i class="far fa-edit text-primary mr-1"></i> Cập nhật',
                        "Cập nhật");

                    html += _renderButton(true,
                        "DeleteEmployeeBusinessPlan",
                        "dropdown-item",
                        "/Cate/EmployeeBusinessPlan/Delete/" + data.EmployeeBusinessPlanID,
                        '<i class="far fa-trash-alt text-danger mr-1"></i> Xoá',
                        "Xoá");

                    html += '</div></div>';
                    html += '</span>';

                    return html;
                }
            }
        ]
    });

    treeGrid.appendTo('#TreeGrid');
}

function loadData(year, boPhanId) {
    $.ajax({
        url: '/Cate/EmployeeBusinessPlan/Get',
        type: 'POST',
        data: {
            year: year,
            boPhanId: boPhanId
        },
        success: function (data) {

            if (!treeGrid) {
                initTreeGrid(data);
            } else {
                treeGrid.setProperties({ dataSource: data });
                treeGrid.refresh();
            }
        }
    });
}

function SearchEmployeeBusiness() {
    var year = $('#Year').val();
    var boPhanId = $('#BoPhanID').val();
    loadData(year, boPhanId);
}

$('#btnExportEmployeeBusinessPlan').click(function () {
    if (treeGrid) {
        treeGrid.excelExport();
    }
});

function EmployeeBusinessPlan_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        $("#ModalContent #modal_" + formId).modal("hide");
        $("#ModalContent #modal_" + formId).on("hidden.bs.modal",
            function () {
                if (response.status != undefined) {
                    eval(response.message);
                    SearchEmployeeBusiness();
                    response.status = undefined;
                }
            });
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}