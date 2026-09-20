/**
 * CenIT TOC CRM - Shared Document Management JavaScript
 * Module: Quáº£n lÃ½ tÃ i liá»‡u chung (Sys/SharedDocument)
 */

var _SharedDocActionURLs = {
    GetData: "/Sys/SharedDocument/Get"
};
var _tableSharedDocument;

$(document).ready(function () {
    initTableSharedDocument();
    normalizeSharedDocumentSearchText();

    // Khá»Ÿi táº¡o datepicker chuáº©n tiáº¿ng Viá»‡t
    if ($.fn.datepicker) {
        $('.datepicker').datepicker({
            format: 'dd/mm/yyyy',
            autoclose: true,
            todayHighlight: true,
            language: 'vi'
        });
    }

    // Báº¯t phÃ­m Enter trong Search Card
    $('#SearchSharedDocument').on('keydown', 'input, select', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            SearchSharedDoc();
        }
    });

    // Lá»c tá»± Ä‘á»™ng khi thay Ä‘á»•i dropdown danh má»¥c
    $('#CategoryId_Search').on('change', function () {
        SearchSharedDoc();
    });
});

function normalizeSharedDocumentSearchText() {
    $("#SearchSharedDocument label, #SearchSharedDocument .card-title, #SearchSharedDocument button").each(function () {
        var $item = $(this);
        $item.contents().filter(function () { return this.nodeType === 3; }).each(function () {
            this.nodeValue = normalizeVietnameseText(this.nodeValue);
        });
    });
    $("#SearchSharedDocument input").each(function () {
        var $input = $(this);
        $input.attr("placeholder", normalizeVietnameseText($input.attr("placeholder")));
    });
}

function SearchSharedDoc() {
    if (_tableSharedDocument) {
        _tableSharedDocument.ajax.reload(null, true);
    }
}

function ResetSearchSharedDoc() {
    $('#Keyword_Search').val('');
    $('#CategoryId_Search').val('0');
    $('#FromDate_Search').val('');
    $('#ToDate_Search').val('');
    SearchSharedDoc();
}

function _docRenderButton(hasPerm, modalId, cssClass, url, iconHtml, title, dataWidth) {
    if (typeof _renderButton === "function") {
        return _renderButton(hasPerm, modalId, cssClass, url, iconHtml, title, dataWidth);
    }
    if (!hasPerm) return "";
    var widthAttr = dataWidth ? ' data-width="' + dataWidth + 'px"' : '';
    return '<a href="' + url + '" class="' + cssClass + '" data-modal="true" data-modal-id="' + modalId + '"' + widthAttr + ' title="' + title + '">' + iconHtml + '</a>';
}

function escapeHtml(text) {
    if (!text) return "";
    text = normalizeVietnameseText(text);
    var map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

function formatSharedDocumentDate(value) {
    if (!value) return "";

    var millisecondsMatch = /\/Date\((-?\d+)/.exec(value.toString());
    var date = millisecondsMatch ? new Date(parseInt(millisecondsMatch[1], 10)) : new Date(value);
    if (isNaN(date.getTime())) return value.toString();

    function pad(number) { return number < 10 ? "0" + number : number; }
    return pad(date.getDate()) + "/" + pad(date.getMonth() + 1) + "/" + date.getFullYear() +
        " " + pad(date.getHours()) + ":" + pad(date.getMinutes());
}

function initTableSharedDocument() {
    _tableSharedDocument = $("#DSSharedDocument").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": false,
        "searching": false,
        "dom": '<"dt-top">t<"dt-bottom d-flex justify-content-between align-items-center"i<"dt-right-group d-flex align-items-center"l p>>',
        "ajax": {
            "url": _SharedDocActionURLs.GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.Keyword = $("#Keyword_Search").val();
                d.CategoryId = $("#CategoryId_Search").val();
                d.FromDate = $("#FromDate_Search").val();
                d.ToDate = $("#ToDate_Search").val();
            }
        },
        "columns": [
            {
                "data": null,
                "className": "text-center",
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": "DocumentName",
                "className": "text-left",
                "render": function (data, type, row) {
                    var safeName = escapeHtml(data || "");
                    var safeDesc = row.Description ? '<div class="text-secondary text-85 mt-1">' + escapeHtml(row.Description) + '</div>' : '';
                    return '<div class="font-bold text-primary-d2 text-95">' + safeName + '</div>' + safeDesc;
                }
            },
            {
                "data": "CategoryName",
                "className": "text-left",
                "render": function (data) {
                    var safeCategory = escapeHtml(data || "");
                    return '<span class="badge bgc-primary-l2 text-primary-d2 border-1 brc-primary-m3 px-2 py-1 radius-1">' + safeCategory + '</span>';
                }
            },
            {
                "data": "FileName",
                "className": "text-left",
                "render": function (data, type, row) {
                    var ext = (row.FileExtension || "").toLowerCase();
                    var icon = 'fa-file-alt text-secondary';
                    if (ext.indexOf('doc') >= 0) icon = 'fa-file-word text-blue';
                    else if (ext.indexOf('xls') >= 0) icon = 'fa-file-excel text-success';
                    else if (ext.indexOf('pdf') >= 0) icon = 'fa-file-pdf text-danger';
                    else if (ext.indexOf('ppt') >= 0) icon = 'fa-file-powerpoint text-orange';

                    var safeFileName = escapeHtml(data || "");
                    var safeSize = escapeHtml(row.FileSizeFormatted || "");

                    return '<div class="d-flex align-items-center">' +
                           '<i class="fa ' + icon + ' text-120 mr-2"></i>' +
                           '<div><div class="text-truncate shared-doc-file-cell" title="' + safeFileName + '">' + safeFileName + '</div>' +
                           '<small class="text-muted">' + safeSize + '</small></div>' +
                           '</div>';
                }
            },
            {
                "data": "DownloadCount",
                "className": "text-center",
                "render": function (data) {
                    return '<span class="badge badge-sm badge-pill bgc-secondary-l1 text-secondary-d2 font-bold px-2">' + (data || 0) + '</span>';
                }
            },
            {
                "data": "CreatedBy",
                "className": "text-left",
                "render": function (data, type, row) {
                    var safeUploader = escapeHtml(data || "");
                    var safeDate = escapeHtml(formatSharedDocumentDate(row.CreatedDateFormatted || row.CreatedDate));
                    return '<div><i class="fa fa-user text-grey-m1 mr-1"></i><span class="font-bold">' + safeUploader + '</span></div>' +
                           '<div class="text-secondary text-85"><i class="far fa-clock mr-1"></i>' + safeDate + '</div>';
                }
            },
            {
                "data": "DocumentId",
                "className": "text-center",
                "render": function (data, type, row) {
                    var html = '<div class="d-inline-flex">';

                    // 1. NÃºt Táº£i vá» (ToÃ n bá»™ ngÆ°á»i dÃ¹ng)
                    html += '<a href="/Sys/SharedDocument/Download/' + data + '" class="btn btn-xs btn-lighter-success mr-1 p-1" title="Táº£i vá»" target="_blank">' +
                            '<i class="fa fa-download text-success text-100"></i></a>';

                    // 2. NÃºt Xem chi tiáº¿t (ToÃ n bá»™ ngÆ°á»i dÃ¹ng)
                    html += _docRenderButton(true, "DetailSharedDocument", "btn btn-xs btn-lighter-info mr-1 p-1",
                        "/Sys/SharedDocument/Detail/" + data,
                        '<i class="fa fa-info-circle text-info text-100"></i>', "Chi tiáº¿t", 800);

                    // 3. NÃºt Sá»­a (Chá»‰ hiá»ƒn thá»‹ khi row.CanEdit == true)
                    if (row.CanEdit) {
                        html += _docRenderButton(true, "EditSharedDocument", "btn btn-xs btn-lighter-primary mr-1 p-1",
                            "/Sys/SharedDocument/Edit/" + data,
                            '<i class="fa fa-pencil-alt text-primary text-100"></i>', "Chá»‰nh sá»­a", 800);
                    }

                    // 4. NÃºt XÃ³a (Chá»‰ hiá»ƒn thá»‹ khi row.CanDelete == true)
                    if (row.CanDelete) {
                        html += _docRenderButton(true, "DeleteSharedDocument", "btn btn-xs btn-lighter-danger p-1",
                            "/Sys/SharedDocument/Delete/" + data,
                            '<i class="fa fa-trash-alt text-danger text-100"></i>', "XÃ³a", 500);
                    }

                    html += '</div>';
                    return html;
                }
            }
        ],
        "drawCallback": function () {
            $("#DSSharedDocument a[href*='/SharedDocument/Download/']").attr("title", "Tải về");
            $("#DSSharedDocument [data-modal-id='DetailSharedDocument']").attr("title", "Chi tiết");
            $("#DSSharedDocument [data-modal-id='EditSharedDocument']").attr("title", "Chỉnh sửa");
            $("#DSSharedDocument [data-modal-id='DeleteSharedDocument']").attr("title", "Xóa");
            $("#DSSharedDocument [title]").each(function () {
                var $item = $(this);
                $item.attr("title", normalizeVietnameseText($item.attr("title")));
            });
        }
    });
}

function SharedDocument_OnProcessSuccess(response, formId) {
    if (response && response.status != undefined) {
        // Ká»‹ch báº£n A: NgÆ°á»i dÃ¹ng chá»n "Tiáº¿p tá»¥c táº¡o má»›i" (#chkNotDismissModal)
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            eval(response.message);
            if (typeof _tableSharedDocument !== "undefined" && _tableSharedDocument) {
                _tableSharedDocument.ajax.reload(null, false);
            }
            response.status = undefined;
            var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
            $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                if (typeof _initElement === "function") _initElement();
            });
        }
        // Ká»‹ch báº£n B: ÄÃ³ng modal chuáº©n má»±c & an toÃ n vÃ²ng Ä‘á»i
        else {
            if (!response.status && response.errorCode == 1) {
                eval(response.message); // BÃ¡o lá»—i ngay vÃ  GIá»® NGUYÃŠN modal
                response.status = undefined;
            } else {
                $("#ModalContent #modal_" + formId).modal("hide");

                // Äá»¢I MODAL áº¨N XONG HOÃ€N TOÃ€N Má»šI KÃCH HOáº T THÃ”NG BÃO VÃ€ RELOAD Báº¢NG (CHá»NG Káº¸T BACKDROP)
                $("#ModalContent #modal_" + formId).one("hidden.bs.modal", function () {
                    if (response.status != undefined) {
                        eval(response.message);
                        if (typeof _tableSharedDocument !== "undefined" && _tableSharedDocument) {
                            _tableSharedDocument.ajax.reload(null, false);
                        }
                        response.status = undefined;
                    }
                });
            }
        }
    }
    // Ká»‹ch báº£n C: Server tráº£ vá» HTML (Validation tháº¥t báº¡i ModelState.IsValid == false)
    else {
        var $bodyForm = $("#ModalContent #modal_" + formId + " #bodyForm_" + (formId.indexOf("Add") >= 0 ? "Add" : "Edit"));
        if (!$bodyForm.length) {
            $bodyForm = $("#ModalContent #modal_" + formId + " .bodyForm, #ModalContent #modal_" + formId + " #bodyForm");
        }
        $bodyForm.html(response);
    }
}

function normalizeVietnameseText(text) {
    var value = text == null ? "" : text.toString();
    if (!/[ÃÂ]/.test(value)) return value;
    try {
        return decodeURIComponent(escape(value));
    } catch (e) {
        return value;
    }
}
