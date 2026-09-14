const cache = {};

$(document).on('click', '.menu-item', function () {
    const id = $(this).data('id');

    $('.menu-item').removeClass('active-item');

    $(this).addClass('active-item');
        
    if (cache[id]) {
        $('#Title-content-container').html(cache[id].title);
        $('#content-container').html(cache[id].content);
        return;
    }

    $('#content-container').html('<div class="p-3">Loading...</div>');

    $.get(`/Sys/Instruct/GetInstructContent/${id}`, function (res) {
        cache[id] = {
            title: res.data.InstructName,
            content: res.data.Content
        };

        // res chính là dữ liệu trả về từ controller
        $('#Title-content-container').html(res.data.InstructName);
        $('#content-container').html(res.data.Content);
    });
});

$(document).on('show.bs.modal', '#modal_ViewInstruct', function () {
    const modal = $(this);
    $(this).find('.modal-dialog').css({
        'max-width': 'calc(100% - 130px)',
    });

    modal.find('.tree-node').each(function () {
        const parent = $(this).data('parent');

        if (parent != 0) {
            $(this).hide(); // ẩn node con
        } else {
            $(this).show(); // chỉ hiện node cha
        }

        // reset trạng thái
        $(this).removeClass('expanded');

        $(this).find('.toggle-icon')
            .removeClass('fa-caret-down')
            .addClass('fa-caret-right');
    });

    $(this).find('.modal-body').addClass('py-0');

    let first = $(this).find('.menu-item').first();

    $('.menu-item').removeClass('active-item');

    $(this).addClass('active-item');

    first.click();
});

$(document).on('click', '.toggle-icon', function (e) {
    e.stopPropagation();

    const parentNode = $(this).closest('.tree-node');
    const parentId = parentNode.data('id');

    if (parentNode.hasClass('expanded')) {
        hideChildren(parentId);
        parentNode.removeClass('expanded');
        $(this).removeClass('fa-caret-down').addClass('fa-caret-right');
    } else {
        showChildren(parentId);
        parentNode.addClass('expanded');
        $(this).removeClass('fa-caret-right').addClass('fa-caret-down');
    }
});

function hideChildren(parentId) {
    const children = $('.tree-node[data-parent="' + parentId + '"]');

    children.each(function () {
        const id = $(this).data('id');

        $(this).hide().removeClass('expanded');

        $(this).find('.toggle-icon')
            .removeClass('fa-caret-down')
            .addClass('fa-caret-right');

        hideChildren(id);
    });
}
function showChildren(parentId) {
    const children = $('.tree-node[data-parent="' + parentId + '"]');

    children.each(function () {
        $(this).show();

        if ($(this).hasClass('expanded')) {
            showChildren($(this).data('id'));
        }
    });
}

$(document).ready(function () {
    const first = $('.menu-item').first();
    first.click();
});

$(document).on('keydown', '.search-input', function (e) {
    if (e.key === 'Enter') {
        e.preventDefault(); 
        const keyword = $(this).val().trim();

        callSearch(keyword);
    }
});

function callSearch(keyword) {

    $('.tree-container').html('<div class="p-3">Loading...</div>');

    $.ajax({
        url: '/Sys/Instruct/Get',
        type: 'POST',
        data: {
            search: keyword,
        },
        success: function (res) {
            renderTree(res.data);

            // auto chọn item đầu
            const first = $('.tree-node:visible .menu-item').first();
            if (first.length) first.click();
        }
    });
}

function renderTree(data) {

    let html = '';

    data.forEach(item => {

        html += `
        <li class="tree-node expanded"
            data-id="${item.InstructID}"
            data-parent="${item.InstructParentID || 0}"
            data-level="${item.Level}">

            <span class="menu-item d-flex align-items-center"
                  data-id="${item.InstructID}"
                  title="${item.InstructName}"
                  style="padding-left:${10 + (item.Level * 20)}px;">

                ${item.HasChild
                ? '<i class="fa fa-caret-down toggle-icon me-2" style="font-size:24px; width:15px"></i>'
                : '<span style="width:15px;"></span>'}

                <div class="flex-grow-1 text-break">
                    <i class="fa fa-file-text-o me-2"></i>
                    ${item.InstructName}
                </div>
            </span>
        </li>
        `;
    });

    $('.tree-container').html(html);

}

$('#btnShowRegulation').on('click', function () {
    $('#regulationModal').modal('show');
});