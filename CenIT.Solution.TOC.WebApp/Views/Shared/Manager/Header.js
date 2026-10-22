var _instructContentCache = {};

$(document).on('click', '.menu-item', function () {
    const $item = $(this);
    const id = $item.data('id');
    const $parentNode = $item.closest('.tree-node');
    const $toggleIcon = $item.find('.toggle-icon');

    if ($toggleIcon.length && !$parentNode.hasClass('expanded')) {
        showChildren(id);
        $parentNode.addClass('expanded');
        $toggleIcon.removeClass('fa-caret-right').addClass('fa-caret-down');
    }

    $('.menu-item').removeClass('active-item');
    $item.addClass('active-item');

    if (_instructContentCache[id]) {
        $('#Title-content-container').html(_instructContentCache[id].title);
        $('#content-container').html(_instructContentCache[id].content);
        $('#InstructContent').scrollTop(0);
        return;
    }

    $('#content-container').html('<div class="p-3 text-secondary"><i class="fas fa-spinner fa-spin mr-2"></i>Đang tải nội dung hướng dẫn...</div>');

    $.get('/Sys/Instruct/GetInstructContent/' + id, function (res) {
        if (res && res.data) {
            _instructContentCache[id] = {
                title: res.data.InstructName || '',
                content: res.data.Content || ''
            };
            $('#Title-content-container').html(res.data.InstructName || '');
            $('#content-container').html(res.data.Content || '');
            $('#InstructContent').scrollTop(0);
        }
    });
});

$(document).on('show.bs.modal', '#modal_ViewInstruct', function () {
    _instructContentCache = {};
    const modal = $(this);
    modal.find('.modal-dialog').css({
        'max-width': 'calc(100% - 100px)'
    });

    modal.find('.tree-node').each(function () {
        const parent = $(this).data('parent');

        if (parent != 0) {
            $(this).hide();
        } else {
            $(this).show();
        }

        $(this).removeClass('expanded');

        $(this).find('.toggle-icon')
            .removeClass('fa-caret-down')
            .addClass('fa-caret-right');
    });

    modal.find('.modal-body').addClass('py-0 px-0');

    const first = modal.find('.menu-item').first();
    $('.menu-item').removeClass('active-item');
    if (first.length) {
        first.click();
    }
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

$(document).on('input keydown', '#modal_ViewInstruct .search-input, .left-panel .search-input', function (e) {
    if (e.type === 'keydown' && e.key === 'Enter') {
        e.preventDefault();
    }
    const keyword = ($(this).val() || '').trim().toLowerCase();
    filterInstructTreeClient(keyword);
});

function filterInstructTreeClient(keyword) {
    const $nodes = $('.tree-container .tree-node');
    if (!$nodes.length) return;

    if (!keyword) {
        $nodes.each(function () {
            const parent = $(this).data('parent');
            if (parent != 0) {
                $(this).hide();
            } else {
                $(this).show();
            }
            $(this).removeClass('expanded');
            $(this).find('.toggle-icon').removeClass('fa-caret-down').addClass('fa-caret-right');
        });
        return;
    }

    const matchedIds = new Set();
    const parentMap = {};

    $nodes.each(function () {
        const id = Number($(this).data('id'));
        const parent = Number($(this).data('parent') || 0);
        parentMap[id] = parent;
        const title = ($(this).find('.menu-item').attr('title') || $(this).text() || '').toLowerCase();
        if (title.indexOf(keyword) !== -1) {
            matchedIds.add(id);
        }
    });

    // Include ancestors of matched nodes and children of matched parent nodes
    const visibleIds = new Set(matchedIds);
    matchedIds.forEach(function (id) {
        let currParent = parentMap[id];
        while (currParent && currParent > 0) {
            visibleIds.add(currParent);
            currParent = parentMap[currParent];
        }
    });

    $nodes.each(function () {
        const id = Number($(this).data('id'));
        const parent = Number($(this).data('parent') || 0);
        if (matchedIds.has(parent)) {
            visibleIds.add(id);
        }
    });

    $nodes.each(function () {
        const id = Number($(this).data('id'));
        if (visibleIds.has(id)) {
            $(this).show().addClass('expanded');
            $(this).find('.toggle-icon').removeClass('fa-caret-right').addClass('fa-caret-down');
        } else {
            $(this).hide().removeClass('expanded');
        }
    });
}

$('#btnShowRegulation').on('click', function () {
    $('#regulationModal').modal('show');
});
