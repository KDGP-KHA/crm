
var _AccessHistoryActionURLs = {
    AccessHistory_GetData: "/Sys/AccessHistory/Get"
};
var _tableAccessHistory;

$(document).ready(function () {
    initTableAccessHistory()
});

function Search() {
    _tableAccessHistory.ajax.reload(null, false);
}

function initTableAccessHistory() {
    _tableAccessHistory = $("#DSAccessHistory").DataTable({
        "Responsive": true,
        "language": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": _AccessHistoryActionURLs.AccessHistory_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "TuKhoa": function () { return $("#SearchAccessHistory #TuKhoa").val(); },
                "TuNgay": function () { return $("#SearchAccessHistory #TuNgay").val(); },
                "DenNgay": function () { return $("#SearchAccessHistory #DenNgay").val(); },                
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
                "data": "Area",
                "defaultContent": ""
            },
            {
                "data": "Controller",
                "defaultContent": ""
            },
            {
                "data": "Description",
                "defaultContent": ""
            },
            {
                "data": "Action",
                "defaultContent": ""
            },   
            {
                "data": "CreatedDate",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    if (data != null) {
                        return moment(data).format("DD/MM/YYYY HH:mm:ss");
                    } else {
                        return "";
                    }
                }
            }, 
            {
                "data": "CreatedBy",
                "defaultContent": "",            
            }            
        ],
        "createdRow": function (row, data, dataIndex) {
            $(row).addClass("d-style bgc-h-default-l4");
        }
    });
}
