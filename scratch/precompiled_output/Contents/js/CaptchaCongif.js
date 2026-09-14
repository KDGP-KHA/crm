
/**
 * Cách dùng.
 * Trog view form thêm đoạn này dưới js : 
 *   $(document).ready(function () {
            CreateCaptcharView('XacThucDatChuongTrinhSanPham', 'DatChuongTrinhSanPham');
        });
 * Trong đó: 
 *          + XacThucDatChuongTrinhSanPham là tên Class chứa ô nhập captcha
 *          + DatChuongTrinhSanPham: tên Cha chứa class trên, có thể là class của form 
 */


/**
 * Tạo view nhập Captchar vào div này
 * @param {any} className
 */
function CreateCaptcharView(className, classParrentName, colSize = null) {
    colSize = (colSize == null) ? 4 : colSize;
    var strHtml = `<div class="row align-items-center">
                        <div class="col-${colSize}">
                            <input class="form-control w-100" id="${className}_XacThucCaptcha" />
                        </div>
                        <div class="form-inline col-${colSize} px-0">
                            <div id="CaptchaCode" class="text-center w-100 col-8 bgc-secondary text-140 text-white py-1 px-1"></div>
                            <span class="btn fa fa-lg fa-undo text-primary" onclick="GenerateCaptcha('${className}')" title="Làm mới Captcha"></span>
                        </div>
                    </div>
                    <div><span id="ThongBaoXacThuc" class="text-danger"></span></div>
                    `;
    $('.' + classParrentName + ' .' + className).html(strHtml);
    GenerateCaptcha(className);
    $('.' + className + ' #CaptchaCode').on('copy', function (event) { event.preventDefault(); }); // không cho copy
    $('.' + className + ' #CaptchaCode').on('selectstart', function (event) { event.preventDefault(); }); // không cho bôi đen
}

/**
 * 
 * Kiểm tra Captcha
 * @param {any} className
 * @returns
 */
function CaptchaIsValid(className) {
    debugger;
    $('.' + className + ' #ThongBaoXacThuc').text('');
    var isValid = $('.' + className + ' input#' + className + '_XacThucCaptcha').val() == $('.' + className + ' #CaptchaCode').text();
    if (!isValid) {
        GenerateCaptcha(className);
        $('.' + className + ' #ThongBaoXacThuc').text('Xác thực Captcha thất bại');
    }
    return isValid;
}

/**
 * Tạo mã captchar
 * @param {any} className
 */
function GenerateCaptcha(className) {
    var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXTZabcdefghiklmnopqrstuvwxyz";
    var captchaLength = 6;
    var captcha = '';

    for (var i = 0; i < captchaLength; i++) {
        var randomIndex = Math.floor(Math.random() * chars.length);
        captcha += chars.substring(randomIndex, randomIndex + 1);
    }
    $('.' + className + ' #CaptchaCode').text(captcha);
}