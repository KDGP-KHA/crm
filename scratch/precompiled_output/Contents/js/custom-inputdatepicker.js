
///// cấu hình lại datepicker: begin /////
$.fn.datepicker.dates["vi"] = {
    days: ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy", "Chủ nhật"],
    daysShort: ["CN", "T2", "T3", "T4", "T5", "T6", "T7", "CN"],
    daysMin: ["CN", "T2", "T3", "T4", "T5", "T6", "T7", "CN"],
    months: ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12"],
    monthsShort: ["Th01", "Th02", "Th03", "Th04", "Th05", "Th06", "Th07", "Th08", "Th09", "Th10", "Th11", "Th12"],
    today: "Hôm nay"
};


function df_datepicker(classPath) {
    $(classPath).datepicker({
        viewMode: "days",
        minViewMode: "days",
        format: "dd/MM/yyyy",
        autoclose: true,
        todayhighlight: true,
        todaybtn: true,
        minDate: null,
        maxDate: null,
        weekStart: 1,
        orientation: "bottom",
        language: "vi"
    })
}


function df_datetimepicker(classPath) {

    ////////////////////////
    // Datetimepicker plugin
    $(classPath).datetimepicker({
        icons: {
            time: 'far fa-clock text-green-d1 text-120',
            date: 'far fa-calendar text-blue-d1 text-120',

            up: 'fa fa-chevron-up text-secondary',
            down: 'fa fa-chevron-down text-secondary',
            previous: 'fa fa-chevron-left text-secondary',
            next: 'fa fa-chevron-right text-secondary',

            today: 'far fa-calendar-check text-purple-d1 text-120',
            clear: 'fa fa-trash-alt text-orange-d2 text-120',
            close: 'fa fa-times text-danger text-120'
        },

        locale: "vi",
        // sideBySide: true,

        toolbarPlacement: "top",

        allowInputToggle: true,
        // showClose: true,
        // showClear: true,
        showTodayButton: true,

        format: 'DD/MM/YYYY hh:mm A' // Định dạng là "DD/MM/YYYY hh:mm TT"
       
    })

    //***** NOTE *******//
    // the above `date/time` picker plugin was designed for BS3.
    // To make it work with BS4, the following piece of code is required
    $(classPath)
        .on('dp.show', function () {
            $(this).find('.collapse.in').addClass('show')
            $(this).find('.table-condensed').addClass('table table-borderless')

            $(this).find('[data-action][title]').tooltip() // enable tooltip
        })
}


// now listen to the `.collapse` events inside this datetimepicker accordion (one `.collapse` is for timepicker, the other one is for datepicker)
// then add or remove the old `in` BS3 class so the plugin works correctly
$(document)
    .on('show.bs.collapse', '.bootstrap-datetimepicker-widget .collapse', function () {
        $(this).addClass('in')
    })
    .on('hide.bs.collapse', '.bootstrap-datetimepicker-widget .collapse', function () {
        $(this).removeClass('in')
    })





/// Custom input mask decimal
function df_decimal_inputmask(classPath) {
    $(classPath).inputmask({
        alias: "decimal",
        digits: 10,
        allowMinus: false,
        autoGroup: true,
        //groupSeparator: ",",
        rightAlign: false
    });

    // alias: "decimal": Định nghĩa kiểu mask là số thập phân.
    // digits: 10: Chỉ định số chữ số sau dấu phẩy.
    // allowMinus: false: Không cho phép nhập dấu trừ.
    // autoGroup: true: Tự động thêm dấu phân cách phần nghìn.
    // groupSeparator: ",": Sử dụng dấu phẩy làm dấu phân cách phần nghìn.
    // rightAlign: false: Căn trái trường input.
}