let breadcrumb_icon = `<i class="fas fa-angle-double-right mx-1 text-secondary fa-sm breadcrumb-icon text-center"></i>`;
//let breadcrumb_home = `<a href="/Dashboard/Manager" class="no-underline text-primary-d2"><i class="fa fa-home fa-1x"></i> <span>Trang chủ</span></a>`;
let breadcrumb_home = `<a href="/Dashboard/Dashboard" class="no-underline text-primary-d2"><i class="fa fa-home fa-1x"></i> <span>Trang chủ</span></a>`;
let title_of_page = $('title').text();
var arrBreadcrumbs = [];
var strBreadcrumbs = breadcrumb_home;
var strIds = $('.nav-link[href="' + window.location.pathname + '"]').prop('name');
if (strIds != undefined) {
    var arrIds = strIds.split(',');
    for (let i = 0; i < arrIds.length; i++) {
        var title = $('.nav-item[id=' + arrIds[i] + ']').prop('title');
        strBreadcrumbs += breadcrumb_icon;
        strBreadcrumbs += (i != arrIds.length - 1)
            ? `<a class="no-underline text-primary-d2" href="/Dashboard/Manager?menu=` + arrIds[i] + `">` + title + `</a>`
            : `<span class="text-secondary-d4">` + title + `</span>`;
    }
    $('div.page-title').html(strBreadcrumbs)
}

/**
 * Tạo breakcum đối với các trang ko có trong menu trái
 * @param {any} arr
 */
function CreateBreadcrumb(arr) {
    try {
        var strBreadcrumbs = breadcrumb_home;
        for (var i = 0; i < arr.length; i++) {
            strBreadcrumbs += breadcrumb_icon;
            strBreadcrumbs += (i != arr.length - 1)
                ? `<a class="no-underline text-primary-d2" href="` + (arr[i].href == undefined ? 0 : arr[i].href) + `">` + arr[i].title + `</a>`
                : `<span class="text-secondary-d4">` + title_of_page + `</span>`;
        }
        $('div.page-title').html(strBreadcrumbs)
    }
    catch (err) {
        console.log('Tạo Breadcrumb thất bại! - ' + err.message);
    }
}

///**
// * Tạo breadcumb từ url hiện tại
// */
//function CreateBreadcumbByUrl() {
//    const urlParams = new URLSearchParams(window.location.search);
//    var doiTuong = urlParams.get('keydoituong');
//    var arr = [];
//    switch (doiTuong) {
//        case 'DonViLuHanh':
//            arr = [
//                { title: 'Đơn vị lữ hành', href: '/Dashboard/Manager?menu=44' },
//                { title: 'Danh sách Đơn vị lữ hành', href: '/DonViLuHanh/DVLH_HoSoDVLH' },
//                { title: $('title').text() }
//            ]; break;
//        case 'CoSoLuuTru':
//            arr = [
//                { title: 'Cơ sở lưu trú', href: '/Dashboard/Manager?menu=43' },
//                { title: 'Danh sách Cơ sở lưu trú', href: '/CoSoLuuTru/CSLT_HoSoCSLT' },
//                { title: $('title').text() }
//            ]; break;
//        case 'NhaHang':
//            arr = [
//                { title: 'Nhà hàng', href: '/Dashboard/Manager?menu=51' },
//                { title: 'Danh sách Nhà hàng', href: '/NhaHang/NH_HoSoNH' },
//                { title: $('title').text() }
//            ]; break;
//        case 'DiemMuaSam':
//            arr = [
//                { title: 'Điểm mua sắm', href: '/Dashboard/Manager?menu=46' },
//                { title: 'Danh sách điểm mua sắm', href: '/DiemMuaSam/DMS_HoSoDMS' },
//                { title: $('title').text() }
//            ]; break;
//        case 'DiemVuiChoi':
//            arr = [
//                { title: 'Điểm vui chơi', href: '/Dashboard/Manager?menu=45' },
//                { title: 'Danh sách Điểm vui chơi', href: '/DiemVuiChoi/DVC_HoSoDVC' },
//                { title: $('title').text() }
//            ]; break;
//    }
//    // tạo breadcrumd
//    CreateBreadcrumb(arr);
//}
