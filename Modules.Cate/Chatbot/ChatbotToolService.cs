using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Modules.Cate.Chatbot
{
    /// <summary>Read-only handlers for DigitalSales data. Never query detail before the scoped list grants access.</summary>
    public sealed class ChatbotToolService
    {
        public static readonly IReadOnlyCollection<string> AllowedTools = Array.AsReadOnly(new[]
        {
            "get_digitalsales_summary", "get_digitalsales_detail",
            "get_project_summary", "get_project_detail",
            "get_opportunity_summary", "get_opportunity_detail"
        });
        private const int MaximumRows = 5000;
        private readonly IChatbotData data;
        public ChatbotToolService() : this(new ChatbotData()) { }
        public ChatbotToolService(IChatbotData data) { this.data = data ?? throw new ArgumentNullException(nameof(data)); }

        public object Execute(string toolName, string subjectId, ChatbotToolInput input, CancellationToken cancellation)
        {
            if (!AllowedTools.Contains(toolName)) throw new ChatbotToolException(HttpStatusCode.Forbidden, "tool_not_allowed", "Tool is not allowed.");
            if (string.IsNullOrWhiteSpace(subjectId)) throw new ChatbotToolException(HttpStatusCode.Unauthorized, "unauthorized", "Missing authenticated subject.");
            cancellation.ThrowIfCancellationRequested();

            bool isProjectAlias = toolName.StartsWith("get_project_", StringComparison.Ordinal);
            if (!data.CanView(subjectId, isProjectAlias))
                throw new ChatbotToolException(HttpStatusCode.Forbidden, "forbidden", "You do not have permission to view this module.");
            cancellation.ThrowIfCancellationRequested();

            bool isDetail = toolName.EndsWith("_detail", StringComparison.Ordinal);
            byte defaultType = isProjectAlias ? (byte)2 : (toolName.StartsWith("get_opportunity_", StringComparison.Ordinal) ? (byte)1 : (byte)0);

            return isDetail
                ? DigitalSalesDetail(subjectId, input, defaultType, cancellation)
                : DigitalSalesSummary(subjectId, input, defaultType, cancellation);
        }

        private object DigitalSalesSummary(string subject, ChatbotToolInput input, byte defaultType, CancellationToken cancellation)
        {
            int total;
            var filter = new RM_DigitalSalesSearchModel
            {
                BusinessType = input.BusinessType ?? defaultType, // 0: Tất cả, 1: Cơ hội, 2: Dự án
                UserName = subject,
                Keyword = input.Keyword,
                CustomerID = input.CustomerId ?? 0,
                DepartmentID = input.DepartmentId ?? 0,
                EmployeeID = input.EmployeeId ?? 0,
                StatusID = input.StatusId ?? 0,
                StatusIDs = null,
                ApplyYear = input.Year ?? 0,
                PageNumber = 1,
                PageSize = MaximumRows + 1,
                IsKeyProject = null,
                IsFollowed = null,
                FilterSpecial = 0
            };
            var rows = data.DigitalSalesList(filter, out total) ?? new List<RM_DigitalSalesModel>();
            cancellation.ThrowIfCancellationRequested();
            Complete(total, rows.Count);
            rows = rows.GroupBy(x => x.DigitalSalesID).Select(x => x.First()).OrderBy(x => x.DigitalSalesID).ToList();

            return new
            {
                asOf = DateTimeOffset.UtcNow,
                appliedFilters = input,
                summary = new
                {
                    totalRecords = rows.Count,
                    totalProjects = rows.Count(x => x.BusinessType == 2),
                    totalOpportunities = rows.Count(x => x.BusinessType == 1),
                    totalExpectedRevenue = rows.Sum(x => x.TotalExpectedRevenue ?? 0m),
                    totalExpectedValue = rows.Sum(x => x.TotalExpectedRevenue ?? 0m),
                    totalActualRevenue = rows.Sum(x => x.TotalActualRevenue ?? 0m),
                    byBusinessType = rows.GroupBy(x => new { x.BusinessType, x.BusinessTypeName })
                        .Select(g => new { businessType = g.Key.BusinessType, businessTypeName = g.Key.BusinessTypeName, count = g.Count(), expectedRevenue = g.Sum(x => x.TotalExpectedRevenue ?? 0m), actualRevenue = g.Sum(x => x.TotalActualRevenue ?? 0m) }).ToArray(),
                    byStatus = rows.GroupBy(x => new { x.StatusID, x.StatusName })
                        .Select(g => new { statusId = g.Key.StatusID, statusName = g.Key.StatusName, count = g.Count(), expectedRevenue = g.Sum(x => x.TotalExpectedRevenue ?? 0m), actualRevenue = g.Sum(x => x.TotalActualRevenue ?? 0m) }).ToArray()
                },
                items = rows.Skip(input.Offset).Take(input.Limit).Select(DigitalSalesRow).ToArray(),
                total = rows.Count,
                offset = input.Offset,
                limit = input.Limit,
                hasMore = rows.Count > input.Offset + input.Limit
            };
        }

        private object DigitalSalesDetail(string subject, ChatbotToolInput input, byte defaultType, CancellationToken cancellation)
        {
            int total;
            var filter = new RM_DigitalSalesSearchModel
            {
                BusinessType = input.BusinessType ?? defaultType,
                UserName = subject,
                Keyword = input.Keyword,
                CustomerID = input.CustomerId ?? 0,
                DepartmentID = input.DepartmentId ?? 0,
                EmployeeID = input.EmployeeId ?? 0,
                StatusID = input.StatusId ?? 0,
                StatusIDs = null,
                ApplyYear = input.Year ?? 0,
                PageNumber = 1,
                PageSize = MaximumRows + 1,
                IsKeyProject = null,
                IsFollowed = null,
                FilterSpecial = 0
            };
            var rows = data.DigitalSalesList(filter, out total) ?? new List<RM_DigitalSalesModel>();
            cancellation.ThrowIfCancellationRequested();
            Complete(total, rows.Count);
            rows = rows.GroupBy(x => x.DigitalSalesID).Select(x => x.First()).OrderBy(x => x.DigitalSalesID).ToList();

            var candidates = rows.Where(x => !input.Id.HasValue || x.DigitalSalesID == input.Id.Value).ToList();
            if (candidates.Count != 1) return Resolution(candidates.Select(DigitalSalesRow).ToList(), input);
            var selected = candidates[0];
            cancellation.ThrowIfCancellationRequested();
            var model = data.DigitalSalesDetail(selected.DigitalSalesID, subject);
            if (model == null) return Resolution(new List<object>(), input);
            cancellation.ThrowIfCancellationRequested();

            var products = model.Products ?? new List<RM_DigitalSalesProductModel>();
            var tasks = (model.TrackingTasks ?? new List<RM_DigitalSalesTrackingModel>())
                .Where(x => x.TrackingID > 0 && !string.IsNullOrWhiteSpace(x.TaskName))
                .OrderBy(x => x.SortOrder).ThenBy(x => x.TrackingID).ToList();
            var members = model.Members ?? new List<RM_DigitalSalesMemberModel>();
            var activities = model.Activities ?? new List<RM_DigitalSalesActivityModel>();

            var itemRow = DigitalSalesRow(model);
            return new
            {
                status = "ok",
                asOf = DateTimeOffset.UtcNow,
                sales = itemRow,
                project = itemRow,
                opportunity = itemRow,
                description = Text(model.Note),
                note = Text(model.Note),
                contact = new
                {
                    name = Text(model.ContactPersonName),
                    phone = Text(model.ContactPersonPhone),
                    email = Text(model.ContactPersonEmail)
                },
                sourceUrl = Link("/Cate/DigitalSales/Detail/" + selected.DigitalSalesID),
                financials = new
                {
                    expectedRevenue = model.TotalExpectedRevenue ?? products.Sum(x => x.ExpectedRevenue),
                    revenue = model.TotalActualRevenue ?? products.Sum(x => x.ActualRevenue ?? 0m),
                    contractValue = model.ContractValue ?? products.Sum(x => x.ContractRevenueMillion * 1000000m),
                    cost = products.Sum(x => x.TotalCostMillion * 1000000m),
                    profit = products.Sum(x => x.ProfitMillion * 1000000m),
                    basis = "DigitalSales totals"
                },
                taskSummary = new
                {
                    totalTasks = tasks.Count,
                    tasksWithReported100Percent = tasks.Count(x => x.Status == 3),
                    tasksWithoutCompletionPercentage = tasks.Count(x => x.Status == 1),
                    tasksCompleted = tasks.Count(x => x.Status == 3),
                    tasksInProgress = tasks.Count(x => x.Status == 2),
                    tasksPending = tasks.Count(x => x.Status == 1),
                    tasksOverdue = tasks.Count(x => x.IsOverdue == 1 || x.Status == 4),
                    byStatus = tasks.GroupBy(x => new { x.Status, x.TaskStatusName })
                        .Select(g => new { statusId = (int)g.Key.Status, statusName = g.Key.TaskStatusName, count = g.Count() }).ToArray()
                },
                products = Page(products.OrderBy(x => x.SalesProductID).Select(x => (object)new
                {
                    id = x.SalesProductID,
                    productServiceId = x.ProductServiceID,
                    name = Text(x.ProductServiceName),
                    packageName = Text(x.PackageName),
                    quantity = x.Quantity,
                    startDate = x.StartDate,
                    endDate = x.EndDate,
                    expectedRevenue = x.ExpectedRevenue,
                    actualRevenue = x.ActualRevenue,
                    cost = x.TotalCostMillion * 1000000m,
                    profit = x.ProfitMillion * 1000000m,
                    note = Text(x.Note)
                }), input),
                members = Page(members.OrderBy(x => x.MemberID).Select(x => (object)new
                {
                    id = x.MemberID,
                    userId = x.UserID,
                    name = Text(x.FullName),
                    userName = x.UserName,
                    email = x.Email,
                    phone = x.Phone,
                    roleTitle = Text(x.RoleTitle),
                    isAM = x.IsAM
                }), input),
                tasks = Page(tasks.Select(x => (object)new
                {
                    id = x.TrackingID,
                    code = x.TrackingCode,
                    parentId = x.ParentID,
                    name = Text(x.TaskName),
                    assignee = Text(x.AssignedUserName),
                    assignees = Text(x.AssignedUserName),
                    startDate = (DateTime?)x.StartDate,
                    deadline = x.Deadline,
                    endDate = x.Deadline,
                    completedDate = x.CompletedDate,
                    status = (int)x.Status,
                    statusName = x.TaskStatusName,
                    isOverdue = x.IsOverdue == 1,
                    note = Text(x.ResultNote),
                    updatedAt = x.LastModifiedDate ?? x.CreatedDate
                }), input),
                plans = Page(tasks.Select(x => (object)new
                {
                    id = x.TrackingID,
                    name = Text(x.TaskName),
                    content = Text(x.ResultNote),
                    workingDate = x.Deadline ?? x.StartDate,
                    username = Text(x.AssignedUserName)
                }), input),
                activities = Page(activities.OrderByDescending(x => x.ActionDate).ThenByDescending(x => x.ActivityID).Select(x => (object)new
                {
                    id = x.ActivityID,
                    type = x.ActivityType,
                    content = Text(x.Content),
                    actionDate = x.ActionDate,
                    author = Text(x.ActionByName ?? x.ActionBy)
                }), input),
                dataNotes = new[]
                {
                    "Data sourced from DigitalSales ecosystem.",
                    "Text fields are limited to 2000 characters; child lists are independently paginated using offset/limit."
                }
            };
        }

        private static void Complete(int total, int returned)
        {
            if (total > MaximumRows || returned > MaximumRows || total > returned)
                throw new ChatbotToolException(HttpStatusCode.BadRequest, "scope_too_large", "Too many records for a complete report. Narrow keyword/department/status filters; no partial aggregate was calculated.");
        }
        private static object Resolution(List<object> candidates, ChatbotToolInput input) => new
        {
            status = candidates.Count == 0 ? "not_found" : "ambiguous",
            message = candidates.Count == 0 ? "No matching record in your accessible scope." : "Ask the user to select a record, then call this tool with its id.",
            candidates = candidates.Skip(input.Offset).Take(input.Limit).ToArray(),
            total = candidates.Count,
            offset = input.Offset,
            limit = input.Limit,
            hasMore = candidates.Count > input.Offset + input.Limit
        };
        private static object Page(IEnumerable<object> source, ChatbotToolInput input)
        {
            var items = source.ToList();
            return new
            {
                items = items.Skip(input.Offset).Take(input.Limit).ToArray(),
                total = items.Count,
                offset = input.Offset,
                limit = input.Limit,
                hasMore = items.Count > input.Offset + input.Limit
            };
        }
        private static object DigitalSalesRow(RM_DigitalSalesModel x) => new
        {
            id = x.DigitalSalesID,
            code = x.Code,
            name = Text(x.Title),
            businessType = x.BusinessType,
            businessTypeName = Text(x.BusinessTypeName),
            customerId = x.CustomerID,
            customerName = Text(x.CustomerName),
            statusId = x.StatusID,
            statusName = x.StatusName,
            startDate = x.StartDate,
            endDate = x.EndDate,
            progressPercentage = x.ProgressPercentage,
            successProbability = x.ClosingProbability,
            closingProbability = x.ClosingProbability,
            expectedRevenue = x.TotalExpectedRevenue,
            expectedValue = x.TotalExpectedRevenue,
            actualRevenue = x.TotalActualRevenue,
            expectedDate = x.ExpectedDate,
            contractValue = x.ContractValue,
            contractNo = x.ContractNo,
            assignedEmployeeName = Text(x.AssignedEmployeeName),
            departmentName = Text(x.DepartmentName),
            productServiceNames = Text(x.ProductServiceNames)
        };
        private static string Text(string value) => value == null || value.Length <= 2000 ? value : value.Substring(0, 1999) + "…";
        private static string Link(string path) => (System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath ?? "").TrimEnd('/') + path;
    }
}

