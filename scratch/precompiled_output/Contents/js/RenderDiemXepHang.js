let ngoiSaoVang = `<span class="icon-stack">
                            <i class="far fa-star text-secondary"></i>
                            <i class="fas fa-star text-orange-m2"></i>
                        </span>`;
let ngoiSaoTrang = `<span class="icon-stack">
                            <i class="far fa-star text-secondary"></i>
                            <i class="far fa-star text-secondary"></i>
                        </span>`;
let ngoiSaoNuaVang = `<span class="icon-stack">
                            <i class="far fa-star text-secondary"></i>
                            <i class="fas fa-star-half text-orange-m2"></i>
                        </span>`;

/**
 * Render sao tu danh sach
 */
function RenderAllStar() {
    var countRc = $('span#renderStar').length;
    for (var i = 0; i < countRc; i++) {
        var item = $('span#renderStar').eq(i);
        RenderDiemXepHang(item.attr('class'));
    }
}

/**
 * Render sao len view theo diem TBC
 * @param {any} diemTBC
 * @param {any} classShowData
 * @param {any} classShowDataParent
 */
function RenderDiemXepHang(classShowData, classShowDataParent = null) {
    var path = classShowDataParent == null ? ('.' + classShowData) : ('.' + classShowDataParent + ' .' + classShowData);
    var diemTB = $(path).attr('title');
    let integerPart = Math.floor(diemTB);   // điểm chẵn
    let decimalPart = diemTB - integerPart; // điểm lẻ
    var slNgoiSao = 0;
    var html = '';

    if (integerPart > 0) {
        html += ngoiSaoVang.repeat(integerPart);
        slNgoiSao = integerPart;
    }
    if (decimalPart >= 0.5) {
        html += ngoiSaoNuaVang;
        slNgoiSao++;
    }
    if (slNgoiSao < 5) {
        html += ngoiSaoTrang.repeat(5 - slNgoiSao);
        slNgoiSao++;
    }
    $(path).html(html);
}