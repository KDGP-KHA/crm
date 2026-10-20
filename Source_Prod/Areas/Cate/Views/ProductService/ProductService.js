/**
 * Product & Service Catalog (ProductService.js)
 * Modern Tree & Flat List UI with Advanced Multi-filter and Dual-mode Pagination
 */

var _ProductServiceActionURLs = {
    ProductService_GetData: "/Cate/ProductService/Get"
};

// Global State
var _productServiceRawData = [];
var _productServiceTreeData = [];
var _productServiceCollapseState = {};
var _productServiceViewMode = "tree"; // "tree" | "flat"

// Pagination State
var _psCurrentPage = 1;
var _psPageSize = 20;
var _psTotalRecords = 0;
var _psTotalPages = 1;

// Active Filter State
var _psActiveFilters = {
    keyword: "",
    groupServiceId: "",
    isActived: "",
    metricFilter: "all" // "all" | "groups" | "active" | "inactive"
};

$(document).ready(function () {
    ProductService_LoadData();
});

function Search() {
    ProductService_LoadData();
}

/**
 * Normalizes raw tree data from the server into hierarchical node models.
 */
function ProductService_NormalizeTreeData(data) {
    var rows = Array.isArray(data) ? data.slice() : [];

    rows.sort(function (left, right) {
        var leftSortPath = left.SortPath || "";
        var rightSortPath = right.SortPath || "";
        if (leftSortPath < rightSortPath) return -1;
        if (leftSortPath > rightSortPath) return 1;
        return 0;
    });

    return $.map(rows, function (item) {
        var isGroup = item.NodeType === "G";
        var nodeId = isGroup
            ? "G_" + (item.GroupServiceID || item.gID || 0)
            : "P_" + (item.ProductServiceID || item.pID || 0);
        var parentNodeId = null;

        if (isGroup) {
            if (parseInt(item.ParentGroupServiceID || 0, 10) > 0) {
                parentNodeId = "G_" + item.ParentGroupServiceID;
            }
        } else if (parseInt(item.ParentProductID || 0, 10) > 0) {
            parentNodeId = "P_" + item.ParentProductID;
        } else if (parseInt(item.GroupServiceID || 0, 10) > 0) {
            parentNodeId = "G_" + item.GroupServiceID;
        }

        item.NodeID = nodeId;
        item.ParentNodeID = parentNodeId;
        item.DisplayTreeName = item.NameProduct || item.DisplayName || "";
        item.Level = parseInt(item.Level || 0, 10);
        item.IsActived = item.IsActived === true || item.IsActived === "true" || item.IsActived === 1;

        return item;
    });
}

/**
 * Loads data via AJAX and initializes filters, metrics, and renderers.
 */
function ProductService_LoadData() {
    $.ajax({
        url: _ProductServiceActionURLs.ProductService_GetData,
        type: "POST",
        dataType: "JSON",
        data: {
            Search: function () { return $('#Keyword').val(); },
            SearchFile: function () { return $('#SearchFile').val(); }
        },
        success: function (response) {
            var rawList = response && response.data ? response.data : [];
            _productServiceRawData = rawList;
            _productServiceTreeData = ProductService_NormalizeTreeData(rawList);

            // Populate group dropdown in search
            ProductService_PopulateGroupDropdown();

            // Calculate & render 4 KPI Metric Cards
            ProductService_CalculateMetrics();

            // Reset page & render table
            _psCurrentPage = 1;
            ProductService_ApplyFilterAndRender();
        },
        error: function (xhr, status, error) {
            console.error("Failed to load ProductService data:", error);
        }
    });
}

/**
 * Calculates numbers for 4 KPI summary cards.
 */
function ProductService_CalculateMetrics() {
    var totalProducts = 0;
    var totalGroups = 0;
    var totalActive = 0;
    var totalInactive = 0;

    for (var i = 0; i < _productServiceTreeData.length; i++) {
        var item = _productServiceTreeData[i];
        if (item.NodeType === "G") {
            totalGroups++;
        } else if (item.NodeType === "P") {
            totalProducts++;
            if (item.IsActived) {
                totalActive++;
            } else {
                totalInactive++;
            }
        }
    }

    $("#kpi_total_products").text(totalProducts);
    $("#kpi_total_groups").text(totalGroups);
    $("#kpi_total_active").text(totalActive);
    $("#kpi_total_inactive").text(totalInactive);
}

/**
 * Dynamically fills the Group dropdown in _Search.cshtml from available groups.
 */
function ProductService_PopulateGroupDropdown() {
    var $select = $("#SearchGroupServiceID");
    if ($select.length === 0) return;

    var currentVal = $select.val();
    $select.empty();
    $select.append('<option value="">-- Tất cả nhóm dịch vụ --</option>');

    var groups = $.grep(_productServiceTreeData, function (item) {
        return item.NodeType === "G";
    });

    for (var i = 0; i < groups.length; i++) {
        var g = groups[i];
        var prefix = "";
        for (var l = 0; l < g.Level; l++) {
            prefix += "-- ";
        }
        var option = $('<option></option>')
            .val(g.GroupServiceID)
            .text(prefix + g.DisplayTreeName);
        $select.append(option);
    }

    if (currentVal) {
        $select.val(currentVal);
    }
}

/**
 * Interactive filter via KPI cards.
 */
function ProductService_FilterByMetric(metric) {
    _psActiveFilters.metricFilter = metric;
    $(".ps-metric-card").removeClass("active");

    if (metric === "all") {
        $("#kpi_card_total").addClass("active");
        $("#SearchIsActived").val("");
        ProductService_SetViewMode("flat");
    } else if (metric === "groups") {
        $("#kpi_card_groups").addClass("active");
        ProductService_SetViewMode("tree");
    } else if (metric === "active") {
        $("#kpi_card_active").addClass("active");
        $("#SearchIsActived").val("1");
        ProductService_SetViewMode("flat");
    } else if (metric === "inactive") {
        $("#kpi_card_inactive").addClass("active");
        $("#SearchIsActived").val("0");
        ProductService_SetViewMode("flat");
    }

    _psCurrentPage = 1;
    ProductService_ApplyFilterAndRender();
}

/**
 * Resets search inputs and filters.
 */
function ProductService_ResetFilter() {
    $("#Keyword").val("");
    $("#SearchFile").val("");
    $("#SearchGroupServiceID").val("");
    $("#SearchIsActived").val("");
    $(".ps-metric-card").removeClass("active");
    _psActiveFilters = {
        keyword: "",
        groupServiceId: "",
        isActived: "",
        metricFilter: "all"
    };
    _psCurrentPage = 1;
    ProductService_LoadData();
}

/**
 * Changes view mode between 'tree' and 'flat'.
 */
function ProductService_SetViewMode(mode) {
    _productServiceViewMode = mode;
    _psCurrentPage = 1;

    var $pageSizeSelect = $("#ps_page_size");
    var currentVal = parseInt($pageSizeSelect.val() || 20, 10);

    if (mode === "tree") {
        $("#btnModeTree").addClass("active");
        $("#btnModeFlat").removeClass("active");
        $("#psTreeControls").show();

        $pageSizeSelect.empty();
        $pageSizeSelect.append('<option value="5">5 nhóm / trang</option>');
        $pageSizeSelect.append('<option value="10">10 nhóm / trang</option>');
        $pageSizeSelect.append('<option value="20">20 nhóm / trang</option>');
        $pageSizeSelect.append('<option value="-1">Tất cả nhóm</option>');

        if ([5, 10, 20, -1].indexOf(currentVal) !== -1) {
            $pageSizeSelect.val(currentVal);
        } else {
            $pageSizeSelect.val(10);
        }
    } else {
        $("#btnModeFlat").addClass("active");
        $("#btnModeTree").removeClass("active");
        $("#psTreeControls").hide();

        $pageSizeSelect.empty();
        $pageSizeSelect.append('<option value="10">10 dòng / trang</option>');
        $pageSizeSelect.append('<option value="20">20 dòng / trang</option>');
        $pageSizeSelect.append('<option value="50">50 dòng / trang</option>');
        $pageSizeSelect.append('<option value="100">100 dòng / trang</option>');
        $pageSizeSelect.append('<option value="-1">Tất cả</option>');

        if ([10, 20, 50, 100, -1].indexOf(currentVal) !== -1) {
            $pageSizeSelect.val(currentVal);
        } else {
            $pageSizeSelect.val(20);
        }
    }

    ProductService_ApplyFilterAndRender();
}

/**
 * Filters the dataset client-side and triggers the appropriate view renderer.
 */
function ProductService_ApplyFilterAndRender() {
    var keyword = $.trim($("#Keyword").val()).toLowerCase();
    var groupServiceId = $("#SearchGroupServiceID").val();
    var isActived = $("#SearchIsActived").val();

    _psActiveFilters.keyword = keyword;
    _psActiveFilters.groupServiceId = groupServiceId;
    _psActiveFilters.isActived = isActived;

    // Render table header depending on mode
    ProductService_RenderTableHeader();

    if (_productServiceViewMode === "flat") {
        ProductService_RenderFlatView();
    } else {
        ProductService_RenderTreeView();
    }
}

/**
 * Renders the table header matching the active view mode.
 */
function ProductService_RenderTableHeader() {
    var $tr = $("#ps_table_header");
    $tr.empty();

    if (_productServiceViewMode === "flat") {
        $tr.append('<th style="width: 50px;" class="text-center">STT</th>');
        $tr.append('<th style="width: 220px;">Nhóm dịch vụ</th>');
        $tr.append('<th>Tên sản phẩm / Dịch vụ</th>');
        $tr.append('<th style="width: 160px;">Mã sản phẩm</th>');
        $tr.append('<th style="width: 140px;">Tên viết tắt</th>');
        $tr.append('<th style="width: 130px;" class="text-center">Trạng thái</th>');
        $tr.append('<th style="width: 160px;" class="text-center">Hành động</th>');
    } else {
        $tr.append('<th>Thông tin nhóm & dịch vụ</th>');
        $tr.append('<th style="width: 170px;">Mã sản phẩm</th>');
        $tr.append('<th style="width: 150px;">Tên viết tắt</th>');
        $tr.append('<th style="width: 130px;" class="text-center">Trạng thái</th>');
        $tr.append('<th style="width: 160px;" class="text-center">Hành động</th>');
    }
}

/**
 * Helper to check if a node has children.
 */
function ProductService_HasChildren(nodeId) {
    for (var index = 0; index < _productServiceTreeData.length; index++) {
        if ((_productServiceTreeData[index].ParentNodeID || "") === nodeId) {
            return true;
        }
    }
    return false;
}

/**
 * Helper to count child products under a group.
 */
function ProductService_CountProductsInGroup(groupId) {
    var count = 0;
    var targetGId = parseInt(groupId, 10);
    for (var i = 0; i < _productServiceTreeData.length; i++) {
        var item = _productServiceTreeData[i];
        if (item.NodeType === "P" && parseInt(item.GroupServiceID, 10) === targetGId) {
            count++;
        }
    }
    return count;
}

/**
 * Check if node is hidden due to any ancestor being collapsed.
 */
function ProductService_IsHidden(item) {
    var parentNodeId = item.ParentNodeID;

    while (parentNodeId) {
        if (_productServiceCollapseState[parentNodeId] === true) {
            return true;
        }

        var parentItem = null;
        for (var index = 0; index < _productServiceTreeData.length; index++) {
            if (_productServiceTreeData[index].NodeID === parentNodeId) {
                parentItem = _productServiceTreeData[index];
                break;
            }
        }
        parentNodeId = parentItem ? parentItem.ParentNodeID : null;
    }

    return false;
}

/**
 * RENDER MODE 1: Tree View with Cluster/Branch Pagination.
 */
function ProductService_RenderTreeView() {
    var keyword = _psActiveFilters.keyword;
    var selectedGroupId = _psActiveFilters.groupServiceId;
    var selectedActived = _psActiveFilters.isActived;

    // Filter matching nodes
    var visibleNodeMap = {};
    var isFiltering = keyword.length > 0 || selectedGroupId.length > 0 || selectedActived.length > 0;

    if (isFiltering) {
        // Find matching products and groups
        for (var i = 0; i < _productServiceTreeData.length; i++) {
            var item = _productServiceTreeData[i];
            var match = true;

            if (item.NodeType === "P") {
                if (keyword) {
                    var name = (item.NameProduct || "").toLowerCase();
                    var code = (item.CodeProduct || "").toLowerCase();
                    var shortName = (item.ShortNameProduct || "").toLowerCase();
                    if (name.indexOf(keyword) === -1 && code.indexOf(keyword) === -1 && shortName.indexOf(keyword) === -1) {
                        match = false;
                    }
                }
                if (selectedGroupId && parseInt(item.GroupServiceID, 10) !== parseInt(selectedGroupId, 10)) {
                    match = false;
                }
                if (selectedActived !== "") {
                    var activedBool = selectedActived === "1";
                    if (item.IsActived !== activedBool) {
                        match = false;
                    }
                }
            } else if (item.NodeType === "G") {
                if (selectedGroupId && parseInt(item.GroupServiceID, 10) !== parseInt(selectedGroupId, 10) && parseInt(item.ParentGroupServiceID, 10) !== parseInt(selectedGroupId, 10)) {
                    match = false;
                }
                if (keyword) {
                    var gName = (item.NameProduct || "").toLowerCase();
                    if (gName.indexOf(keyword) === -1) {
                        match = false;
                    }
                }
            }

            if (match) {
                visibleNodeMap[item.NodeID] = true;
                // Reveal all parents
                var currParent = item.ParentNodeID;
                while (currParent) {
                    visibleNodeMap[currParent] = true;
                    // Auto-expand parents when searching
                    _productServiceCollapseState[currParent] = false;
                    for (var j = 0; j < _productServiceTreeData.length; j++) {
                        if (_productServiceTreeData[j].NodeID === currParent) {
                            currParent = _productServiceTreeData[j].ParentNodeID;
                            break;
                        }
                    }
                }
            }
        }
    }

    // Determine major branches to paginate (Level 1 groups, or Level 0 if only roots)
    var majorBranches = [];
    for (var b = 0; b < _productServiceTreeData.length; b++) {
        var node = _productServiceTreeData[b];
        // Major branches: Level 1 groups, or Level 0 groups with no children
        if (node.NodeType === "G" && node.Level <= 1) {
            if (!isFiltering || visibleNodeMap[node.NodeID]) {
                majorBranches.push(node);
            }
        }
    }

    // Pagination over major branches
    _psTotalRecords = majorBranches.length;
    var pageSize = parseInt($("#ps_page_size").val() || _psPageSize, 10);
    _psPageSize = pageSize;

    if (pageSize === -1) {
        _psTotalPages = 1;
        _psCurrentPage = 1;
    } else {
        _psTotalPages = Math.max(1, Math.ceil(_psTotalRecords / pageSize));
        if (_psCurrentPage > _psTotalPages) _psCurrentPage = _psTotalPages;
        if (_psCurrentPage < 1) _psCurrentPage = 1;
    }

    var startIdx = pageSize === -1 ? 0 : (_psCurrentPage - 1) * pageSize;
    var endIdx = pageSize === -1 ? _psTotalRecords : Math.min(startIdx + pageSize, _psTotalRecords);

    var paginatedBranchMap = {};
    for (var p = startIdx; p < endIdx; p++) {
        if (majorBranches[p]) {
            paginatedBranchMap[majorBranches[p].NodeID] = true;
        }
    }

    // Render tree rows that belong to paginated branches
    var html = "";
    var renderedCount = 0;

    for (var index = 0; index < _productServiceTreeData.length; index++) {
        var item = _productServiceTreeData[index];

        // Filter check
        if (isFiltering && !visibleNodeMap[item.NodeID]) {
            continue;
        }

        // Branch pagination check: find ancestor major branch
        var belongsToCurrentPage = false;
        if (item.Level <= 1 && item.NodeType === "G") {
            belongsToCurrentPage = paginatedBranchMap[item.NodeID] === true;
        } else {
            // Traverse up to find Level 1 or Level 0 ancestor
            var pNodeId = item.ParentNodeID;
            while (pNodeId) {
                if (paginatedBranchMap[pNodeId] === true) {
                    belongsToCurrentPage = true;
                    break;
                }
                var pNode = null;
                for (var k = 0; k < _productServiceTreeData.length; k++) {
                    if (_productServiceTreeData[k].NodeID === pNodeId) {
                        pNode = _productServiceTreeData[k];
                        break;
                    }
                }
                pNodeId = pNode ? pNode.ParentNodeID : null;
            }
        }

        if (!belongsToCurrentPage && pageSize !== -1) {
            continue;
        }

        var level = item.Level;
        var hasChildren = ProductService_HasChildren(item.NodeID);
        var isCollapsed = _productServiceCollapseState[item.NodeID] === true;
        var isHidden = !isFiltering && ProductService_IsHidden(item);

        var indentWidth = level * 18;
        var indentHtml = indentWidth > 0 ? "<span class='d-inline-block' style='width:" + indentWidth + "px;'></span>" : "";
        var toggleHtml = "";

        if (hasChildren) {
            toggleHtml =
                "<a href='javascript:void(0)' class='ps-tree-toggle " + (isCollapsed ? "collapsed" : "expanded") + "' data-node-id='" + item.NodeID + "' title='" + (isCollapsed ? "Mở rộng" : "Thu gọn") + "'>"
                + "<i class='fa fa-chevron-right'></i>"
                + "</a>";
        } else {
            toggleHtml = "<span class='ps-tree-spacer'></span>";
        }

        var nameHtml = $("<div/>").text(item.DisplayTreeName || "").html();
        var rowClass = item.NodeType === "G" ? "product-service-group-row" : "product-service-item-row";

        html += "<tr class='" + rowClass + "'" + (isHidden ? " style='display:none;'" : "") + ">";
        html += "<td class='align-middle'>";
        html += indentHtml + toggleHtml;

        if (item.NodeType === "G") {
            var childCount = ProductService_CountProductsInGroup(item.GroupServiceID);
            html += "<span class='ps-group-title'>";
            html += "<i class='fa fa-folder-open ps-group-icon'></i>";
            html += nameHtml;
            if (childCount > 0) {
                html += "<span class='ps-group-badge' title='Số lượng sản phẩm trong nhóm'>" + childCount + " sản phẩm</span>";
            }
            html += "</span>";
        } else {
            if (level >= 3) {
                html += "<i class='fa fa-level-up-alt fa-rotate-90 ps-sub-item-icon'></i>";
            } else {
                html += "<i class='fa fa-cube ps-item-icon'></i>";
            }
            html += "<span class='ps-item-title'>" + nameHtml + "</span>";
        }

        html += "</td>";
        html += "<td class='align-middle'>" + (item.NodeType === "P" && item.CodeProduct ? "<code class='text-primary font-weight-bold'>" + item.CodeProduct + "</code>" : "") + "</td>";
        html += "<td class='align-middle'>" + (item.NodeType === "P" ? (item.ShortNameProduct || "") : "") + "</td>";
        html += "<td class='align-middle text-center'>" + (item.NodeType === "P" ? ProductService_RenderStatusBadge(item.IsActived) : "") + "</td>";
        html += "<td class='align-middle text-center'>" + ProductService_RenderActions(item) + "</td>";
        html += "</tr>";
        renderedCount++;
    }

    if (renderedCount === 0) {
        html = "<tr><td colspan='5' class='ps-empty-state'><i class='fa fa-folder-open'></i><div class='ps-empty-state-text'>Không tìm thấy nhóm hoặc sản phẩm dịch vụ phù hợp</div></td></tr>";
    }

    $("#DSProductService tbody").html(html);

    // Update pagination footer controls
    ProductService_RenderPaginationControls(startIdx + 1, endIdx, _psTotalRecords, "nhóm dịch vụ");

    // Initialize tooltips
    $('[data-rel="tooltip"]').tooltip({ container: 'body' });
}

/**
 * RENDER MODE 2: Standard Flat List View with per-item pagination.
 */
function ProductService_RenderFlatView() {
    var keyword = _psActiveFilters.keyword;
    var selectedGroupId = _psActiveFilters.groupServiceId;
    var selectedActived = _psActiveFilters.isActived;

    // Filter only products (NodeType === "P")
    var filteredProducts = [];

    // Map groupId to group name for fast lookup
    var groupNameMap = {};
    for (var g = 0; g < _productServiceTreeData.length; g++) {
        if (_productServiceTreeData[g].NodeType === "G") {
            groupNameMap[_productServiceTreeData[g].GroupServiceID] = _productServiceTreeData[g].DisplayTreeName;
        }
    }

    for (var i = 0; i < _productServiceTreeData.length; i++) {
        var item = _productServiceTreeData[i];
        if (item.NodeType !== "P") continue;

        var match = true;

        if (keyword) {
            var name = (item.NameProduct || "").toLowerCase();
            var code = (item.CodeProduct || "").toLowerCase();
            var shortName = (item.ShortNameProduct || "").toLowerCase();
            if (name.indexOf(keyword) === -1 && code.indexOf(keyword) === -1 && shortName.indexOf(keyword) === -1) {
                match = false;
            }
        }

        if (selectedGroupId && parseInt(item.GroupServiceID, 10) !== parseInt(selectedGroupId, 10)) {
            match = false;
        }

        if (selectedActived !== "") {
            var activedBool = selectedActived === "1";
            if (item.IsActived !== activedBool) {
                match = false;
            }
        }

        if (match) {
            filteredProducts.push(item);
        }
    }

    _psTotalRecords = filteredProducts.length;
    var pageSize = parseInt($("#ps_page_size").val() || _psPageSize, 10);
    _psPageSize = pageSize;

    if (pageSize === -1) {
        _psTotalPages = 1;
        _psCurrentPage = 1;
    } else {
        _psTotalPages = Math.max(1, Math.ceil(_psTotalRecords / pageSize));
        if (_psCurrentPage > _psTotalPages) _psCurrentPage = _psTotalPages;
        if (_psCurrentPage < 1) _psCurrentPage = 1;
    }

    var startIdx = pageSize === -1 ? 0 : (_psCurrentPage - 1) * pageSize;
    var endIdx = pageSize === -1 ? _psTotalRecords : Math.min(startIdx + pageSize, _psTotalRecords);

    var html = "";

    if (filteredProducts.length === 0) {
        html = "<tr><td colspan='7' class='ps-empty-state'><i class='fa fa-inbox'></i><div class='ps-empty-state-text'>Không tìm thấy sản phẩm dịch vụ nào phù hợp</div></td></tr>";
    } else {
        for (var p = startIdx; p < endIdx; p++) {
            var item = filteredProducts[p];
            var stt = p + 1;
            var groupName = groupNameMap[item.GroupServiceID] || item.GroupServiceName || "Khác";
            var nameHtml = $("<div/>").text(item.NameProduct || "").html();

            html += "<tr>";
            html += "<td class='align-middle text-center text-muted'>" + stt + "</td>";
            html += "<td class='align-middle'><span class='ps-badge-group-tag' title='" + groupName + "'><i class='fa fa-folder text-warning mr-1'></i>" + groupName + "</span></td>";
            html += "<td class='align-middle'>";
            html += "<span class='font-weight-600 text-dark'>" + nameHtml + "</span>";
            if (item.ParentProductName) {
                html += "<div class='text-muted text-85 mt-1'><i class='fa fa-level-up-alt fa-rotate-90 text-secondary mr-1'></i>Thuộc: " + item.ParentProductName + "</div>";
            }
            html += "</td>";
            html += "<td class='align-middle'>" + (item.CodeProduct ? "<code class='text-primary font-weight-bold'>" + item.CodeProduct + "</code>" : "-") + "</td>";
            html += "<td class='align-middle'>" + (item.ShortNameProduct || "-") + "</td>";
            html += "<td class='align-middle text-center'>" + ProductService_RenderStatusBadge(item.IsActived) + "</td>";
            html += "<td class='align-middle text-center'>" + ProductService_RenderActions(item) + "</td>";
            html += "</tr>";
        }
    }

    $("#DSProductService tbody").html(html);

    // Update pagination controls
    ProductService_RenderPaginationControls(startIdx + 1, endIdx, _psTotalRecords, "sản phẩm");

    // Initialize tooltips
    $('[data-rel="tooltip"]').tooltip({ container: 'body' });
}

/**
 * Renders modern pagination info and button navigation.
 */
function ProductService_RenderPaginationControls(start, end, total, unitLabel) {
    if (total === 0) {
        $("#ps_page_start").text(0);
        $("#ps_page_end").text(0);
        $("#ps_total_records").text("0 " + unitLabel);
        $("#ps_pagination_nav").empty();
        return;
    }

    $("#ps_page_start").text(start);
    $("#ps_page_end").text(end);
    $("#ps_total_records").text(total + " " + unitLabel);

    var $nav = $("#ps_pagination_nav");
    $nav.empty();

    if (_psTotalPages <= 1) {
        return;
    }

    // First button
    var isFirstDisabled = _psCurrentPage === 1;
    $nav.append(
        $('<a href="javascript:void(0)" class="ps-page-btn ' + (isFirstDisabled ? 'disabled' : '') + '" title="Trang đầu"><i class="fa fa-angle-double-left"></i></a>')
            .on("click", function () {
                if (!_psCurrentPage !== 1) ProductService_GoToPage(1);
            })
    );

    // Prev button
    $nav.append(
        $('<a href="javascript:void(0)" class="ps-page-btn ' + (isFirstDisabled ? 'disabled' : '') + '" title="Trang trước"><i class="fa fa-angle-left"></i></a>')
            .on("click", function () {
                if (_psCurrentPage > 1) ProductService_GoToPage(_psCurrentPage - 1);
            })
    );

    // Numeric buttons with smart ellipsis
    var maxVisible = 5;
    var startPage = Math.max(1, _psCurrentPage - 2);
    var endPage = Math.min(_psTotalPages, startPage + maxVisible - 1);

    if (endPage - startPage + 1 < maxVisible) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    if (startPage > 1) {
        $nav.append(
            $('<a href="javascript:void(0)" class="ps-page-btn">1</a>').on("click", function () { ProductService_GoToPage(1); })
        );
        if (startPage > 2) {
            $nav.append('<span class="ps-page-ellipsis">...</span>');
        }
    }

    for (var page = startPage; page <= endPage; page++) {
        (function (p) {
            var isActive = p === _psCurrentPage;
            var $btn = $('<a href="javascript:void(0)" class="ps-page-btn ' + (isActive ? 'active' : '') + '">' + p + '</a>');
            if (!isActive) {
                $btn.on("click", function () { ProductService_GoToPage(p); });
            }
            $nav.append($btn);
        })(page);
    }

    if (endPage < _psTotalPages) {
        if (endPage < _psTotalPages - 1) {
            $nav.append('<span class="ps-page-ellipsis">...</span>');
        }
        $nav.append(
            $('<a href="javascript:void(0)" class="ps-page-btn">' + _psTotalPages + '</a>').on("click", function () { ProductService_GoToPage(_psTotalPages); })
        );
    }

    // Next button
    var isLastDisabled = _psCurrentPage === _psTotalPages;
    $nav.append(
        $('<a href="javascript:void(0)" class="ps-page-btn ' + (isLastDisabled ? 'disabled' : '') + '" title="Trang sau"><i class="fa fa-angle-right"></i></a>')
            .on("click", function () {
                if (_psCurrentPage < _psTotalPages) ProductService_GoToPage(_psCurrentPage + 1);
            })
    );

    // Last button
    $nav.append(
        $('<a href="javascript:void(0)" class="ps-page-btn ' + (isLastDisabled ? 'disabled' : '') + '" title="Trang cuối"><i class="fa fa-angle-double-right"></i></a>')
            .on("click", function () {
                if (_psCurrentPage !== _psTotalPages) ProductService_GoToPage(_psTotalPages);
            })
    );
}

/**
 * Navigate to a specific page.
 */
function ProductService_GoToPage(page) {
    if (page < 1 || page > _psTotalPages) return;
    _psCurrentPage = page;
    if (_productServiceViewMode === "flat") {
        ProductService_RenderFlatView();
    } else {
        ProductService_RenderTreeView();
    }
}

/**
 * Handles page size change.
 */
function ProductService_ChangePageSize(newSize) {
    _psPageSize = parseInt(newSize, 10);
    _psCurrentPage = 1;
    if (_productServiceViewMode === "flat") {
        ProductService_RenderFlatView();
    } else {
        ProductService_RenderTreeView();
    }
}

/**
 * Renders status badge.
 */
function ProductService_RenderStatusBadge(isActived) {
    if (isActived === true || isActived === "true" || isActived === 1) {
        return '<span class="ps-badge-status ps-badge-active"><i class="fa fa-check-circle mr-1"></i>Hoạt động</span>';
    } else {
        return '<span class="ps-badge-status ps-badge-inactive"><i class="fa fa-pause-circle mr-1"></i>Tạm dừng</span>';
    }
}

/**
 * Renders direct 1-click action buttons with tooltips.
 */
function ProductService_RenderActions(item) {
    if (!item || item.NodeType !== "P" || !item.ProductServiceID) {
        return "";
    }

    var id = item.ProductServiceID;
    var html = '<div class="ps-action-group">';

    // 1. View Detail
    html += _renderButton(true,
        "ViewProductService",
        "ps-btn-action ps-btn-view",
        "/Cate/ProductService/View/" + id,
        '<i class="fas fa-eye"></i>',
        "Xem chi tiết", 1024);

    // 2. Attachments
    html += _renderButton(true,
        "List",
        "ps-btn-action ps-btn-file",
        "/Cate/ProductServiceGroupFilePath/List?ProductServiceID=" + id,
        '<i class="fas fa-paperclip"></i>',
        "Tài liệu đính kèm", 1024);

    // 3. Edit
    html += _renderButton(true,
        "EditProductService",
        "ps-btn-action ps-btn-edit",
        "/Cate/ProductService/Edit/" + id,
        '<i class="far fa-edit"></i>',
        "Cập nhật", 1024);

    // 4. Delete
    html += _renderButton(true,
        "DeleteProductService",
        "ps-btn-action ps-btn-delete",
        "/Cate/ProductService/Delete/" + id,
        '<i class="far fa-trash-alt"></i>',
        "Xóa");

    html += '</div>';
    return html;
}

/**
 * Tree expand all.
 */
function ProductService_ExpandAll() {
    for (var i = 0; i < _productServiceTreeData.length; i++) {
        _productServiceCollapseState[_productServiceTreeData[i].NodeID] = false;
    }
    ProductService_RenderTreeView();
}

/**
 * Tree collapse all.
 */
function ProductService_CollapseAll() {
    for (var i = 0; i < _productServiceTreeData.length; i++) {
        _productServiceCollapseState[_productServiceTreeData[i].NodeID] = true;
    }
    ProductService_RenderTreeView();
}

// Tree toggle handler
$(document).off("click", ".ps-tree-toggle").on("click", ".ps-tree-toggle", function (e) {
    e.preventDefault();
    var nodeId = $(this).data("node-id");
    _productServiceCollapseState[nodeId] = _productServiceCollapseState[nodeId] !== true;
    ProductService_RenderTreeView();
});

/**
 * Handles modal form success callbacks.
 */
function ProductService_OnProcessSuccess(response, formId) {
    if (response.status != undefined) {
        if ($("#ModalContent #modal_" + formId + " #chkNotDismissModal").is(":checked")) {
            if (response.status != undefined) {
                eval(response.message);
                ProductService_LoadData();
                response.status = undefined;
                var urlAction = $("#ModalContent #modal_" + formId + " form").attr("action");
                $("#ModalContent #modal_" + formId + " #modal-content").load(urlAction, function () {
                    _initElement();
                });
            }
        } else {
            $("#ModalContent #modal_" + formId).modal("hide");
            $("#ModalContent #modal_" + formId).on("hidden.bs.modal", function () {
                if (response.status != undefined) {
                    eval(response.message);
                    ProductService_LoadData();
                    response.status = undefined;
                }
            });
        }
    } else {
        $("#ModalContent #modal_" + formId + " #bodyForm").html(response);
    }
}

/**
 * Initializes parent selector inside Add/Edit modal.
 */
function ProductService_InitParentSelector() {
    var $selector = $("#ProductService_HierarchySelection");
    if ($selector.length === 0) return;

    var $groupInput = $("#ProductService_GroupServiceID");
    var $parentInput = $("#ProductService_ParentProductID");

    function syncHiddenFields() {
        var selectedValue = $selector.val();
        var $selectedOption = $selector.find("option:selected");

        if (!selectedValue) {
            $groupInput.val("");
            $parentInput.val("0");
            return;
        }

        var nodeType = $selectedOption.data("node-type");
        var groupId = $selectedOption.data("group-id");
        var parentProductId = $selectedOption.data("parent-product-id");

        $groupInput.val(groupId || "");

        if (nodeType === "P") {
            $parentInput.val(parentProductId || "0");
        } else {
            $parentInput.val("0");
        }
    }

    var selectedValue = "";
    if (parseInt($parentInput.val() || "0", 10) > 0) {
        selectedValue = "P_" + $parentInput.val();
    } else if (parseInt($groupInput.val() || "0", 10) > 0) {
        selectedValue = "G_" + $groupInput.val();
    }

    if (selectedValue) {
        $selector.val(selectedValue);
    }

    syncHiddenFields();

    $selector.off("change").on("change", function () {
        syncHiddenFields();
    });
}
