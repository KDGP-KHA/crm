if ($.fn.datepicker != undefined) {
    $.fn.datepicker.dates["vi"] = {
        days: ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy", "Chủ nhật"],
        daysShort: ["CN", "T2", "T3", "T4", "T5", "T6", "T7", "CN"],
        daysMin: ["CN", "T2", "T3", "T4", "T5", "T6", "T7", "CN"],
        months: [
            "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10",
            "Tháng 11", "Tháng 12"
        ],
        monthsShort: ["Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12"],
        today: "Hôm nay"
    };
    $.fn.datepicker.defaults.language = "vi";
    $.fn.datepicker.defaults.weekStart = 1;
}

$.extend(true,
    $.fn.dataTable.defaults,
    {
        dom:
            "<'row'<'col-12 col-sm-6'l><'col-12 col-sm-6 text-right table-tools-col'f>>" +
                "<'row'<'col-12'tr>>" +
                "<'row'<'col-12 col-md-5'i><'col-12 col-md-7'p>>",
        renderer: "bootstrap",
        language: {
            "lengthMenu": "<span>Hiển thị</span> <b>_MENU_</b> <span >dòng</span>",
            "zeroRecords": "<span>Không tìm thấy</span>",
            "emptyTable": "<span>Không có dữ liệu</span>",
            //"info": "<span>Đang hiển thị trang</span> _PAGE_ <span >trên</span> _PAGES_",
            "info":
                "<span>Hiển thị từ </span> <b>_START_</b> <span> đến </span> <b>_END_</b> <span >trên tổng số </span> <b>_TOTAL_</b> dữ liệu",
            "infoEmpty": "<span>Không có dữ liệu</span>",
            "infoFiltered": "(<span >Lọc từ</span> <b>_MAX_</b> <span >tổng dòng</span>)",
            "search": "<span>Tìm kiếm:</span>",
            "paginate": {
                "first": "<span>Trang đầu</span>",
                "last": "<span>Trang cuối</span>",
                "next": "<span>Tiếp</span>",
                "previous": "<span>Trước</span>"
            },
            "aria": {
                "sortAscending": ": <span >Sắp xếp tăng dần</span>",
                "sortDescending": ": <span >Sắp xếp giảm dần</span>"
            }
        },
        classes: {
            sLength: "dataTables_length text-left w-auto",
        },
        buttons: {
            dom: {
                button: {
                    className: "btn" //remove the default 'btn-secondary'
                },
                container: {
                    className: "dt-buttons btn-group bgc-white-tp2 text-left w-auto"
                }
            },
            buttons: [
                {
                    "extend": "colvis",
                    "text": "<i class='far fa-eye text-110'></i> <span class='d-none'>Show/hide columns</span>",
                    "className": "btn btn-outline-info",
                    columns: ":not(:first)"
                }
            ]
        },
        createdRow: function (row) {
            $(row).addClass("d-style bgc-h-default-l4");
        }
    });

$.fn.select2.defaults.set("amdBase", "select2/");
$.fn.select2.defaults.set("amdLanguageBase", "select2/i18n/");
$.fn.select2.defaults.set("width", "resolve");
$.fn.select2.defaults.set("language", "vi");

if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g,
            function (match, number) {
                return typeof args[number] != "undefined"
                    ? args[number]
                    : match;
            });
    };
}

moment.locale("vi");

$(document)
    .bind("ajaxStart",
        function (event, jqxhr, settings, thrownError) {
            _onWaiting();
        })
    .bind("ajaxComplete",
        function (event, jqxhr, settings, thrownError) {
            _initSelectElement();
            _initDatePicker();
            _initDateTimePicker();
            _endWaiting();
        })
    .bind("ajaxError",
        function (event, jqxhr, settings, thrownError) {
            if (jqxhr.status === 401) {
                window.location.href = window.location.origin + "/Account/Login";
            }
            else if (jqxhr.status === 500) {
                window.location.href = window.location.origin + "/Error/Error";
            }
            //window.location = "/Error/Error";
        });

var htmlModalTemplate =
    '<div class="modal fade {0}" id="{1}" parent-id="{2}" data-keyboard="false" data-backdrop-bg="bgc-grey-tp4" data-backdrop="static" tabindex="-1" role="dialog" aria-labelledby="modalTitle" aria-hidden="true"><div class="modal-dialog" {3} role="document"><div id="modal-content" class="modal-content border-0 shadow radius-1"></div></div></div>';

$(function () {

    var htmlLoader =
        '<div id="loader" class="pageload-overlay" data-opening="M 0,0 80,-10 80,60 0,70 0,0" data-closing="M 0,-10 80,-20 80,-10 0,0 0,-10"><svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" viewBox="0 0 80 60" preserveAspectRatio="none"><path d="M 0,70 80,60 80,80 0,80 0,70"/></svg></div>';
    $("body").append(htmlLoader);

    $("body").append('<div id="ModalContent"></div>');

    $('<audio id="soundSuccess"><source src="/Contents/Base/sound/notify/success.mp3" type="audio/mpeg"></audio>').appendTo('body');
    $('<audio id="soundError"><source src="/Contents/Base/sound/notify/error.mp3" type="audio/mpeg"></audio>').appendTo('body');
    $('<audio id="soundNotify"><source src="/Contents/Base/sound/notify/notify.wav" type="audio/wav"></audio>').appendTo('body');
    $('<audio id="soundModal"><source src="/Contents/Base/sound/notify/modal.wav" type="audio/wav"></audio>').appendTo('body');
    /* 
        Parrams
        - 0: Modal width
        - 1: Modal id
        - 2: Parent id
        modal size 
        - modal-fs: fullscreen
        - modal-lg: large
        - modal-xl: x-large
    */

    $(document, "table", "form").on("click",
        "[data-modal]",
        function () {
            _onWaiting();
            var idModal = "modal_" + $(this).attr("data-modal-id");
            var idParentModal = $(this).attr("data-modal-id-parent");
            if ($("#" + idModal).length > 0) {
                $("#" + idModal).remove();
            }

            var asidePlacement = $(this).attr("data-aside-placement");
            var asideDismiss = $(this).attr("data-aside-dismiss");

            var modalWidthClass = "modal-lg";
            var styleWithModal = '';
            var width = $(this).attr("data-width");
            if (typeof width == "undefined") width = $(this).attr("data_width");
            if (width != undefined) {
                width = width.replace("px", "");
                if (width == "fullscreen") {
                    modalWidthClass = "modal-fs";
                } else if (parseInt(width) == 1024) {
                    modalWidthClass = "modal-xl";
                } else if (parseInt(width) > 1024) {
                    modalWidthClass = "modal-xl";
                    styleWithModal = 'style="max-width: calc(100vw - 5%);"';
                } else if (parseInt(width) < 1024 && parseInt(width) >= 800) {
                    modalWidthClass = "modal-lg";
                }
            }
            var htmlModal = htmlModalTemplate.format(modalWidthClass, idModal, idParentModal, styleWithModal);

            $("#ModalContent").append(htmlModal);
            $("#" + idModal + " #modal-content").load(this.href,
                function (data, textStatus, xhr) {
                    if (xhr.status === 401) {
                        window.location.href = window.location.origin + "/Account/Login";
                    } else if (xhr.status === 404) {
                        window.location.href = window.location.origin + "/Error/NotFound";
                    } else if (xhr.status === 500) {
                        window.location.href = window.location.origin + "/Error/Error";
                    } else if (xhr.status === 405) {
                        window.location.href = window.location.origin + "/Error/AccessDenied";
                    } else {
                        if (_isJson(data)) {
                            var response = JSON.parse(data);
                            if (response.status != undefined) {
                                eval(response.message);
                                $(this).remove();
                            }
                        } else {
                            if (typeof asidePlacement != "undefined") {
                                $("#" + idModal).aceAside({
                                    placement: asidePlacement,
                                    dismiss: asideDismiss,
                                    belowNav: true,
                                    extrwNav: true,
                                    extraClass: 'my-0'
                                });
                                $("#" + idModal + " .modal-dialog").css("max-width", "680px");
                                    //.addClass('w-auto');
                            }
                            $("#" + idModal).modal("show");
                        }
                        _endWaiting();
                    }
                });
            return false;
        });

    $(document, "table", "form").on("click",
        "[data-in-page]",
        function () {
            var formId = $(this).attr("data-form-id");
            if (formId != undefined) {
                _onWaiting();
                $("#" + formId + " #bodyForm").load(this.href,
                    function (data, textStatus, xhr) {
                        if (xhr.status === 401) {
                            window.location.href = window.location.origin + "/Account/Login";
                        } else if (xhr.status === 404) {
                            window.location.href = window.location.origin + "/Error/NotFound";
                        } else if (xhr.status === 500) {
                            window.location.href = window.location.origin + "/Error/Error";
                        } else if (xhr.status === 405) {
                            window.location.href = window.location.origin + "/Error/AccessDenied";
                        } else {
                            if (_isJson(data)) {
                                var response = JSON.parse(data);
                                if (response.status != undefined) {
                                    eval(response.message);
                                    $(this).remove();
                                }
                            } else {
                                $("#modal_" + idModal).modal("show");
                            }
                            _endWaiting();
                        }
                    });
            }
            return false;
        });

    $(document).on({
        'show.bs.modal': function () {
            var zIndex = 1040 + (10 * $(".modal:visible").length);
            $(this).css("z-index", zIndex);
            setTimeout(function () {
                $(".modal-backdrop").not(".modal-stack").css("z-index", zIndex - 1).addClass("modal-stack");
            }, 0);
            //$('#soundModal')[0].play();
        },
        'shown.bs.modal': function () {
            $(this).find("select").each(function () {
                var dropdownParent = $(document.body);
                if ($(this).parents(".modal.in:first").length !== 0)
                    dropdownParent = $(this).parents(".modal.in:first");
            });
            _initElement();
            _initDatePicker();
            $('[data-rel="tooltip"], [data_rel="tooltip"]').tooltip({ trigger: 'hover' });
            $(document).off('focusin.modal');
            if ($(this).find('#modal-content .modal-header').attr('rule') != "Confirm") {
                $('#soundModal')[0].play();
            }
        },
        'hidden.bs.modal': function () {
            if ($(".modal:visible").length > 0) {
                setTimeout(function () {
                    $(document.body).addClass("modal-open");
                },
                    0);
            }
            if ($(this).parent().is("#ModalContent")) {
                $(this).remove();
            }
        }
    },
        ".modal");
});

$(document).ready(function () {
    _initDatePicker();
    _initDateTimePicker();
    _initElement();
    _initValidateForm();

    $('[data-rel="tooltip"], [data_rel="tooltip"]').tooltip({ trigger: 'hover' });
});

window.onerror = function (message, source, lineno, colno, error) {
    toastr.error(message);
    $('#soundError')[0].play();
};

function showNotify(title, icon, message, url, target, type, placement) {
    var clssName = type;

    if (typeof placement === "undefined") placement = "tc";

    $.aceToaster.add({
        placement: placement,
        width: "40rem",
        title: title,
        body: message,
        icon: '<i class="text-' +
            clssName +
            ' mr-2 text-130"><i class="fas ' +
            icon +
            " mt-25 fa-2x text-" +
            clssName +
            '"></i></i>',
        iconClass: "mt-3",
        delay: 5000,
        animation: true,
        //showHideTransition: 'slide',
        showHideTransition: "plain",
        closeClass: "btn btn-light-danger border-0 btn-bgc-tp btn-xs px-2 py-0 text-150 position-tr mt-n25",
        className: "bgc-" + clssName + "-l4 border-none border-t-4 brc-" + clssName + "-tp1 rounded-sm pl-3 pr-1",
        headerClass: "bg-transparent border-0 text-120 text-" + clssName + "-d3 font-bolder mt-3",
        bodyClass: "pt-0 pb-3 text-105 text-" + clssName,
        progress: "position-bl bgc-" + clssName + "-tp4 py-2px m-1px",
        progressReverse: true
    });

    if (type === "success" || type === "info") {
        $('#soundSuccess')[0].play();
    } else if (type === "danger" || type === "warning") {
        $('#soundError')[0].play();
    }
}

function _isJson(str) {
    try {
        return (JSON.parse(str) && !!str);
    } catch (e) {
        return false;
    }
}

$(document).on("init.dt",
    function (e, settings) {
        _initColTable(settings);
    });

$(document).on('draw.dt', function (e, settings, data) {
    $('[data-rel="tooltip"], [data_rel="tooltip"]').tooltip({ trigger: 'hover' });
    $("#" + settings.sTableId + ' tbody td').addClass("align-middle");
});

function _initColTable(settings) {
    if (typeof (settings) == "undefined")
        return;
    var dataTable = $("#" + settings.sTableId);
    $(".table-tools-col > .dataTables_filter").find("input").removeClass("form-control-sm");
    $(".table-tools-col > .dataTables_filter").find("div.dt-buttons").remove();
    $(".table-tools-col > .dataTables_filter")
        .prepend($(dataTable).DataTable().buttons().container().css("margin-right", "5px"));
    $("#" + settings.sTableId + ' tbody td').addClass("align-middle");
}

function _initDatePicker() {
    if ($(".datepicker").length <= 0) return;
    if (!$(".datepicker").hasClass("datepicker-dropdown")) {
        $(".datepicker").datepicker("remove");
        //$(".datepicker").inputmask("dd/mm/yyyy", { "placeholder": "dd/mm/yyyy" });
        $(".datepicker").datepicker({
            autoclose: true,
            format: "dd/mm/yyyy",
            todayhighlight: true,
            orientation: "auto",
            todaybtn: true,
            minDate: null,
            maxDate: null,
            weekStart: 1,
            language: "vi",
            calendarWeeks: true
        });
    }

    $(".input-group > .input-group-addon").on("click",
        function () {
            var datePickerElement = $(this).next();
            if ($(datePickerElement).hasClass("datepicker")) {
                $(datePickerElement).datepicker("show");
            } else {
                $(this).next().trigger("click");
            }
        });
}

function _initDateTimePicker() {
    $('.datetimepicker').datetimepicker({
        locale: 'vi',
        format: 'DD/MM/YYYY HH:mm',
        dayViewHeaderFormat: 'MMMM YYYY',
        calendarWeeks: true,
        showTodayButton: true,
        icons: {
            time: 'fas fa-clock',
            date: 'fas fa-calendar-alt',
            up: 'fas fa-chevron-up',
            down: 'fas fa-chevron-down',
            previous: 'fas fa-chevron-left',
            next: 'fas fa-chevron-right',
            today: 'fas fa-calendar-day',
            clear: 'fas fa-trash',
            close: 'far fa-window-close'
        }
    });
}

function _initElement() {
    $.each($("button[type='submit']"),
        function (idx, btnSubmit) {
            if ($(btnSubmit).parents("form").length == 0) {
                $(btnSubmit).unbind("click");
                $(btnSubmit).on("click",
                    function () {
                        //if ($("[type='submit']").parents('div.modal').find('form').length > 0) {
                        //    $("[type='submit']").parents('div.modal').find('form').submit();
                        //}
                        if ($(this).parents("div.modal").find("form").length > 0) {
                            $(this).parents("div.modal").find("form").submit();
                        }
                    });
            }
        });

    _initSelectElement();
}

function _initSelectElement() {

    $.each($("select"),
        function (idx, ele) {
            var isComboboxLength = !ele.name.includes("_length");
            if (!$(ele).hasClass("none-select2")) {
                if ($(ele).data("select2")) {
                    $(ele).select2("destroy");
                }
                $(ele).select2({
                    allowClear: isComboboxLength,
                    placeholder: "Chọn 1 giá trị",
                    width: "element",
                    dropdownParent: $(this).parent(),
                    dropdownAutoWidth: true,
                    language: "vi"
                });
            }
        });

}

function _initSelect(cbbEle) {
    var ele = $(cbbEle)[0];
    var isComboboxLength = !ele.name.includes("_length");
    if (!$(ele).hasClass("none-select2")) {
        if ($(ele).data("select2")) {
            $(ele).select2("destroy");
        }
        $(ele).select2({
            allowClear: isComboboxLength,
            placeholder: "Chọn 1 giá trị",
            width: "element",
            dropdownParent: $(cbbEle).parent(),
            dropdownAutoWidth: true,
            language: "vi"
        });
    }
}

function _initSelectFromHtml(htmlContent) {
    //var eleSelects = $(htmlContent).find('select');
    $.each($(htmlContent).find('select'), function (idx, ele) {
        var isComboboxLength = !ele.name.includes("_length");
        if (!$(ele).hasClass("none-select2")) {
            if ($(ele).data("select2")) {
                $(ele).select2("destroy");
            }
            $(ele).select2({
                allowClear: isComboboxLength,
                placeholder: "Chọn 1 giá trị",
                width: "element",
                dropdownParent: $(this).parent(),
                dropdownAutoWidth: true,
                language: "vi"
            });
        }
    });
}

function _initMenuView() {
    var url = window.location.href;

    $("#sidebar ul.nav li.nav-item a").each(function () {
        if (this.href.toString().replace("#", "").toLowerCase() === (url.toLowerCase().toString())) {
            var name = $(this).attr("name");
            if (name != undefined) {
                var ids = name.split(",");
                var i;
                for (i = 0; i < ids.length; i++) {
                    $("#" + ids[i]).addClass("active");
                }
            }
        }
    });
}

function _renderButton(isModal, modalId, eleClass, urlAction, icon, title, modalWidth, attrs) {
    var attrWidth = modalWidth != null && modalWidth !== undefined ? 'data-width="{0}"'.format(modalWidth) : "";

    var tmpUrlAction = urlAction;
    var idxParram = tmpUrlAction.indexOf("?");
    if (idxParram > -1) {
        tmpUrlAction = tmpUrlAction.substring(0, idxParram);
    }

    var pActions = tmpUrlAction != null ? tmpUrlAction.split("/") : [];
    pActions = pActions.filter(function (v) { return v !== "" });
    var id = pActions.join("");

    var controllerName = pActions.length > 2 ? pActions[1] : pActions[0];
    var areaName = pActions.length > 2 ? pActions[0] : null;
    var dataParrams = {
        controllerName: controllerName,
        actionName: pActions.length > 2 ? pActions[2] : pActions[1],
        areaName: areaName
    };

    $.ajax({
        type: "GET",
        async: true,
        url: areaName == null ? "/{0}/IsPermit".format(controllerName) : "/{0}/{1}/IsPermit".format(areaName, controllerName),
        dataType: "JSON",
        data: dataParrams,
        success: function (response) {
            if (!response.status) {
                $("a#" + id).remove();
            }
        }
    });

    var template =
        '<a {7} id={6} {8} {0} data-modal-id="{1}" class="{2}" data-rel="tooltip" title="{5}" href="{3}">{4}</a>';
    var sModal = isModal ? 'data-modal=""' : "";
    return template.format(sModal, modalId, eleClass, urlAction, icon, title, id, attrWidth, attrs);
}

function _initValidateForm() {
    if ($("form").length <= 0) return;
    $("form").validate({
        errorClass: "help-block animation-slideDown",
        errorElement: "div",
        errorPlacement: function (error, e) {
            e.parents(".form-group > div").append(error);
        },
        highlight: function (e) {
            $(e).closest(".form-group").removeClass("has-success has-error").addClass("has-error");
            $(e).closest(".help-block").remove();
        },
        success: function (e) {
            e.closest(".form-group").removeClass("has-success has-error");
            e.closest(".help-block").remove();
        }
    });

    var dataValidate = window.mvcClientValidationMetadata == undefined
        ? undefined
        : window.mvcClientValidationMetadata[0];
    if (typeof dataValidate != "undefined") {
        var dataFields = dataValidate.Fields;
        dataFields.forEach(function (item, idx) {
            var validateRules = {};
            var validateMessages = {};

            item.ValidationRules.forEach(function (itemRule) {
                switch (itemRule.ValidationType) {
                    case "required":
                        {
                            validateRules[itemRule.ValidationType] = true;
                            validateMessages[itemRule.ValidationType] = itemRule.ErrorMessage;
                        }
                        break;
                    case "url":
                        {
                            validateRules[itemRule.ValidationType] = true;
                            validateMessages[itemRule.ValidationType] = itemRule.ErrorMessage;
                        }
                        break;
                    case "date":
                        {
                            validateRules[itemRule.ValidationType] = true;
                            validateMessages[itemRule.ValidationType] = itemRule.ErrorMessage;
                        }
                        break;
                    case "equalto":
                        {
                            validateRules["equalTo"] = itemRule.ValidationParameters.other.replace("*.", "#");
                            validateMessages["equalTo"] = itemRule.ErrorMessage;
                        }
                        break;
                    case "length":
                        {
                            var paramsLength = itemRule.ValidationParameters;
                            if (typeof paramsLength.min !== "undefined") {
                                validateRules["minlength"] = paramsLength.min;
                                validateMessages["minlength"] = itemRule.ErrorMessage;
                            }
                            if (typeof paramsLength.max !== "undefined") {
                                validateRules["maxlength"] = paramsLength.max;
                                validateMessages["maxlength"] = itemRule.ErrorMessage;
                            }
                        }
                        break;
                    case "range":
                        {
                            var paramsRange = itemRule.ValidationParameters;
                            validateRules[itemRule.ValidationType] = [paramsRange.min, paramsRange.max];
                            validateMessages[itemRule.ValidationType] = itemRule.ErrorMessage;
                        }
                        break;
                    default:
                        {
                            validateRules[itemRule.ValidationType] = true;
                            validateMessages[itemRule.ValidationType] = itemRule.ErrorMessage;
                        }
                        break;
                }
            });

            $("form").validate().settings.rules[item.FieldName] = validateRules;
            $("form").validate().settings.messages[item.FieldName] = validateMessages;
        });
    }
}

function _showPassword(inpt) {
    if ("password" == $(inpt).attr("type")) {
        $(inpt).prop("type", "text");
    } else {
        $(inpt).prop("type", "password");
    }
}

function _onWaiting() {
    $("#loader").addClass("show pageload-loading").css("display", "inherit");
}

function _endWaiting() {
    $("#loader").addClass("show pageload-loading").css("display", "none");
}

function _onChangeCombo(cbb, eleName, isOptgroup) {
    if (isOptgroup) {
        $(eleName).val($(cbb).children("optgroup").children("option:selected").text());
    } else {
        $(eleName).val($(cbb).children("option:selected").text());
    }
}

function _onDownloadDoc(btnDownloadDoc) {
    if (typeof btnDownloadDoc != "undefined") {
        var url = $(btnDownloadDoc).data("href");
        $.ajax({
            type: "GET",
            url: url,
            success: function (response) {
                if (typeof response != "undefined" && response != null && response.status) {
                    window.location = response.downloadPath;
                } else if (typeof response != "undefined" && response != null && !response.status) {
                    eval(response.message);
                    return false;
                }
            }
        });
    }
}

function _showModal(modalHtml, modalId, modalWidth, parentModalId) {
    _onWaiting();
    if ($("#" + modalId).length > 0) {
        $("#" + modalId).remove();
    }
    var modalWidthClass = "modal-lg";
    if (typeof modalWidth == "undefined") modalWidth = "860";
    if (modalWidth != undefined) {
        modalWidth = modalWidth.replace("px", "");
        if (modalWidth == "fullscreen") {
            modalWidthClass = "modal-fs";
        } else if (parseInt(modalWidth) >= 1024) {
            modalWidthClass = "modal-xl";
        } else if (parseInt(modalWidth) < 1024 && parseInt(width) >= 800) {
            modalWidthClass = "modal-lg";
        }
    }

    var htmlModal = htmlModalTemplate.format(modalWidthClass, modalId, parentModalId);
    $("#ModalContent").append(htmlModal);
    $("#" + modalId + " #modal-content").html(modalHtml);
    $("#" + modalId).modal("show");
    _endWaiting();
}

function _initValidForm(formId, arrIgnore) {
    var $invalidClass = 'brc-danger-tp2'
    var $validClass = 'brc-info-tp2'
    var $errorClass = 'form-text form-error text-danger-m2'

    var $formValid = $(formId).validate({
        errorElement: 'span',
        errorClass: $errorClass,
        focusInvalid: false,
        ignore: arrIgnore != undefined ? arrIgnore.join(',') : "",
        rules: {
            //    email: {
            //        required: true,
            //        email: true
            //    },
            //    password: {
            //        required: true,
            //        minlength: 5
            //    },
            //    password2: {
            //        required: true,
            //        minlength: 5,
            //        equalTo: "#password"
            //    },
            //    name: {
            //        required: true
            //    },
            //    phone: {
            //        required: true,
            //        phone: 'required'
            //    },
            //    url: {
            //        required: true,
            //        url: true
            //    },
            //    comment: {
            //        //required: true
            //    },
            //    state: {
            //        //required: true
            //    },
            //    platform: {
            //        required: true
            //    },
            //    subscription: {
            //        required: true
            //    },
            //    gender: {
            //        required: true,
            //    },
            //    agree: {
            //        required: true,
            //    }
        },

        errorPlacement: function (error, element) {
            // prepend 'fa-exclamation-circle' icon
            error.prepend('<i class="form-text fa fa-exclamation-circle text-danger-m1 text-100 mr-1 ml-2"></i>')

            if (element.is('input[type=checkbox]') || element.is('input[type=radio]')) {
                element.closest('div[class*="col-"]').append(error)
            }
                //else if (element.is('.select2')) {
            else if (element.data('select2')) {
                var container = element.siblings('[class*="select2-container"]')
                error.insertAfter(container)
                container.find('.select2-selection').addClass($invalidClass)
            }
                //else if (element.is('.chosen')) {
            else if (element.data('chosen')) {
                var container = element.siblings('[class*="chosen-container"]')
                error.insertAfter(container)
                container.find('.chosen-choices, .chosen-single').addClass($invalidClass)
            }
            else {
                error.addClass('d-inline-block').insertAfter(element)
            }

            element.focus(function () {
                element.removeClass($errorClass);
            });
        },

        submitHandler: function (form) {
        }
    });

    return $formValid;
}

function XuLy_VietHoa_VietThuong(NameInput) {
    // Lấy giá trị của input
    var Input_Value = $("[name='" + NameInput + "']:not(span)");
    var currentVal = Input_Value.val();

    // Tạo input cho viết hoa
    var inputVietHoa = document.createElement("input");
    inputVietHoa.type = "hidden";
    inputVietHoa.name = NameInput + "_Hoa";
    inputVietHoa.value = currentVal.charAt(0).toUpperCase() + currentVal.slice(1);

    // Tạo input cho viết thường
    var inputVietThuong = document.createElement("input");
    inputVietThuong.type = "hidden";
    inputVietThuong.name = NameInput + "_Thuong";
    inputVietThuong.value = currentVal.charAt(0).toLowerCase() + currentVal.slice(1);

    // Lấy thẻ form
    var formElement = document.getElementById("VietHoa_VietThuong");

    // Thêm inputVietHoa và inputVietThuong vào form
    formElement.appendChild(inputVietHoa);
    formElement.appendChild(inputVietThuong);
}