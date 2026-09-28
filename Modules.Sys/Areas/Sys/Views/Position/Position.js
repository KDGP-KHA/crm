var _PositionsActionURLs = {
    Positions_GetData: "/Sys/Position/Get"
};
var _tablePositions;

$(document).ready(function () {
    if (!_tablePositions) {
        initTablePositions();
    }
    initExport();
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
            "data": function (d) {
                d.search = { value: $('#Keyword').val() };
            }
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "orderable": false,
                "className": "text-center",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "MaChucVu",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data || "";
                }
            },
            {
                "data": "TenChucVu",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data || "";
                }
            },
            {
                "data": "ChucVu_ID",
                "defaultContent": "",
                "orderable": false,
                "className": "text-center",
                "render": function (data, type, row, meta) {
                    var html = '<span>';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
                        html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';
                        html += _renderButton(true, "EditPosition", "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item", "/Sys/Position/Edit/" + data, '<i class="far fa-edit text-primary text-120 mr-1"></i> Cập nhật', "Cập nhật", "800px");
                        html += _renderButton(true, "DeletePosition", "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item", "/Sys/Position/Delete/" + data, '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá', "Xoá", "600px");
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function Position_OnProcessSuccess(response, formId) {
    if (response && response.status !== undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.message) {
                try { eval(response.message); } catch (e) { }
            }
            if (_tablePositions) {
                _tablePositions.ajax.reload(null, false);
            }
            response.status = undefined;
            var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
            $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                if (typeof _initElement === "function") _initElement();
            });
        } else {
            if (!response.status && response.errorCode === 1) {
                if (response.message) {
                    try { eval(response.message); } catch (e) { }
                }
                response.status = undefined;
            } else {
                $("#ModalContent #modal_" + formId).modal("hide");
                $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                    if (response && response.status !== undefined) {
                        if (response.message) {
                            try { eval(response.message); } catch (e) { }
                        }
                        if (_tablePositions) {
                            _tablePositions.ajax.reload(null, false);
                        }
                        response.status = undefined;
                    }
                });
            }
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

$('#Keyword').on('keypress', function (e) {
    if (e.which === 13) {
        e.preventDefault();
        searchPosition();
    }
});

function searchPosition() {
    if (_tablePositions) {
        _tablePositions.ajax.reload(null, false);
    }
}

function initExport() {
    $(document).off("click", "#btnExportPosition").on("click", "#btnExportPosition", function () {
        var baseUrl = "/Sys/Position/Export";
        var keyword = $("#Keyword").val() || "";

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
    if (show) {
        if ($("#exportPositonOverlay").length === 0) {
            $("body").append(
                '<div id="exportPositonOverlay" style="display:none;position:fixed;top:0;left:0;' +
                'width:100%;height:100%;background:rgba(0,0,0,0.35);z-index:99999;' +
                'align-items:center;justify-content:center;">' +
                '<div style="background:#fff;padding:24px 36px;border-radius:8px;' +
                'box-shadow:0 4px 20px rgba(0,0,0,0.25);text-align:center;font-size:15px;' +
                'font-weight:600;color:#1F4E79;">' +
                '<div class="spinner-border text-primary mb-2" role="status" style="width:2.5rem;height:2.5rem;">' +
                '<span class="sr-only">Loading...</span></div>' +
                '<div>Đang xuất danh sách chức vụ...</div>' +
                '<div style="font-size:12px;color:#888;margin-top:6px;">Vui lòng đợi trong giây lát</div>' +
                '</div></div>'
            );
        }
        $("#exportPositonOverlay").css("display", "flex").hide().fadeIn(150);
    } else {
        $("#exportPositonOverlay").fadeOut(150, function () { $(this).remove(); });
    }
}