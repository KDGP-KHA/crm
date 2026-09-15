function applyEmailFormat($input) {
    $input = $($input);
    if ($input.data("email-bound")) return;
    $input.data("email-bound", true);
    $input
        .attr("placeholder", "example@domain.com")
        .attr("maxlength", "150")
        .on("blur", function () {
            var val = $(this).val().trim();
            $(this).val(val);
            if (val === "") { $(this).removeClass("is-invalid is-valid"); removeFieldError($(this)); return; }
            if (validateEmail(val)) {
                $(this).removeClass("is-invalid").addClass("is-valid");
                removeFieldError($(this));
            } else {
                $(this).removeClass("is-valid").addClass("is-invalid");
                showFieldError($(this), "Email không hợp lệ. Định dạng: ten@domain.com");
            }
        })
        .on("input", function () {
            $(this).removeClass("is-invalid is-valid");
            removeFieldError($(this));
        });
}

function validateEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email);
}

function applyPercentFormat($input) {
    $input = $($input);
    if ($input.data("percent-bound")) return;
    $input.data("percent-bound", true);
    $input
        .attr("type", "number")
        .attr("min", "0")
        .attr("max", "100")
        .attr("step", "0.01")
        .attr("placeholder", "0 – 100")
        .on("blur", function () {
            var val = $(this).val();
            if (val === "") return;
            var num = parseFloat(val);
            if (isNaN(num) || num < 0 || num > 100) {
                $(this).val("");
                $(this).addClass("is-invalid");
                showFieldError($(this), "Tỷ lệ phải từ 0 đến 100.");
            } else {
                $(this).removeClass("is-invalid").addClass("is-valid");
                removeFieldError($(this));
            }
        })
        .on("input", function () {
            $(this).removeClass("is-invalid is-valid");
            removeFieldError($(this));
        });
}

function applyPhoneFormat($input) {
    $input = $($input);
    if ($input.data("phone-bound")) return;
    $input.data("phone-bound", true);
    $input
        .attr("type", "tel")
        .attr("inputmode", "numeric")
        .attr("maxlength", "15")
        .attr("placeholder", "VD: 0901234567")
        .css("width", "160px")
        .on("input", function () {
            $(this).val($(this).val().replace(/[^0-9+]/g, ""));
        })
        .on("blur", function () {
            var val = $(this).val().trim();
            if (val === "") { $(this).removeClass("is-invalid is-valid"); removeFieldError($(this)); return; }
            if (validatePhoneVN(val)) {
                $(this).removeClass("is-invalid").addClass("is-valid");
                removeFieldError($(this));
            } else {
                $(this).removeClass("is-valid").addClass("is-invalid");
                showFieldError($(this), "Số di động không hợp lệ. VD: 0901234567 hoặc +84901234567");
            }
        });
}

function validatePhoneVN(phone) {
    var p = phone.replace(/\s+/g, "");
    if (p.startsWith("+84")) p = "0" + p.slice(3);
    if (p.startsWith("84") && p.length === 11) p = "0" + p.slice(2);
    return /^(0(3[2-9]|5[25689]|7[06-9]|8[0-9]|9[0-9]))\d{7}$/.test(p);
}

function applyLandlineFormat($input) {
    $input = $($input);
    if ($input.data("landline-bound")) return;
    $input.data("landline-bound", true);
    $input
        .attr("type", "tel")
        .attr("inputmode", "numeric")
        .attr("maxlength", "15")
        .attr("placeholder", "028xxxxxxx")
        .css("width", "160px")
        .on("input", function () {
            $(this).val($(this).val().replace(/[^0-9+\-\s()]/g, ""));
        })
        .on("blur", function () {
            var val = $(this).val().trim();
            if (val === "") { $(this).removeClass("is-invalid is-valid"); removeFieldError($(this)); return; }
            if (validateLandlineVN(val)) {
                $(this).removeClass("is-invalid").addClass("is-valid");
                removeFieldError($(this));
            } else {
                $(this).removeClass("is-valid").addClass("is-invalid");
                showFieldError($(this), "Số điện thoại bàn không hợp lệ. VD: 028xxxxxxx hoặc 0123xxxxxx");
            }
        });
}

// Validate số điện thoại bàn Việt Nam:
function validateLandlineVN(phone) {
    var p = phone.replace(/[\s\-().]/g, "");
    if (p.startsWith("+84")) p = "0" + p.slice(3);
    if (/^84\d{9,10}$/.test(p)) p = "0" + p.slice(2);
    if (/^02[0-9]\d{7}$/.test(p)) return true;
    if (/^0[2-9][0-9]{2}\d{6}$/.test(p)) return true;
    return false;
}

function showFieldError($input, message) {
    removeFieldError($input);
    $input.after($('<div class="invalid-feedback d-block"></div>').text(message));
}

function removeFieldError($input) {
    $input.next(".invalid-feedback").remove();
}

function applyNumberFormat($input) {
    $input = $($input);
    if ($input.data("number-bound")) return;
    $input.data("number-bound", true);

    var name = $input.attr("name");

    // Tạo hidden input để submit giá trị raw (không format), display input chỉ để hiển thị
    var $hidden = $('<input type="hidden">').attr("name", name);
    $input.attr("name", name + "_display").after($hidden);

    // Load giá trị ban đầu (Edit form): set hidden = raw, display = formatted
    var initVal = $input.val();
    if (initVal !== "") {
        var initNum = parseFloat(initVal);
        if (!isNaN(initNum)) {
            $hidden.val(initVal);
            $input.val(_numberFormat(initVal));
        }
    }

    $input
        .attr("type", "text")
        .attr("inputmode", "decimal")
        .attr("autocomplete", "off")
        // Khi focus: hiển thị raw để người dùng edit tự do (vd: 13312.5 thay vì 13.312,5)
        .on("focus", function () {
            var raw = $hidden.val();
            if (raw !== "") $(this).val(raw);
        })
        // Chặn ký tự không hợp lệ; chỉ cho nhập 1 dấu thập phân (. hoặc ,)
        .on("keydown", function (e) {
            var allowed = ["Backspace", "Delete", "Tab", "ArrowLeft", "ArrowRight", "Home", "End"];
            if (allowed.indexOf(e.key) !== -1) return;
            if (e.ctrlKey || e.metaKey) return;
            if (e.key === "." || e.key === ",") {
                var cur = $(this).val();
                // Từ chối nếu đã có dấu thập phân rồi
                if (cur.indexOf(".") !== -1 || cur.indexOf(",") !== -1) {
                    e.preventDefault();
                }
                return;
            }
            if (!/^\d$/.test(e.key)) e.preventDefault();
        })
        // Khi gõ: chỉ cập nhật hidden, KHÔNG reformat để tránh mất cursor và chặn nhập thập phân
        .on("input", function () {
            $hidden.val(_getRaw($(this).val()));
        })
        // Khi blur: lúc này mới format đẹp để hiển thị
        .on("blur", function () {
            var raw = _getRaw($(this).val());
            $hidden.val(raw);
            $(this).val(raw !== "" ? _numberFormat(raw) : "");
        });
}

// Chuyển giá trị display về raw number string (dùng "." làm thập phân)
function _getRaw(displayVal) {
    var val = String(displayVal).trim();
    var dotCount = (val.match(/\./g) || []).length;
    var commaCount = (val.match(/,/g) || []).length;

    if (commaCount === 1 && dotCount >= 1) {
        // Dạng vi-VN đã format: dấu "." là phân cách nghìn, dấu "," là thập phân
        return val.replace(/\./g, "").replace(",", ".");
    } else if (dotCount === 1 && commaCount === 0) {
        // Dạng nhập thẳng với dấu "." là thập phân
        return val.replace(/[^0-9.]/g, "");
    } else if (commaCount === 1 && dotCount === 0) {
        // Dạng nhập với dấu "," là thập phân (không có dấu nghìn)
        return val.replace(",", ".");
    } else {
        // Số nguyên hoặc có nhiều dấu "." (phân cách nghìn, không có thập phân)
        return val.replace(/[^0-9]/g, "");
    }
}

// Format raw number string → chuỗi hiển thị vi-VN
//  "13312.5" → "13.312,5"
function _numberFormat(raw) {
    if (raw === "" || raw === undefined || raw === null) return "";
    var parts = String(raw).split(".");
    var intPart = parseInt(parts[0], 10);
    if (isNaN(intPart)) return "";
    // toLocaleString vi-VN dùng "." làm phân cách nghìn
    var result = intPart.toLocaleString("vi-VN");
    // Gắn phần thập phân với dấu ","
    if (parts.length > 1 && parts[1] !== "") {
        result += "," + parts[1];
    }
    return result;
}

function initFormUtils(scope) {
    var $scope = scope ? $(scope) : $(document);
    $scope.find('[data-format="email"]').each(function () { applyEmailFormat($(this)); });
    $scope.find('[data-format="percent"]').each(function () { applyPercentFormat($(this)); });
    $scope.find('[data-format="phone"]').each(function () { applyPhoneFormat($(this)); });
    $scope.find('[data-format="landline"]').each(function () { applyLandlineFormat($(this)); });
    $scope.find('input.text-right').each(function () {
        if (!$(this).attr('data-format')) applyNumberFormat($(this));
    });
}

function validateForm($form) {
    var isValid = true;
    $form.find('[data-format="email"]').each(function () {
        var val = $(this).val().trim();
        if (val && !validateEmail(val)) { $(this).addClass("is-invalid"); showFieldError($(this), "Email không hợp lệ."); isValid = false; }
    });
    $form.find('[data-format="percent"]').each(function () {
        var val = parseFloat($(this).val());
        if (!isNaN(val) && (val < 0 || val > 100)) { $(this).addClass("is-invalid"); showFieldError($(this), "Tỷ lệ phải từ 0 đến 100."); isValid = false; }
    });
    $form.find('[data-format="phone"]').each(function () {
        var val = $(this).val().trim();
        if (val && !validatePhoneVN(val)) { $(this).addClass("is-invalid"); showFieldError($(this), "Số di động không hợp lệ."); isValid = false; }
    });
    $form.find('[data-format="landline"]').each(function () {
        var val = $(this).val().trim();
        if (val && !validateLandlineVN(val)) { $(this).addClass("is-invalid"); showFieldError($(this), "Số điện thoại bàn không hợp lệ."); isValid = false; }
    });
    return isValid;
}

$(document).ready(function () {
    initFormUtils(document);
    $(document).on("shown.bs.modal", ".modal", function () {
        initFormUtils(this);
    });
});