var _DepartmentActionURLs = {
    Department_GetData: "/sys/Department/Get"
};

var _tableDepartment = null;

$(document).ready(function () {
    if (!_tableDepartment) {
        initTableDepartment();
    }
});

function initTableDepartment() {
    _tableDepartment = $('#tblDepartment').DataTable({
        processing: true,
        serverSide: false,
        searching: false,
        ordering: false,
        paging: false,
        ajax: {
            url: _DepartmentActionURLs.Department_GetData,
            type: "POST",
            dataSrc: "data",
            data: function (d) {
                d.search = $('#Keyword').val();
            }
        },
        columns: [
            {
                data: "TenBoPhan",
                class: "text-left",
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
            { data: "MaBoPhan" },
        ]
    });
};

$('#Keyword').on('keypress', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        searchDepartment();
    }
});

function searchDepartment() {
    _tableDepartment.ajax.reload(null, false);
}

$(document).ready(function () {
    initExport();
});

function initExport() {
    $(document).on("click", "#btnExportDeparment", function () {
        var baseUrl = "/sys/Department/Export";
        var keyword = $("#SearchDeparment #Keyword").val() || "";

        var qs = [];
        if (keyword) qs.push("keyword=" + encodeURIComponent(keyword));

        var cookieName = "expDep_" + Date.now();
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
    var $btn = $("#btnExportDeparment");
    if (show) {
        if ($("#exportDepartmentOverlay").length === 0) {
            $("body").append(
                '<div id="exportDepartmentOverlay" style="display:none;position:fixed;top:0;left:0;' +
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
        $("#exportDepartmentOverlay").css("display", "flex");
        $btn.prop("disabled", true).find("span").text("Đang tải...");
    } else {
        $("#exportDepartmentOverlay").hide();
        $btn.prop("disabled", false).find("span").text("Tải danh sách Bộ phận");
    }
}