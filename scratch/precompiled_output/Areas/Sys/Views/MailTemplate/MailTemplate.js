var _mailTemplateActionURLs = {
    MailTemplate_GetData: "/Sys/MailTemplate/Get"
};

var _tableMailTemplate;
var _mailTemplateEditorId = "TemplateContent";
var _mailTemplateCkEditorPath = "/Contents/Modules/Major/ckeditor4/ckeditor.js";
var _mailTemplateCkFinderPath = "/Contents/Modules/Major/ckfinder/ckfinder.js";
var _mailTemplateEditorInitTimeout = null;
var _mailTemplateCkEditorCallbacks = [];
var _mailTemplateCkEditorLoading = false;
var _mailTemplateCkFinderLoading = false;

$(document).ready(function () {
    if (!_tableMailTemplate) {
        initTableMailTemplate();
    }
});

function initTableMailTemplate() {
    _tableMailTemplate = $("#DSMailTemplate").DataTable({
        "responsive": false,
        "lengthChange": true,
        "processing": true,
        "serverSide": true,
        "searching": false,
        "paging": false,
        "ajax": {
            "url": _mailTemplateActionURLs.MailTemplate_GetData,
            "type": "POST",
            "dataType": "JSON",
            "data": function (d) {
                d.search = $("#Keyword").val();
            }
        },
        "columns": [
            {
                "data": "TemplateCode",
                "className": "text-left",
                "defaultContent": ""
            },
            {
                "data": "TemplateName",
                "className": "text-left",
                "defaultContent": ""
            },
            {
                "data": "SubjectTemplate",
                "className": "text-left",
                "defaultContent": ""
            },
            {
                "data": "IsActive",
                "className": "text-center",
                "defaultContent": "",
                "render": function (data) {
                    return data ? '<i class="fa fa-check text-green"></i>' : "";
                }
            },
            {
                "data": "FilePath",
                "className": "text-left",
                "defaultContent": ""
            },
            {
                "data": "MailTemplateId",
                "defaultContent": "",
                "render": function (data, type) {
                    var html = "<span>";

                    if (type === "display") {
                        html += _renderButton(
                            true,
                            "EditMailTemplate",
                            "btn btn-lighter-primary mr-1",
                            "/Sys/MailTemplate/Edit/" + data,
                            '<i class="far fa-edit text-primary text-120"></i>',
                            "Cập nhật");

                        html += _renderButton(
                            true,
                            "DeleteMailTemplate",
                            "btn btn-lighter-danger mr-1",
                            "/Sys/MailTemplate/Delete/" + data,
                            '<i class="far fa-trash-alt text-danger text-120"></i>',
                            "Xóa");
                    }

                    html += "</span>";
                    return html;
                }
            }
        ]
    });
}

function searchMailTemplates() {
    if (_tableMailTemplate) {
        _tableMailTemplate.ajax.reload(null, false);
    }
}

$("#Keyword").on("keypress", function (e) {
    if (e.which === 13) {
        e.preventDefault();
        searchMailTemplates();
    }
});

function MailTemplate_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        $("#ModalContent #modal_" + formId).modal("hide");
        $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
            MailTemplate_DestroyEditor();

            if (response.message) {
                eval(response.message);
            }

            if (_tableMailTemplate) {
                _tableMailTemplate.ajax.reload(null, false);
            }
        });
    } else {
        MailTemplate_DestroyEditor();
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
        setTimeout(function () {
            MailTemplate_BindDynamicUI();
        }, 50);
    }
}

function MailTemplate_BindForm(formId, modalId) {
    $(".modal-footer #btnSave").attr("type", "button");

    MailTemplate_BindDynamicUI();

    $("form#" + formId).ajaxForm({
        beforeSubmit: function () {
            MailTemplate_PrepareSubmit();
        },
        success: function (response) {
            if (response.status != undefined) {
                if ($("#ModalContent #modal_" + modalId + " #chkNotDismissModal").is(":checked") && response.status) {
                    if (response.message) {
                        eval(response.message);
                    }

                    if (_tableMailTemplate) {
                        _tableMailTemplate.ajax.reload(null, false);
                    }

                    $("form#" + formId)[0].reset();
                    MailTemplate_ResetFileUpload();
                    MailTemplate_ClearParamRows();
                    MailTemplate_SetEditorContent("");
                    return;
                }

                $("#ModalContent #modal_" + modalId).modal("hide");
                $("#ModalContent #modal_" + modalId).on("hidden.bs.modal", function () {
                    MailTemplate_DestroyEditor();

                    if (response.message) {
                        eval(response.message);
                    }

                    if (_tableMailTemplate) {
                        _tableMailTemplate.ajax.reload(null, false);
                    }
                });
            } else {
                MailTemplate_DestroyEditor();
                $("#ModalContent #modal_" + modalId + " #bodyForm").html(response);
                setTimeout(function () {
                    MailTemplate_BindDynamicUI();
                }, 50);
            }
        }
    });

    $("#btnSave").off("click.mailtemplate").on("click.mailtemplate", function () {
        $("form#" + formId).submit();
    });
}

function MailTemplate_BindDynamicUI() {
    MailTemplate_BindFileUpload();
    MailTemplate_BindParamEvents();
    MailTemplate_BindEditorSection();
}

function MailTemplate_PrepareSubmit() {
    MailTemplate_UpdateEditorValue();
    MailTemplate_ReIndexParams();
}

function MailTemplate_QueueInitEditor() {
    if (_mailTemplateEditorInitTimeout) {
        clearTimeout(_mailTemplateEditorInitTimeout);
    }

    _mailTemplateEditorInitTimeout = setTimeout(function () {
        MailTemplate_LoadCkEditor(function () {
            MailTemplate_InitEditor();
        });
    }, 100);
}

function MailTemplate_BindEditorSection() {
    $(document)
        .off("shown.bs.collapse.mailtemplate", "#MailTemplateEditorCollapse")
        .on("shown.bs.collapse.mailtemplate", "#MailTemplateEditorCollapse", function () {
            MailTemplate_QueueInitEditor();
        });

    $(document)
        .off("hidden.bs.collapse.mailtemplate", "#MailTemplateEditorCollapse")
        .on("hidden.bs.collapse.mailtemplate", "#MailTemplateEditorCollapse", function () {
            MailTemplate_UpdateEditorValue();
            MailTemplate_DestroyEditor();
        });
}

function MailTemplate_BindFileUpload() {
    $(document)
        .off("change.mailtemplate", "#TemplateFile")
        .on("change.mailtemplate", "#TemplateFile", function () {
            MailTemplate_OnFileSelected(this);
        });
}

function MailTemplate_ResetFileUpload() {
    $("#TemplateFileName").text("Chưa chọn file .cshtml");
}

function MailTemplate_OnFileSelected(fileInput) {
    var fileName = "";

    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        fileName = fileInput.files[0].name;
    } else if (fileInput) {
        fileName = $(fileInput).val().split("\\").pop();
    }

    if (!fileName) {
        fileName = "Chưa chọn file .cshtml";
    }

    $("#TemplateFileName").text(fileName);

    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        return;
    }

    var selectedFile = fileInput.files[0];
    var extension = selectedFile.name.split(".").pop().toLowerCase();

    if (extension !== "cshtml") {
        return;
    }

    var reader = new FileReader();
    reader.onload = function (e) {
        MailTemplate_SetEditorContent(e.target.result || "");
        $("#MailTemplateEditorCollapse").collapse("show");
    };
    reader.readAsText(selectedFile);
}

function MailTemplate_BindParamEvents() {
    $(document)
        .off("click.mailtemplate", "#btnAddTemplateParam")
        .on("click.mailtemplate", "#btnAddTemplateParam", function () {
            MailTemplate_AddParamRow();
        });

    $(document)
        .off("click.mailtemplate", ".btn-remove-template-param")
        .on("click.mailtemplate", ".btn-remove-template-param", function () {
            MailTemplate_RemoveParamRow(this);
        });
}

function MailTemplate_AddParamRow() {
    $("#TemplateParamBody .template-param-empty").remove();

    var rowHtml = [
        '<tr class="template-param-row">',
        '    <td>',
        '        <input class="template-param-id" type="hidden" value="0" />',
        '        <input class="template-param-templateid" type="hidden" value="0" />',
        '        <input class="form-control form-control-sm template-param-code" type="text" maxlength="100" />',
        '    </td>',
        '    <td>',
        '        <input class="form-control form-control-sm template-param-name" type="text" maxlength="250" />',
        '    </td>',
        '    <td class="text-center">',
        '        <input class="template-param-required" type="checkbox" />',
        '    </td>',
        '    <td>',
        '        <input class="form-control form-control-sm template-param-default" type="text" />',
        '    </td>',
        '    <td class="text-center">',
        '        <button type="button" class="btn btn-outline-danger btn-sm btn-remove-template-param">',
        '            <i class="fa fa-trash"></i>',
        '        </button>',
        '    </td>',
        '</tr>'
    ].join("");

    $("#TemplateParamBody").append(rowHtml);
    MailTemplate_ReIndexParams();
}

function MailTemplate_RemoveParamRow(button) {
    $(button).closest(".template-param-row").remove();

    if ($("#TemplateParamBody .template-param-row").length === 0) {
        MailTemplate_ClearParamRows();
    } else {
        MailTemplate_ReIndexParams();
    }
}

function MailTemplate_ClearParamRows() {
    $("#TemplateParamBody").html(
        '<tr class="template-param-empty">' +
        '    <td colspan="5" class="text-center text-muted">' +
        '        Chưa có tham số nào. Nhấn "Thêm tham số" để khai báo các biến dùng trong mẫu email.' +
        '    </td>' +
        '</tr>');
}

function MailTemplate_ReIndexParams() {
    $("#TemplateParamBody .template-param-row").each(function (index) {
        $(this).find(".template-param-id").attr("name", "TemplateParams[" + index + "].MailTemplateParamId");
        $(this).find(".template-param-templateid").attr("name", "TemplateParams[" + index + "].MailTemplateId");
        $(this).find(".template-param-code").attr("name", "TemplateParams[" + index + "].ParamCode");
        $(this).find(".template-param-name").attr("name", "TemplateParams[" + index + "].ParamName");
        $(this).find(".template-param-required").attr("name", "TemplateParams[" + index + "].IsRequired");
        $(this).find(".template-param-default").attr("name", "TemplateParams[" + index + "].DefaultValue");
    });
}

function MailTemplate_InitEditor() {
    if (typeof CKEDITOR === "undefined") {
        return;
    }

    var textarea = $("#ModalContent").find("#" + _mailTemplateEditorId);
    if (textarea.length === 0) {
        return;
    }

    var instance = CKEDITOR.instances[_mailTemplateEditorId];
    if (instance) {
        try {
            instance.destroy(true);
        } catch (e) {
        }
    }

    CKEDITOR.replace(textarea[0], {
        height: 420,
        filebrowserWindowWidth: "1000",
        filebrowserWindowHeight: "750"
    });
}

function MailTemplate_DestroyEditor() {
    if (typeof CKEDITOR === "undefined") {
        return;
    }

    var instance = CKEDITOR.instances[_mailTemplateEditorId];
    if (instance) {
        instance.destroy(true);
    }
}

function MailTemplate_LoadCkEditor(callback) {
    if (typeof callback !== "function") {
        return;
    }

    if (typeof CKEDITOR !== "undefined") {
        MailTemplate_LoadCkFinder(callback);
        return;
    }

    _mailTemplateCkEditorCallbacks.push(callback);

    if (_mailTemplateCkEditorLoading) {
        return;
    }

    _mailTemplateCkEditorLoading = true;

    MailTemplate_LoadScript(_mailTemplateCkEditorPath, function () {
        MailTemplate_WaitForGlobal("CKEDITOR", function () {
            MailTemplate_LoadCkFinder(function () {
                _mailTemplateCkEditorLoading = false;
                MailTemplate_RunEditorCallbacks();
            });
        }, function () {
            _mailTemplateCkEditorLoading = false;
            MailTemplate_RunEditorCallbacks();
        });
    }, function () {
        _mailTemplateCkEditorLoading = false;
        MailTemplate_RunEditorCallbacks();
    });
}

function MailTemplate_UpdateEditorValue() {
    if (typeof CKEDITOR === "undefined") {
        return;
    }

    var instance = CKEDITOR.instances[_mailTemplateEditorId];
    if (instance) {
        instance.updateElement();
    }
}

function MailTemplate_SetEditorContent(content) {
    var normalizedContent = content || "";

    if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances[_mailTemplateEditorId]) {
        CKEDITOR.instances[_mailTemplateEditorId].setData(normalizedContent);
    }

    $("#" + _mailTemplateEditorId).val(normalizedContent);
}

function MailTemplate_LoadScript(scriptPath, onSuccess, onError) {
    var existedScript = document.querySelector('script[src="' + scriptPath + '"]');

    function handleSuccess() {
        if (typeof onSuccess === "function") {
            onSuccess();
        }
    }

    function handleError() {
        if (typeof onError === "function") {
            onError();
        }
    }

    if (existedScript) {
        if (existedScript.getAttribute("data-loaded") === "true") {
            handleSuccess();
            return;
        }

        existedScript.addEventListener("load", handleSuccess, { once: true });
        existedScript.addEventListener("error", handleError, { once: true });
        return;
    }

    var script = document.createElement("script");
    script.type = "text/javascript";
    script.src = scriptPath;
    script.onload = function () {
        script.setAttribute("data-loaded", "true");
        handleSuccess();
    };
    script.onerror = function () {
        handleError();
    };
    document.head.appendChild(script);
}

function MailTemplate_WaitForGlobal(globalName, onSuccess, onError, retryCount) {
    var currentRetryCount = retryCount || 0;

    if (typeof window[globalName] !== "undefined") {
        if (typeof onSuccess === "function") {
            onSuccess();
        }
        return;
    }

    if (currentRetryCount >= 20) {
        if (typeof onError === "function") {
            onError();
        }
        return;
    }

    setTimeout(function () {
        MailTemplate_WaitForGlobal(globalName, onSuccess, onError, currentRetryCount + 1);
    }, 100);
}

function MailTemplate_LoadCkFinder(callback) {
    if (typeof callback !== "function") {
        return;
    }

    if (typeof CKFinder !== "undefined") {
        callback();
        return;
    }

    if (_mailTemplateCkFinderLoading) {
        MailTemplate_WaitForGlobal("CKFinder", callback, callback);
        return;
    }

    _mailTemplateCkFinderLoading = true;

    MailTemplate_LoadScript(_mailTemplateCkFinderPath, function () {
        MailTemplate_WaitForGlobal("CKFinder", function () {
            _mailTemplateCkFinderLoading = false;
            callback();
        }, function () {
            _mailTemplateCkFinderLoading = false;
            callback();
        });
    }, function () {
        _mailTemplateCkFinderLoading = false;
        callback();
    });
}

function MailTemplate_RunEditorCallbacks() {
    var callbacks = _mailTemplateCkEditorCallbacks.slice(0);

    _mailTemplateCkEditorCallbacks = [];

    $.each(callbacks, function (index, callback) {
        if (typeof callback === "function") {
            callback();
        }
    });
}
