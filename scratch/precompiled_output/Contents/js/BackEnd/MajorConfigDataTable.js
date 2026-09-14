
// reset cái bảng cuối xuất hiện trên màn hình
function ReloadLastTable() {
    var idLastTable = $('table.table').eq($('table.table').length - 1).prop('id')
    var functions = findDataTableFunctionsById(idLastTable);
    var lastFunc = functions[functions.length - 1]
    if (typeof window[lastFunc] === 'function') {
        $('#' + idLastTable).DataTable().destroy();
        eval(lastFunc + '()')
    }
}


// Lắng nghe sự kiện click modal trên toàn bộ trang
document.addEventListener('click', function (event) {
    var clickedElement = event.target;
    // Kiểm tra xem phần tử được click có thuộc tính aria-label="Close" không
    var isOpenModal = clickedElement.getAttribute('data-modal-id');
    // Nếu phần tử được click là nút mở modal
    if (isOpenModal != null) {
        var fnString = `$('#modal_` + isOpenModal + `').on('hidden.bs.modal', function () { 
            $('.dataTables_filter button').addClass('disabled');
            setTimeout(() => {
              ReloadLastTable();
            }, "1000");
        });`;
        // chờ 1 giây reload lại con mắt 
        eval(fnString);

        // Ngăn chặn hành vi mặc định của nút (ví dụ: chặn form submit)
        //event.preventDefault();
    }
});


/**
 * Tìm functions dưới JS có sử dụng DataTable và ID bảng truyền vào
 */
function findDataTableFunctionsById(id) {
    var matchingFunctions = [];

    // Iterate through all functions
    for (var functionName in window) {
        if (typeof window[functionName] === 'function') {
            var functionBody = window[functionName].toString();

            // Check if the function uses DataTable and the specified ID
            if (functionBody.includes('DataTable') && functionBody.includes(id)) {
                matchingFunctions.push(functionName);
            }
        }
    }
    return matchingFunctions;
}

