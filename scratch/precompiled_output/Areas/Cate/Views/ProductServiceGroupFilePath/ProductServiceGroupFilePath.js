
var _tableProductServiceGroupFilePath;
$(document).ready(function () {
    initTableProductServiceGroupFilePath();
});

function initTableProductServiceGroupFilePath() {
    if (!$("#DSProductServiceGroupFilePath").length) return;
    if ($.fn.DataTable.isDataTable("#DSProductServiceGroupFilePath")) {
        _tableProductServiceGroupFilePath = $("#DSProductServiceGroupFilePath").DataTable();
        return;
    }

    _tableProductServiceGroupFilePath = $("#DSProductServiceGroupFilePath").DataTable({
        "responsive": true,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "paging": false,
        "ajax": {
            "url": "/Cate/ProductServiceGroupFilePath/Get",
            "type": "POST",
            "dataType": "JSON",
            "data": {
                "ProductServiceID": _ProductServiceID,
                "Search": function () { return $('#searchTaiLieu #Search').val(); },
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
                "data": "Title",
                "className": "text-left w-25",
                "defaultContent": "", "render": function (data, type, row, meta) {
                    return `<div class="article-content content-lop-${makeRandomId()}">
                                      ${data ?? ''}
                                   </div>`;
                }
            },
            {
                "data": "FilePaths",
                "className": "text-left w-25",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return `<div class="change-info text-grey" data-id="${row.GroupFilePathID}">
                                        <i class="fas fa-lg fa-spinner icon text-primary-l1"></i>
                                        Xin chờ...
                                    </div>`;
                }
            },
            {
                "data": "Note",
                "className": "text-left w-25",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return `<div class="article-content content-lop-${makeRandomId()}">
                                      ${data ?? ''}
                                  </div>`;
                }

            },
            {
                "data": "UserUpdated",
                "defaultContent": "",
                "render": function (data, type, row, meta) {
                    return data + `<div>${moment(row.DateUpdated).format("DD/MM/YYYY HH:mm:ss")}</div>`;
                }
            },
            {
                "data": "GroupFilePathID",
                //"style": "width:100px;",
                "className": "text-nowrap",
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var html = '<span class="">';
                    if (type === "display") {
                        //html += _renderButton(true,
                        //    "DetailProductServiceGroupFilePath",
                        //    "btn btn-lighter-secondary mr-1",
                        //    "/Cate/ProductServiceGroupFilePath/Detail/" + data,
                        //    '<i class="far fa-eye text-secondary text-120"></i>',
                        //    "Xem chi tiết");

                        html += _renderButton(true,
                            "EditProductServiceGroupFilePath",
                            "btn btn-lighter-primary mr-1",
                            "/Cate/ProductServiceGroupFilePath/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật", 1024);

                        html += _renderButton(true,
                            "DeleteProductServiceGroupFilePath",
                            "btn btn-lighter-danger mr-1",
                            "/Cate/ProductServiceGroupFilePath/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xoá");
                    }
                    html += "</span>";
                    return html;
                }
            }
        ], drawCallback: function (settings) {
            setTimeout(() => {
                RutGonNoiDung();
            }, 1000);

            var api = this.api();
            $('.change-info').each(function () {
                var el = $(this);
                var id = el.data('id');
                if (el.data('loaded')) return;
                $.get(`/Cate/ProductServiceGroupFilePath/GetFilePathsByGroupFilePathID?id=${id}&mode=VIEW`, function (html) {
                    el.html(html);
                    el.data('loaded', true);
                });


            });

        }
    });
}

function ProductServiceGroupFilePath_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                _tableProductServiceGroupFilePath.ajax.reload(null, false);
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    _tableProductServiceGroupFilePath.ajax.reload(null, false);
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}
function reloadCKEditorForProductServiceGroupFilePath(listIDInputs) {
    listIDInputs.forEach((element, index) => {
        eval(`var editor = CKEDITOR.instances['${element}'];
                   if (editor) { editor.destroy(true); }
                   editor = CKEDITOR.replace('${element}', {
                       height: 250,
                       filebrowserWindowWidth: '1000',
                       filebrowserWindowHeight: '350',
                       allowedContent: true,
                        extraAllowedContent: 'img[width,height,style]',
                        toolbar: [
                            { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline'] },
                            { name: 'paragraph', items: ['NumberedList', 'BulletedList'] },
                            { name: 'insert', items: ['Image', 'Table', 'Link'] },
                            { name: 'styles', items: ['Format'] },
                            { name: 'tools', items: ['Maximize'] }
                        ]
                   });
                   editor.on('change', function (e) {
                       $("#${element}").val(editor.getData());
                   });`);
    });
}

// ── CKEditor ──────────────────────────────────────────
function initTaskManagementCKEditor() {
    if (typeof CKEDITOR === 'undefined') return;
    var ex = CKEDITOR.instances['ContentDescriptionComment'];
    if (ex) ex.destroy(true);
    CKEDITOR.replace('ContentDescriptionComment', {
        height: 100,
        allowedContent: true,
        extraAllowedContent: 'img[width,height,style]',
        toolbar: [
            { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline'] },
            { name: 'paragraph', items: ['NumberedList', 'BulletedList'] },
            { name: 'insert', items: ['Image', 'Table', 'Link'] },
            { name: 'styles', items: ['Format'] },
            { name: 'tools', items: ['Maximize'] }
        ]
    });
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
        if (ele.height() > 50) {
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
