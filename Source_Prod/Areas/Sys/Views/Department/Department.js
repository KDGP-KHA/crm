var _DepartmentActionURLs = {
    Department_GetData: "/Sys/Department/Get"
};

var _tableDepartment = null;

$(document).ready(function () {
    if (!_tableDepartment) {
        initTableDepartment();
    }
    initExport();
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

                    return '<div class="text-left" style="padding-left:' + padding + 'px; font-weight:' + (level === 0 ? 700 : 400) + '">' +
                           (data || "") +
                           '</div>';
                }
            },
            {
                data: "MaBoPhan",
                defaultContent: "",
                render: function (data, type, row, meta) {
                    return data || "";
                }
            },
            {
                data: "BoPhan_ID",
                defaultContent: "",
                orderable: false,
                className: "text-center",
                render: function (data, type, row, meta) {
                    var html = '<span>';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
                        html += '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';
                        html += _renderButton(true, "EditDepartment", "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item", "/Sys/Department/Edit/" + data, '<i class="far fa-edit text-primary text-120 mr-1"></i> Cáº­p nháº­t', "Cáº­p nháº­t", "800px");
                        html += _renderButton(true, "DeleteDepartment", "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item", "/Sys/Department/Delete/" + data, '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> XoÃ¡', "XoÃ¡", "600px");
                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function Department_OnProcessSuccess(response, formId) {
    if (response && response.status !== undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.message) {
                try { eval(response.message); } catch (e) { }
            }
            if (_tableDepartment) {
                _tableDepartment.ajax.reload(null, false);
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
                        if (_tableDepartment) {
                            _tableDepartment.ajax.reload(null, false);
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
        searchDepartment();
    }
});

function searchDepartment() {
    if (_tableDepartment) {
        _tableDepartment.ajax.reload(null, false);
    }
}

function initExport() {
    $(document).off("click", "#btnExportDeparment").on("click", "#btnExportDeparment", function () {
        var baseUrl = "/Sys/Department/Export";
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
                '<div style="font-size:16px;font-weight:600;color:#1F4E79;margin-bottom:6px;">Äang xuáº¥t dá»¯ liá»‡u...</div>' +
                '<div style="font-size:13px;color:#888;">Vui lÃ²ng chá», khÃ´ng Ä‘Ã³ng trÃ¬nh duyá»‡t</div>' +
                '</div></div>'
            );
        }
        $("#exportDepartmentOverlay").css("display", "flex");
        $btn.prop("disabled", true).find("span").text("Äang táº£i...");
    } else {
        $("#exportDepartmentOverlay").hide();
        $btn.prop("disabled", false).find("span").text("Táº£i danh sÃ¡ch Bá»™ pháº­n");
    }
}