function LogTask_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        $("#" + formId).closest(".modal").modal("hide");
        eval(response.message);
        if (typeof reloadProjectTask === 'function') {
            reloadProjectTask();
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

function LogTask_SetReload() { }

$(document).ready(function () {
    if ($("#LogDate").length) {
        $("#LogDate").datetimepicker({
            format: "DD/MM/YYYY HH:mm",
        });
    }
});

function bindEditLogForm() {
    $('form#EditLogForm').ajaxForm({
        success: function (response) {
            if (response.status != undefined) {
                eval(response.message);
                if (response.list) {
                    $("#LogList").html(response.list);
                }
                var modalId = $('form#EditLogForm').closest('.modal').attr('id');
                if (modalId) $('#' + modalId).modal('hide');
            } else {
                $("#bodyForm").html(response);
            }
        }
    });
}

function initLogCKEditor() {
    if (typeof CKEDITOR === 'undefined') {
        loadCKEditorThenInit();
        return;
    }
    var instance = CKEDITOR.instances['LogTaskDescription'];
    if (instance) {
        try {
            instance.destroy(true);
        } catch (e) {
            delete CKEDITOR.instances['LogTaskDescription'];
        }
    }

    var attempts = 0;
    var tryReplace = function () {
        var el = document.getElementById('LogTaskDescription');
        if (el) {
            CKEDITOR.replace('LogTaskDescription');
        } else if (attempts < 20) {
            attempts++;
            setTimeout(tryReplace, 50);
        }
    };
    tryReplace();
}

function loadCKEditorThenInit() {
    if (typeof CKEDITOR !== 'undefined') {
        initLogCKEditor();
        return;
    }
    var s = document.createElement('script');
    s.src = '/Contents/Modules/Major/ckeditor4/ckeditor.js';
    s.onload = function () {
        var ck2 = document.createElement('script');
        ck2.src = '/Contents/Modules/Major/ckfinder/ckfinder.js';
        ck2.onload = function () { initLogCKEditor(); };
        ck2.onerror = function () { initLogCKEditor(); };
        document.head.appendChild(ck2);
    };
    document.head.appendChild(s);
}

$(document).ready(function () {
    if ($('form#EditLogForm').length) {
        bindEditLogForm();
    }
});

$(document).on('click', '.btn-toggle-log', function (e) {
    e.preventDefault();
    $(this).closest('.col-12').next('.content-detail-log').slideToggle();
});