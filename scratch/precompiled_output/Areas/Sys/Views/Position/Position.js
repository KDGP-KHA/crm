var _PositionsActionURLs = {
    Positions_GetData: "/Sys/Position/Get"
    };
var _tablePositions;
$(document).ready(function () {
    if (!_tablePositions) {
        initTablePositions();
    }
});
function initTablePositions() {
    _tablePositions = $('#tblPosition').DataTable({
        "responsive": false,
        "lengthChange": true,
        "processing": true,
        "searching": false,
        "serverSide": true,
        "dom": '<"dt-top">t<"dt-bottom d-flex justify-content-between align-items-center"i<"dt-right-group d-flex align-items-center"l p>>',
        "ajax": {
            "url": _PositionsActionURLs.Positions_GetData,
            "type": "POST",
            "dataType": "JSON",
            data: function (d) {
                d.search.value = $('#Keyword').val();
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
                "data": "MaChucVu",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            },
            {
                "data": "TenChucVu",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data;
                }
            }
        ]
    });
}
$('#Keyword').on('keypress', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        searchPosition();
    }
});
function searchPosition() {
    _tablePositions.ajax.reload(null, false);
}
$(document).ready(function () {
    initExport();
});
function initExport() {
    $(document).on("click", "#btnExportPosition", function () {
        var baseUrl = "/sys/Position/Export";
        var keyword = $("#SearchDeparment #Keyword").val() || "";

        var qs = [];
        if (keyword) qs.push("keyword=" + encodeURIComponent(keyword));

        var cookieName = "expPos_" + Date.now();
        qs.push("cookieName=" + cookieName);

        showExportOverlay(true);

        var $iframe = $("<iframe>").hide().appendTo("body");
        $iframe.attr("src", baseUrl + (qs.length ? "?" + qs.join("&") : ""));

        var checkTimer = setInterval(function () {
            if (document.cookie.indexOf(cookieName + "=done") !== -1) {
                clearInterval(checkTimer);
                document.cookie = cookieName + "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                $iframe.remove();
                showExportOverlay(false);
            }
        }, 500);
        setTimeout(function () {
            clearInterval(checkTimer);
            $iframe.remove();
            showExportOverlay(false);
        }, 300000);
    });
}

function showExportOverlay(show) {
    var $btn = $("#btnExportPosition");
    if (show) {
        if ($("#exportPositonOverlay").length === 0) {
            $("body").append(
                '<div id="exportPositonOverlay" style="display:none;position:fixed;top:0;left:0;' +
                'width:100%;height:100%;background:rgba(0,0,0,0.45);z-index:99999;' +
                'align-items:center;justify-content:center;flex-direction:column;">' +
                '<div style="background:#fff;border-radius:10px;padding:32px 40px;text-align:center;' +
                'box-shadow:0 8px 32px rgba(0,0,0,0.18);">' +
                '<i class="fa fa-spinner fa-spin fa-3x text-primary mb-3" style="display:block;"></i>' +
                '<div style="font-size:16px;font-weight:600;color:#1F4E79;margin-bottom:6px;">Đang xuất dữ liệu...</div>' +
                '<div style="font-size:13px;color:#888;">Vui lòng chờ, không đóng trình duyệt</div>' +
                '</div></div>'
            );
        }
        $("#exportPositonOverlay").css("display", "flex");
        $btn.prop("disabled", true).find("span").text("Đang tải...");
    } else {
        $("#exportPositonOverlay").hide();
        $btn.prop("disabled", false).find("span").text("Tải danh sách Chức vụ");
    }
}