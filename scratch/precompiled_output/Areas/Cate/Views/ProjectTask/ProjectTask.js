function ProjectTask_BeforeSubmit() {
    var selected = $('#AssignedEmployeeIDs_Multi').val();
    if (selected && selected.length > 0) {
        $('#hiddenAssignedEmployeeIDs').val(selected.join(','));
    } else {
        $('#hiddenAssignedEmployeeIDs').val('');
    }
}

function ProjectTask_InitBulkTaskGroupForm() {
    var $form = $("#AddProjectTaskFromTaskGroup");
    var isSyncingEditor = false;

    if (!$form.length) {
        return;
    }

    function getFilters() {
        return {
            keyword: ($("#Keyword").val() || "").toLowerCase().trim(),
            taskGroupId: $("#TaskGroupID").val() || "",
            taskTypeId: $("#TaskTypeID").val() || ""
        };
    }

    function getTaskRows() {
        return $form.find(".bulk-task-row");
    }

    function getSelectedRows() {
        return getTaskRows().filter(function () {
            return $(this).find(".bulk-task-checkbox").is(":checked");
        });
    }

    function setEditorFieldValue(selector, value) {
        var $field = $(selector);
        $field.val(value || "");

        if ($field.is("select")) {
            $field.trigger("change");
        }
    }

    function resetRowEditorValues($row) {
        $row.find(".bulk-task-hidden-status").val("");
        $row.find(".bulk-task-hidden-priority-id").val("");
        $row.find(".bulk-task-hidden-assigned-employee").val("");
        $row.find(".bulk-task-hidden-start-date").val("");
        $row.find(".bulk-task-hidden-completed-date").val("");
        $row.find(".bulk-task-hidden-completion-percentage").val("");
        $row.find(".bulk-task-hidden-estimated-hours").val("");
        $row.find(".bulk-task-hidden-actual-hours").val("");
        $row.find(".bulk-task-hidden-description").val("");
        $row.find(".bulk-task-hidden-note").val("");
    }

    function clearAllSelections() {
        getTaskRows().each(function () {
            var $row = $(this);
            var $checkbox = $row.find(".bulk-task-checkbox");

            if (!$checkbox.is(":disabled")) {
                $checkbox.prop("checked", false);
            }
        });
    }

    function updateSelectedCount() {
        $("#bulkTaskSelectedCount").text(getSelectedRows().length);
    }

    function updateCheckAllState() {
        var $visibleCheckboxes = getTaskRows().filter(":visible").find(".bulk-task-checkbox:not(:disabled)");
        var checkedCount = $visibleCheckboxes.filter(":checked").length;
        $("#bulkTaskCheckAll").prop("checked", $visibleCheckboxes.length > 0 && checkedCount === $visibleCheckboxes.length);
    }

    function getCommonHiddenValue(fieldClass) {
        var $selectedRows = getSelectedRows();
        var commonValue = null;
        var hasValue = false;

        $selectedRows.each(function () {
            var currentValue = $.trim($(this).find(fieldClass).val() || "");

            if (!hasValue) {
                commonValue = currentValue;
                hasValue = true;
                return;
            }

            if (commonValue !== currentValue) {
                commonValue = "";
                return false;
            }
        });

        return hasValue ? commonValue : "";
    }

    function syncEditorPanel() {
        var $selectedRows = getSelectedRows();

        getTaskRows().each(function () {
            var $row = $(this);
            $row.toggleClass("bulk-task-selected", $row.find(".bulk-task-checkbox").is(":checked"));
        });

        if ($selectedRows.length === 0) {
            $("#bulkTaskEditorPanel").addClass("d-none");

            isSyncingEditor = true;
            setEditorFieldValue("#bulkTaskEditorStatus", "");
            setEditorFieldValue("#bulkTaskEditorStartDate", "");
            setEditorFieldValue("#bulkTaskEditorPriorityID", "");
            setEditorFieldValue("#bulkTaskEditorCompletedDate", "");
            setEditorFieldValue("#bulkTaskEditorAssignedEmployeeIDs", "");
            setEditorFieldValue("#bulkTaskEditorCompletionPercentage", "");
            setEditorFieldValue("#bulkTaskEditorEstimatedHours", "");
            setEditorFieldValue("#bulkTaskEditorActualHours", "");
            setEditorFieldValue("#bulkTaskEditorDescription", "");
            isSyncingEditor = false;
            return;
        }

        $("#bulkTaskEditorPanel").removeClass("d-none");

        isSyncingEditor = true;
        setEditorFieldValue("#bulkTaskEditorStatus", getCommonHiddenValue(".bulk-task-hidden-status"));
        setEditorFieldValue("#bulkTaskEditorStartDate", getCommonHiddenValue(".bulk-task-hidden-start-date"));
        setEditorFieldValue("#bulkTaskEditorPriorityID", getCommonHiddenValue(".bulk-task-hidden-priority-id"));
        setEditorFieldValue("#bulkTaskEditorCompletedDate", getCommonHiddenValue(".bulk-task-hidden-completed-date"));
        setEditorFieldValue("#bulkTaskEditorAssignedEmployeeIDs", getCommonHiddenValue(".bulk-task-hidden-assigned-employee"));
        setEditorFieldValue("#bulkTaskEditorCompletionPercentage", getCommonHiddenValue(".bulk-task-hidden-completion-percentage"));
        setEditorFieldValue("#bulkTaskEditorEstimatedHours", getCommonHiddenValue(".bulk-task-hidden-estimated-hours"));
        setEditorFieldValue("#bulkTaskEditorActualHours", getCommonHiddenValue(".bulk-task-hidden-actual-hours"));
        setEditorFieldValue("#bulkTaskEditorDescription", getCommonHiddenValue(".bulk-task-hidden-description"));
        isSyncingEditor = false;
    }

    function refreshSelectionState() {
        updateSelectedCount();
        updateCheckAllState();
        syncEditorPanel();
    }

    function refreshTaskRows() {
        var filters = getFilters();
        var visibleCount = 0;

        $("#bulkTaskSelectGroupMessage, #bulkTaskNoDataMessage").hide();

        if (!filters.taskGroupId) {
            getTaskRows().hide();
            $("#bulkTaskSelectGroupMessage").show();
            refreshSelectionState();
            return;
        }

        getTaskRows().each(function () {
            var $row = $(this);
            if ($row.data("removed") === 1 || $row.attr("data-removed") === "1") {
                $row.hide();
                return;
            }

            var matchGroup = String($row.data("task-group") || "") === filters.taskGroupId;
            var matchType = !filters.taskTypeId || String($row.data("task-type") || "") === filters.taskTypeId;
            var rowKeyword = String($row.data("keyword") || "");
            var matchKeyword = !filters.keyword || rowKeyword.indexOf(filters.keyword) >= 0;
            var isVisible = matchGroup && matchType && matchKeyword;

            $row.toggle(isVisible);

            if (isVisible) {
                visibleCount++;
            }
        });

        if (visibleCount === 0) {
            $("#bulkTaskNoDataMessage").show();
        }

        refreshSelectionState();
    }

    function normalizeCompletionValue(value) {
        if (value === null || value === undefined || value === "") {
            return "";
        }

        var numberValue = parseInt(value, 10);
        if (isNaN(numberValue)) {
            return "";
        }

        if (numberValue < 0) {
            numberValue = 0;
        }

        if (numberValue > 100) {
            numberValue = 100;
        }

        return numberValue.toString();
    }

    $form.off("click.bulkTaskSearch").on("click.bulkTaskSearch", "#btnSearchTaskFromGroup", function () {
        refreshTaskRows();
    });

    $form.off("keypress.bulkTaskSearch").on("keypress.bulkTaskSearch", "#Keyword", function (event) {
        if (event.which === 13) {
            event.preventDefault();
            refreshTaskRows();
        }
    });

    $form.off("change.bulkTaskTypeFilter").on("change.bulkTaskTypeFilter", "#TaskTypeID", function () {
        refreshTaskRows();
    });

    $form.off("change.bulkTaskGroupFilter").on("change.bulkTaskGroupFilter", "#TaskGroupID", function () {
        clearAllSelections();
        refreshTaskRows();
    });

    $form.off("click.bulkTaskRow").on("click.bulkTaskRow", ".bulk-task-row", function (event) {
        if ($(event.target).closest(".bulk-task-checkbox, .bulk-task-action-cell, .dropdown-toggle, .dropdown-menu, button, a, input, select, textarea, label").length) {
            return;
        }

        var $checkbox = $(this).find(".bulk-task-checkbox");
        if ($checkbox.is(":disabled")) {
            return;
        }

        $checkbox.prop("checked", !$checkbox.is(":checked"));
        refreshSelectionState();
    });

    $form.off("change.bulkTaskCheck").on("change.bulkTaskCheck", ".bulk-task-checkbox", function () {
        refreshSelectionState();
    });

    $form.off("click.bulkTaskActionEdit").on("click.bulkTaskActionEdit", ".bulk-task-action-edit", function () {
        var $row = $(this).closest(".bulk-task-row");
        var $checkbox = $row.find(".bulk-task-checkbox");

        if ($checkbox.is(":disabled")) {
            return;
        }

        clearAllSelections();
        $checkbox.prop("checked", true);
        refreshSelectionState();

        var editorPanel = document.getElementById("bulkTaskEditorPanel");
        if (editorPanel) {
            editorPanel.scrollIntoView({ behavior: "smooth", block: "nearest" });
        }
    });

    $form.off("click.bulkTaskActionDelete").on("click.bulkTaskActionDelete", ".bulk-task-action-delete", function () {
        var $row = $(this).closest(".bulk-task-row");
        var $checkbox = $row.find(".bulk-task-checkbox");

        if ($checkbox.is(":disabled")) {
            return;
        }

        resetRowEditorValues($row);
        $checkbox.prop("checked", false);
        $row.attr("data-removed", "1").data("removed", 1).addClass("bulk-task-removed");
        refreshSelectionState();
        refreshTaskRows();
    });

    $form.off("change.bulkTaskCheckAll").on("change.bulkTaskCheckAll", "#bulkTaskCheckAll", function () {
        var isChecked = $(this).is(":checked");

        getTaskRows().filter(":visible").each(function () {
            var $checkbox = $(this).find(".bulk-task-checkbox");

            if (!$checkbox.is(":disabled")) {
                $checkbox.prop("checked", isChecked);
            }
        });

        refreshSelectionState();
    });

    $form.off("change.bulkTaskEditor input.bulkTaskEditor").on("change.bulkTaskEditor input.bulkTaskEditor", ".bulk-task-editor-field", function () {
        var $field = $(this);
        var fieldClass = $field.data("fieldClass");
        var value = $field.val();

        if (isSyncingEditor || !fieldClass) {
            return;
        }

        if ($field.attr("id") === "bulkTaskEditorCompletionPercentage") {
            value = normalizeCompletionValue(value);
            $field.val(value);
        }

        getSelectedRows().each(function () {
            var $row = $(this);
            $row.find(fieldClass).val(value || "");

            if ($field.attr("id") === "bulkTaskEditorDescription") {
                $row.find(".bulk-task-hidden-note").val(value || "");
            }
        });
    });

    setTimeout(function () {
        refreshTaskRows();
    }, 0);
}
