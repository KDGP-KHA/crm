var _BusinessOpportunityActionURLs = {
    BusinessOpportunity_GetData: "/Cate/RM_BusinessOpportunity/Get"
};
var _tableBusinessOpportunity;

$(document).ready(function () {
    initTableBusinessOpportunity();

    $(document).on('click', '.toggle-more', function () {
        var row = $(this).closest('td');
        var shortText = row.find('.short-text');
        var fullText = row.find('.full-text');
        if (fullText.hasClass('d-none')) {
            shortText.hide();
            fullText.removeClass('d-none');
            $(this).text('Ẩn bớt');
        } else {
            shortText.show();
            fullText.addClass('d-none');
            $(this).text('Xem thêm');
        }
    });
});

function SearchRM() {
    _tableBusinessOpportunity.ajax.reload(null, true);
}

function initTableBusinessOpportunity() {
    _tableBusinessOpportunity = $("#DSBusinessOpportunity").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "ordering": false,
        "searching": false,
        "dom": '<"dt-top">t<"dt-bottom d-flex justify-content-between align-items-center"i<"dt-right-group d-flex align-items-center"l p>>',
        "order": [[1, "desc"]],
        "ajax": {
            "url": _BusinessOpportunityActionURLs.BusinessOpportunity_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "ProductServiceID": function () { return $("#SearchBusinessOpportunity #ProductServiceID").val(); },
                "StatusID": function () { return $("#SearchBusinessOpportunity #StatusID").val(); },
                "BoPhanID": function () { return $("#SearchBusinessOpportunity #BoPhanID").val(); },
                "EmployeeID": function () { return $("#SearchBusinessOpportunity #EmployeeID").val(); },
                "Keyword": function () { return $("#SearchBusinessOpportunity #Keyword").val(); },
                "FromDate": function () { return $("#SearchBusinessOpportunity #FromDate").val(); },
                "ToDate": function () { return $("#SearchBusinessOpportunity #ToDate").val(); },
                "CustomerID": function () { return $("#SearchBusinessOpportunity #CustomerID").val(); },
                "SuccessRateFrom": function () { return $("#SearchBusinessOpportunity #SuccessRateFrom").val(); },
                "SuccessRateTo": function () { return $("#SearchBusinessOpportunity #SuccessRateTo").val(); }
            }
        },
        "columns": [
            {
                "data": "",
                "defaultContent": "1",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    return meta.settings._iDisplayStart + meta.row + 1;
                }
            },
            {
                "data": null,
                "className": "text-left",
                "render": function (data, type, row) {

                    let allServices = row.ProductServiceNames || "";

                    let arr = allServices
                        ? allServices.split(";")
                        : [];

                    let services = arr.slice(0, 2).join(", ");
                    let more = arr.length > 2 ? "..." : "";

                    let closingHtml = "";
                    if (row.ClosingProbability != null) {
                        let colorClass =
                            row.ClosingProbability > 50 ? 'text-success' :
                                row.ClosingProbability <= 20 ? 'text-danger' : 'text-warning';

                        closingHtml = ` | Xác suất chốt: <strong class="${colorClass}">${row.ClosingProbability}%</strong>`;
                    }

                    return `
                        <div>
                            <span class="badge ${row.StatusClass}">${row.StatusName}</span>
                        </div>
                        <a href="/Cate/BusinessOpportunityOverview/Index/${row.BusinessOpportunityID}" class="font-weight-bold text-primary">
                            [${row.CodeOpportunity}] ${row.OpportunityName}
                        </a>
                        <div class="text-danger small">
                            <i class="fa fa-building mr-1"></i>${row.CustomerName || ""}
                        </div>

                        <div style="font-size:11px" title="${allServices.replace(/"/g, '&quot;')}">
                            <span class="font-weight-bold">Dịch vụ</span>: ${services}${more}
                        </div>

                        <div style="font-size:11px; white-space: nowrap;">
                            ${row.ExpectedValue != null ? `Giá trị dự kiến: <span class="text-success font-weight-bold">${row.ExpectedValue.toLocaleString()} triệu</span>` : ""}
                            ${closingHtml}
                        </div>
                    `;
                }
            },
            {
                "data": "Members",
                "defaultContent": "",
                "className": "text-left",
                "width": 150,
                "orderable": false,
                "render": function (data) {
                    if (!data?.length) return "";

                    const sorted = [...data].sort((a, b) => {
                        const aIsAM = a.Roles?.some(r => r.RoleName === "AM");
                        const bIsAM = b.Roles?.some(r => r.RoleName === "AM");
                        return (bIsAM === true) - (aIsAM === true);
                    });

                    const top = sorted.slice(0, 4);

                    let html = top.map(x => {
                        const isAM = x.Roles?.some(r => r.RoleName === "AM");

                        const roles = x.Roles?.map(r => r.RoleName).join(", ") || "";

                        return `
                            <div style="font-size:11px; white-space: nowrap;"
                                 class="${isAM ? 'text-danger font-weight-bold' : ''}">
                                ${x.Employee_Name}
                                <span>(${roles})</span>
                            </div>
                        `;
                        }).join("");

                        if (sorted.length > 4) {
                            html += `
                            <div class="text-muted" style="font-size:11px">
                                +${sorted.length - 4} more
                            </div>
                        `;
                    }

                    return `<div title="${sorted.map(x =>
                                        x.Employee_Name + ' (' + x.Roles.map(r => r.RoleName).join(', ') + ')'
                                        ).join('\n')}">
                                ${html}
                            </div>`;
                }
            },
            {
                "data": "Description",
                "className": "text-left",
                "defaultContent": "",
                "width": 350,
                "render": function (data, type, row) {
                    if (!data) return "";
                    let decoded = $("<textarea/>").html(data).text();
                    let desText = decoded.replace(/<.*?>/g, "");

                    let shortDesText = desText.length > 150
                        ? desText.substring(0, 150) + "..."
                        : desText;

                    let hasImage = data.includes("<img");
                    let html = `<div class="col-12 exchange-wrapper" style="font-size:12px;">`;
                    if (desText) {
                        html += `<span class="preview-content text-secondary">${shortDesText}</span>`;
                    }
                    if (hasImage) {
                        html += _renderButton(
                            true,
                            "ViewContentBusinessOpportunity",
                            "",
                            "/Cate/RM_BusinessOpportunity/ViewContent/" + row.BusinessOpportunityID,
                            'Xem nội dung', 'Xem nội dung'
                        );
                    }
                    if (desText.length > 100) {
                        html += _renderButton(true,
                            "ViewContentBusinessOpportunity",
                            "",
                            "/Cate/RM_BusinessOpportunity/ViewContent/" + row.BusinessOpportunityID,
                            'Xem thêm','Xem thêm'
                            );
                    }
                    return html;
                }
            },
            {
                "data": null,
                "className": "text-left",
                "width": 210,
                "render": function (data, type, row) {
                    let plainText = row.ExchangeContent
                        ? row.ExchangeContent.replace(/<[^>]*>?/gm, '') : "";
                    let shortText = plainText.length > 120
                        ? plainText.substring(0, 100) + "..." : plainText;
                    let dateStr = "";
                    if (row.ExchangeDate) {
                        let m = moment(row.ExchangeDate);
                        dateStr = m.format("HH:mm") === "00:00"
                            ? m.format("DD/MM/YYYY")
                            : m.format("DD/MM/YYYY HH:mm");
                    }
                    return `
                        ${row.ExchangeDate ? `
                            <div class="small text-muted" style="white-space: nowrap;"><i class="far fa-clock mr-1"></i>${dateStr}</div>
                            <div class="small text-primary font-weight-bold" style="white-space: nowrap;">
                                <i class="fa fa-user mr-1"></i>${row.FullName || ""}
                            </div>` : ''}
                        ${row.ContactPersonName ? `
                            <div class="small text-primary font-weight-bold" style="white-space: nowrap;">
                                <i class="fas fa-phone-square mr-1"></i>${row.ContactPersonName}
                            </div>` : ''}
                        ${shortText ? `<div class="small mt-1">Nội dung: ${shortText}</div>` : ''}
                    `;
                }
            },
            {
                "data": "BusinessOpportunityID",
                "defaultContent": "",
                "orderable": false,
                "render": function (data, type, row) {
                    var html = '<span>';
                    if (type === "display") {
                        html += '<div class="dropdown d-inline-block">';
                        html += '<button class="btn btn-lighter-primary mr-1 dropdown-toggle" type="button"'
                            + ' data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">'
                            + '<i class="fa fa-ellipsis-h text-120"></i></button>';
                        html += '<div class="dropdown-menu dropdown-menu-right">';

                        // CHI TIẾT CƠ HỘI → BusinessOpportunityOverview/Index
                        html += `<a class="btn btn-lighter-secondary btn-a-outline-secondary dropdown-item"
                                    href="/Cate/BusinessOpportunityOverview/Index/${data}">
                                    <i class="fas fa-eye text-secondary text-120 mr-1"></i> Chi tiết cơ hội
                                 </a>`;

                        // Xoá
                        if (row.CanDelete) {
                            html += _renderButton(true,
                                "DeleteBusinessOpportunity",
                                "btn btn-lighter-danger mr-1 btn-a-outline-danger dropdown-item",
                                "/Cate/RM_BusinessOpportunity/Delete/" + data,
                                '<i class="far fa-trash-alt text-danger text-120 mr-1"></i> Xoá',
                                "Xoá");
                        }

                        // Thêm thành viên
                        html += _renderButton(true,
                            "ListSalesTeamMembers",
                            "btn btn-lighter-primary mr-1 btn-a-outline-primary dropdown-item",
                            "/Cate/SalesTeamMembers/List/" + data,
                            '<i class="fas fa-list text-primary text-120 mr-1"></i> Thêm thành viên',
                            "Thêm thành viên", 1024);

                        html += '</div></div>';
                    }
                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function BusinessOpportunity_OnProcessSuccess(response, formId) {
    if (response && response.projectId) {
        window.open("/Cate/ProjectOverview/Index/" + response.projectId, "_blank");
    }
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                if (typeof _tableBusinessOpportunity !== "undefined" && _tableBusinessOpportunity) {
                    _tableBusinessOpportunity.ajax.reload(null, false);
                }
                if (typeof reloadBoDetail === "function") reloadBoDetail();
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                    _initElement && _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    if (typeof _tableBusinessOpportunity !== "undefined" && _tableBusinessOpportunity) {
                        _tableBusinessOpportunity.ajax.reload(null, false);
                    }
                    if (typeof reloadBoDetail === "function") reloadBoDetail();
                    if (typeof reloadBoMembers === "function") reloadBoMembers();
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

$(document).on('click', '.toggle-content', function () {
    const wrapper = $(this).closest('.exchange-wrapper');
    const contentDiv = wrapper.find('.exchange-content');
    const preview = wrapper.find('.preview-content');

    if (contentDiv.is(':animated')) return;

    const isHidden = contentDiv.is(':hidden');

    contentDiv.slideToggle(200);
    preview.toggle(!isHidden);

    $(this).text(isHidden ? 'Ẩn bớt' : 'Xem thêm');
});

function ensureBusinessOpportunityFilePreviewModal() {
    if ($("#businessOpportunityFilePreviewModal").length) {
        return;
    }

    var modalHtml = ''
        + '<div class="modal fade" id="businessOpportunityFilePreviewModal" tabindex="-1" role="dialog" aria-hidden="true">'
        + '  <div class="modal-dialog modal-xl modal-dialog-centered" role="document" style="max-width: 1080px;">'
        + '    <div class="modal-content">'
        + '      <div class="modal-header py-2 bgc-primary-tp1 border-0 radius-t-1">'
        + '        <h5 class="modal-title text-white-tp1 text-110 pl-2 font-bolder my-auto" id="businessOpportunityFilePreviewTitle">'
        + '          <i class="fas fa-file-alt"></i>&nbsp;Xem tệp đính kèm'
        + '        </h5>'
        + '        <button type="button" class="btn btn-outline-white btn-h-danger btn-a-danger mt-1px mr-1px btn-brc-tp" data-dismiss="modal" aria-label="Đóng" title="Đóng">'
        + '          <i class="fas fa-times"></i>'
        + '        </button>'
        + '      </div>'
        + '      <div class="modal-body p-3">'
        + '        <div id="businessOpportunityFilePreviewFallback" class="alert alert-warning mb-0 d-none"></div>'
        + '        <iframe id="businessOpportunityFilePreviewFrame"'
        + '                style="width:100%; height:72vh; border:1px solid #e3e7ed; border-radius:4px; background:#fff;"'
        + '                frameborder="0"></iframe>'
        + '      </div>'
        + '      <div class="modal-footer">'
        + '        <button type="button" class="btn px-4 btn-outline-danger mb-1" data-dismiss="modal">'
        + '          <i class="fas fa-times"></i>&nbsp;Đóng'
        + '        </button>'
        + '      </div>'
        + '    </div>'
        + '  </div>'
        + '</div>';

    $("body").append(modalHtml);
}

function getBusinessOpportunityFileExtension(filePath, fileName) {
    var source = fileName || filePath || "";
    var cleanSource = source.split("?")[0].split("#")[0];
    var dotIndex = cleanSource.lastIndexOf(".");

    if (dotIndex < 0) {
        return "";
    }

    return cleanSource.substring(dotIndex).toLowerCase();
}

function toBusinessOpportunityAbsoluteUrl(filePath) {
    if (!filePath) {
        return "";
    }

    if (/^https?:\/\//i.test(filePath)) {
        return filePath;
    }

    var link = document.createElement("a");
    link.href = filePath;
    return link.href;
}

function buildBusinessOpportunityPreviewUrl(filePath, fileName) {
    var absoluteUrl = toBusinessOpportunityAbsoluteUrl(filePath);
    var extension = getBusinessOpportunityFileExtension(filePath, fileName);
    var officeExtensions = [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

    if (officeExtensions.indexOf(extension) >= 0) {
        return "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(absoluteUrl);
    }

    return absoluteUrl;
}

function isBusinessOpportunityOfficeFile(filePath, fileName) {
    var extension = getBusinessOpportunityFileExtension(filePath, fileName);
    var officeExtensions = [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

    return officeExtensions.indexOf(extension) >= 0;
}

function canUseBusinessOpportunityOfficeViewer(filePath) {
    var absoluteUrl = toBusinessOpportunityAbsoluteUrl(filePath);
    var link = document.createElement("a");
    link.href = absoluteUrl;

    if (!/^https?:$/i.test(link.protocol)) {
        return false;
    }

    var hostName = (link.hostname || "").toLowerCase();

    if (!hostName || hostName === "localhost" || hostName === "127.0.0.1") {
        return false;
    }

    if (/^\d+\.\d+\.\d+\.\d+$/.test(hostName)) {
        if (/^(10|127)\./.test(hostName)) {
            return false;
        }

        if (/^192\.168\./.test(hostName)) {
            return false;
        }

        if (/^172\.(1[6-9]|2\d|3[0-1])\./.test(hostName)) {
            return false;
        }
    }

    return true;
}

function showBusinessOpportunityFilePreview(filePath, fileName, title) {
    ensureBusinessOpportunityFilePreviewModal();

    var previewUrl = buildBusinessOpportunityPreviewUrl(filePath, fileName);
    var absoluteUrl = toBusinessOpportunityAbsoluteUrl(filePath);
    var extension = getBusinessOpportunityFileExtension(filePath, fileName);
    var supportedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".txt", ".csv", ".log", ".htm", ".html", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];
    var canPreview = supportedExtensions.indexOf(extension) >= 0;
    var isOfficeFile = isBusinessOpportunityOfficeFile(filePath, fileName);
    var canUseOfficeViewer = canUseBusinessOpportunityOfficeViewer(filePath);
    var displayTitle = title || fileName || "Xem tệp đính kèm";
    var $modal = $("#businessOpportunityFilePreviewModal");
    var $frame = $("#businessOpportunityFilePreviewFrame");
    var $fallback = $("#businessOpportunityFilePreviewFallback");

    $("#businessOpportunityFilePreviewTitle").html('<i class="fas fa-file-alt"></i>&nbsp;' + displayTitle);

    if (canPreview && (!isOfficeFile || canUseOfficeViewer)) {
        $fallback.addClass("d-none").empty();
        $frame.removeClass("d-none").attr("src", previewUrl);
    } else if (isOfficeFile) {
        $frame.addClass("d-none").attr("src", "about:blank");
        $fallback
            .removeClass("d-none")
            .html(
                '<div class="font-weight-bold mb-2">Không thể xem trước trực tiếp file Office trong popup.</div>'
                + '<div class="small text-muted mb-2">Viewer của Microsoft chỉ hoạt động khi URL file có thể truy cập công khai.</div>'
                + '<div class="small text-muted mb-3">' + (fileName || filePath) + '</div>'
                + '<a href="' + absoluteUrl + '" target="_blank" class="btn btn-outline-primary">'
                + '<i class="fas fa-external-link-alt mr-1"></i>Mở tệp'
                + '</a>');
    } else {
        $frame.addClass("d-none").attr("src", "about:blank");
        $fallback
            .removeClass("d-none")
            .html(
                '<div class="font-weight-bold mb-2">Không hỗ trợ xem trước trực tiếp cho định dạng này.</div>'
                + '<div class="small text-muted mb-3">' + (fileName || filePath) + '</div>'
                + '<a href="' + absoluteUrl + '" target="_blank" class="btn btn-outline-primary">'
                + '<i class="fas fa-external-link-alt mr-1"></i>Mở tệp'
                + '</a>');
    }

    $modal.modal("show");
}

function openBusinessOpportunityFilePreviewLink(element) {
    var $link = $(element);
    var filePath = $link.data("filePath") || $link.attr("href");
    var fileName = $link.data("fileName") || $.trim($link.text());
    var title = $link.data("previewTitle") || "Xem tệp đính kèm";

    showBusinessOpportunityFilePreview(filePath, fileName, title);
    return false;
}

$(document).on("click", ".js-file-preview", function (e) {
    e.preventDefault();
    openBusinessOpportunityFilePreviewLink(this);
});

$(document).on("hidden.bs.modal", "#businessOpportunityFilePreviewModal", function () {
    $("#businessOpportunityFilePreviewFrame").attr("src", "about:blank").removeClass("d-none");
    $("#businessOpportunityFilePreviewFallback").addClass("d-none").empty();
});
// ── EXPORT ──────────────────────────────────────────────────────────────────
$(document).on('click', '#btnExportBusinessOpportunity', function () {
    var baseUrl = '/Cate/RM_BusinessOpportunity/Export';

    var keyword = $('#SearchBusinessOpportunity #Keyword').val() || '';
    var statusID = $('#SearchBusinessOpportunity #StatusID').val() || '';
    var productServiceID = $('#SearchBusinessOpportunity #ProductServiceID').val() || '';
    var fromDate = $('#SearchBusinessOpportunity #FromDate').val() || '';
    var toDate = $('#SearchBusinessOpportunity #ToDate').val() || '';
    var customerID = $('#SearchBusinessOpportunity #CustomerID').val() || '';

    var qs = [];
    if (keyword) qs.push('keyword=' + encodeURIComponent(keyword));
    if (statusID) qs.push('statusID=' + encodeURIComponent(statusID));
    if (productServiceID) qs.push('productServiceID=' + encodeURIComponent(productServiceID));
    if (fromDate) qs.push('fromDate=' + encodeURIComponent(fromDate));
    if (toDate) qs.push('toDate=' + encodeURIComponent(toDate));
    if (customerID) qs.push('customerID=' + encodeURIComponent(customerID));

    window.location.href = baseUrl + (qs.length ? '?' + qs.join('&') : '');
});
