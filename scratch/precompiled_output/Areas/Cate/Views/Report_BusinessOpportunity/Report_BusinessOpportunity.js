var _tableReport_BusinessOpportunity;
$(document).ready(function () {
    $("#Report #Nam").datepicker({
        viewMode: "years",
        minViewMode: "years",
        format: "yyyy",
        autoclose: true,
        todayhighlight: true,
        todaybtn: true,
        minDate: null,
        maxDate: null,
        orientation: "bottom",
        language: "vi"
    })
    initTableReport_BusinessOpportunity();
});
function initTableReport_BusinessOpportunity() {
    if ($.fn.DataTable.isDataTable('#Report_BusinessOpportunity')) {
        $('#Report_BusinessOpportunity').DataTable().destroy();
    }
    _tableReport_BusinessOpportunity = $("#Report_BusinessOpportunity").DataTable({
        "Responsive": true,
        "Report_BusinessOpportunity": {
            "processing":
                "<div class='overlay'><i class='fas fa-cog fa-spin'></i></div>"
        },
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ajax":
        {
            "url": "/Cate/Report_BusinessOpportunity/Get",
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "Nam": function () { return $("#RM_Report_BusinessOpportunitySearch #Nam").val(); },
                "Loai": function () { return $("#RM_Report_BusinessOpportunitySearch #Loai").val(); },
                "TuKhoa": function () { return $("#RM_Report_BusinessOpportunitySearch #TuKhoa").val(); },
                "CustomerTypeID": function () { return $("#RM_Report_BusinessOpportunitySearch #CustomerTypeID").val(); },
                "ProductServiceID": function () { return $("#RM_Report_BusinessOpportunitySearch #ProductServiceID").val(); },
                "ProjectTypeID": function () { return $("#RM_Report_BusinessOpportunitySearch #ProjectTypeID").val(); },
                "StatusID": function () { return $("#RM_Report_BusinessOpportunitySearch #StatusID").val(); }
            },
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            { "data": "CustomerName", "defaultContent": "", "className": "text-left" },
            { "data": "CustomerTypeName", "defaultContent": "" },
            { "data": "CustomerGroupName", "defaultContent": "" },
            { "data": "ProductServiceNames", "defaultContent": "", "className": "text-left" },
            {
                "data": "ProjectName", "defaultContent": "", "className": "text-left",
                "render": function (data, type, row) {
                    if (type !== "display") return data;
                    var url = buildDetailUrl(row);
                    if (!url) return data || "";
                    // Bấm vào tên để mở chi tiết cơ hội/dự án tương ứng
                    return `<a class="report-detail-link" href="${url}" title="Xem chi tiết">${data || ""}</a>`;
                }
            },
            { "data": "ProjectTypeName", "defaultContent": "" },
            { "data": "OpportunityStatusName", "defaultContent": "" },
            { "data": "ContactPersonInfo", "defaultContent": "", "className": "text-left" },
            {
                "data": "TotalExpectedValue", "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return `<div class="text-right">${new Intl.NumberFormat("vi-VN", {
                        style: "decimal"
                    }).format(data)}</div>`;
                }
            },
            {
                "data": "TotalVNPTValue", "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return `<div class="text-right">${new Intl.NumberFormat("vi-VN", {
                        style: "decimal"
                    }).format(data)}</div>`;
                }
            },
            { "data": "ExecutionTime", "defaultContent": "" },
            {
                "data": "Note", "defaultContent": "", "className": "text-left",
                "render": function (data, type, row, meta) {
                    return `<div class="article-content content-lop-${makeRandomId()}">
                                ${data}
                            </div>`;
                }
            },
        ], drawCallback: function (settings) {
            RutGonNoiDung();
        }
    });
}
//const vnFormatter = new Intl.NumberFormat("vi-VN", {
//    style: "decimal",
//    currency: "VND",
//});

/**
 * Tạo đường dẫn tới màn hình chi tiết theo loại bản ghi của dòng báo cáo.
 */
function buildDetailUrl(row) {
    if (!row || !row.ObjectID) return "";

    if (row.DataType === "Project") {
        return "/Cate/ProjectOverview/Index/" + row.ObjectID;
    }
    if (row.DataType === "BusinessOpportunity") {
        return "/Cate/BusinessOpportunityOverview/Index/" + row.ObjectID;
    }
    return "";
}

function fnExport() {
    window.location = '/Cate/Report_BusinessOpportunity/Export' + "?" + $("#RM_Report_BusinessOpportunitySearch").serialize();
}



/**
* Render nút xem thêm nội dung tin bài.
*/
function RutGonNoiDung() {
    var article_content = $('.article-content');

    for (var i = 0; i < article_content.length; i++) {
        var lastClass = article_content[i].classList[1];
        var ele = $('.' + lastClass);
        //if (ele.height() > (window.innerHeight / 2)) {
        if (ele.height() > 100) {
            ele.addClass('article-short-content');
            ele.after(`<div class="text-center text-primary"><span class="show-full-article-button" onclick="XemDayDu(this,'${lastClass}')"><i class="fa fa-sm fa-angle-double-down"></i> Xem thêm </span></div>`);
        }
    }

    // xử lý đồng bộ style nội dung bài viết
    $('.article-content [style*="font-size"]').css("font-size", ""),
        $('.article-content [style*="line-height"]').css("line-height", "");

    // thay đổi các tag <tt> thành <div> , để ko lỗi font chữ
    // Tìm các phần tử có tên thẻ cần thay đổi
    var elementsTT = $('.article-content tt');
    // Tạo các phần tử mới với tên thẻ và nội dung tương ứng
    for (var i = 0; i < elementsTT.length; i++) {
        var newElement = document.createElement('div');
        newElement.innerHTML = elementsTT[i].innerHTML;
        // Thay thế phần tử cũ bằng phần tử mới
        elementsTT[i].parentNode.replaceChild(newElement, elementsTT[i]);
    }
}
function XemDayDu(e, idLop) {
    $('.' + idLop).removeClass('article-short-content');
    e.classList.add('d-none')
}

function makeRandomId(length = 10) {
    var result = '';
    var characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    for (var i = 0; i < length; i++) {
        result += characters.charAt(Math.floor(Math.random() * characters.length));
    }
    return result;
}
