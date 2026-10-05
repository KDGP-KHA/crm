using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using Core.Cate.Models;
using Modules.Cate.Chatbot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TSFramework.Libs.Models.Base;

public static class ChatbotTests
{
    private static int passed;
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var path = Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(); Console.WriteLine("PASS: " + passed + " checks (no database/network calls)."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); passed++; }
    private static ChatbotToolInput Input(string name, string json) => ChatbotToolInput.Parse(name, JObject.Parse(json));
    private static JObject Execute(FakeData data, string tool, string json) => JObject.FromObject(new ChatbotToolService(data).Execute(tool, "alice", Input(tool, json), CancellationToken.None));
    private static void Reject(Action action, string code)
    {
        try { action(); throw new Exception("Expected rejection: " + code); }
        catch (ChatbotToolException ex) { Check(ex.Code == code, "Wrong rejection: " + ex.Code); }
    }

    private static void Run()
    {
        Environment.SetEnvironmentVariable("Chatbot_BotId", "test-bot");
        foreach (var tool in ChatbotToolService.AllowedTools)
        {
            Reject(() => Input(tool, "{\"subjectId\":\"admin\"}"), "invalid_input");
            Reject(() => Input(tool, "{\"limit\":51}"), "invalid_input");
            Reject(() => Input(tool, "{\"limit\":1.5}"), "invalid_input");
        }
        Reject(() => Input("get_project_detail", "{}"), "invalid_input");
        Reject(() => Input("get_opportunity_detail", "{\"id\":1,\"keyword\":\"ABC\"}"), "invalid_input");
        Reject(() => Input("get_project_summary", "{\"year\":\"2026\"}"), "invalid_input");
        Reject(() => Input("get_digitalsales_detail", "{}"), "invalid_input");
        Reject(() => Input("get_digitalsales_detail", "{\"id\":1,\"keyword\":\"ABC\"}"), "invalid_input");
        Reject(() => Input("get_digitalsales_summary", "{\"businessType\":5}"), "invalid_input");
        Check(Input("get_project_summary", "{}").Limit == 10, "Default page size");
        Check(Input("get_digitalsales_summary", "{}").Limit == 10, "Default page size for digitalsales");

        var data = new FakeData();
        var dsResult = Execute(data, "get_digitalsales_summary", "{}");
        Check((int)dsResult["total"] == 4 && (int)dsResult["summary"]["totalProjects"] == 2 && (int)dsResult["summary"]["totalOpportunities"] == 2, "DigitalSales combined totals");
        Check((decimal)dsResult["summary"]["totalExpectedRevenue"] == 600, "DigitalSales total expected revenue");
        var dsFiltered = Execute(data, "get_digitalsales_summary", "{\"businessType\":1}");
        Check((int)dsFiltered["total"] == 2 && (int)dsFiltered["summary"]["totalOpportunities"] == 2, "DigitalSales businessType filter");

        var result = Execute(data, "get_project_summary", "{\"limit\":1}");
        Check((int)result["summary"]["totalProjects"] == 2 && result["items"].Count() == 1 && (bool)result["hasMore"], "Aggregate must precede pagination");
        Check(data.LastSubject == "alice", "Authenticated username must reach list query");
        result = Execute(data, "get_project_summary", "{\"customerId\":8}");
        Check((int)result["total"] == 1 && (int)result["items"][0]["customerId"] == 8, "Project customer filter");
        result = Execute(data, "get_opportunity_summary", "{\"limit\":1}");
        Check((decimal)result["summary"]["totalExpectedValue"] == 300 && result["items"].Count() == 1, "Opportunity aggregate must include all pages");
        result = Execute(data, "get_project_detail", "{\"keyword\":\"ABC\"}");
        Check((string)result["status"] == "ambiguous" && data.DetailReads == 0, "Ambiguous names must not load children");
        result = Execute(data, "get_project_detail", "{\"id\":999}");
        Check((string)result["status"] == "not_found" && data.DetailReads == 0, "Unauthorized ID must not load detail");
        result = Execute(data, "get_opportunity_detail", "{\"id\":999}");
        Check((string)result["status"] == "not_found" && data.DetailReads == 0, "Unauthorized opportunity must not load detail");
        result = Execute(data, "get_project_detail", "{\"id\":1}");
        Check((string)result["status"] == "ok" && (int)result["taskSummary"]["totalTasks"] == 3, "Project detail task count");
        Check((int)result["taskSummary"]["tasksWithoutCompletionPercentage"] == 1, "Missing progress is not zero");
        var taskItem = result["tasks"]["items"].Single(x => (int)x["id"] == 1);
        Check((int)taskItem["id"] == 1 && taskItem["deadline"].Type != JTokenType.Null, "Tracking task deadline check");
        result = Execute(data, "get_opportunity_detail", "{\"id\":11}");
        Check((string)result["status"] == "ok" && result["activities"] != null && result["plans"] != null, "Opportunity detail sections");
        var dsDetail = Execute(data, "get_digitalsales_detail", "{\"id\":1}");
        Check((string)dsDetail["status"] == "ok" && dsDetail["sales"] != null && (int)dsDetail["sales"]["businessType"] == 2, "DigitalSales detail sales object");
        var dsOppDetail = Execute(data, "get_digitalsales_detail", "{\"id\":11}");
        Check((string)dsOppDetail["status"] == "ok" && (int)dsOppDetail["sales"]["businessType"] == 1, "DigitalSales opportunity detail");
        data.Permission = false;
        var reads = data.ListReads;
        Reject(() => Execute(data, "get_project_summary", "{}"), "forbidden");
        Check(data.ListReads == reads, "Module permission must precede data queries");
        data.Permission = true;
        data.ReportedTotal = 5001;
        Reject(() => Execute(data, "get_project_summary", "{}"), "scope_too_large");
        Reject(() => Execute(data, "get_opportunity_summary", "{}"), "scope_too_large");
        Reject(() => Execute(data, "get_digitalsales_summary", "{}"), "scope_too_large");

        DateTimeOffset expires;
        var token = ChatbotIntegration.IssueCapability("alice", out expires);
        ChatbotCapability capability;
        Check(ChatbotIntegration.TryReadCapability(token, out capability) && capability.SubjectId == "alice", "Capability round trip");
        Check(!ChatbotIntegration.TryReadCapability("invalid", out capability), "Malformed capability");
        var raw = HttpServerUtility.UrlTokenDecode(token); raw[raw.Length / 2] ^= 1;
        Check(!ChatbotIntegration.TryReadCapability(HttpServerUtility.UrlTokenEncode(raw), out capability), "Tampered capability");
        var expired = Protect(new ChatbotCapability { SubjectId = "alice", BotId = "test-bot", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        Check(!ChatbotIntegration.TryReadCapability(expired, out capability), "Expired capability");
        var otherBot = Protect(new ChatbotCapability { SubjectId = "alice", BotId = "other", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) });
        Check(!ChatbotIntegration.TryReadCapability(otherBot, out capability), "Wrong bot capability");
        Check(Call(null, token).StatusCode == HttpStatusCode.BadRequest, "Null request");
        Check(Call(Request("bob"), token).StatusCode == HttpStatusCode.Unauthorized, "Subject substitution");
        Check(Call(Request("alice"), expired).StatusCode == HttpStatusCode.Unauthorized, "Expired HTTP capability");
        Check(Call(Request("alice"), token, "different-id").StatusCode == HttpStatusCode.BadRequest, "Idempotency mismatch");
        Check(Call(Request("alice"), token, "call", true).StatusCode == HttpStatusCode.BadRequest, "Duplicate idempotency headers");
        var unsupported = Request("alice"); unsupported.ToolName = "delete_project";
        Check(Call(unsupported, token).StatusCode == HttpStatusCode.Forbidden, "Unknown tool");
        var invalid = Request("alice"); invalid.Input = JObject.Parse("{\"username\":\"admin\"}");
        var bad = Call(invalid, token);
        Check(bad.StatusCode == HttpStatusCode.BadRequest && bad.Content.Headers.ContentType.MediaType == "application/json" && bad.Headers.CacheControl.NoStore, "Validation response contract");
    }

    private static string Protect(ChatbotCapability value) => HttpServerUtility.UrlTokenEncode(MachineKey.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)), "CRM.Chatbot.Tools.v1"));
    private static ChatbotToolRequest Request(string subject) => new ChatbotToolRequest { Version = 1, BotId = "test-bot", SubjectId = subject, ToolCallId = "call", ToolName = "get_project_summary", Input = new JObject() };
    private static HttpResponseMessage Call(ChatbotToolRequest body, string token, string key = "call", bool duplicate = false)
    {
        using (var controller = new ChatbotToolsController())
        {
            controller.Configuration = new HttpConfiguration();
            controller.Request = new HttpRequestMessage(HttpMethod.Post, "https://crm.test/api/Chatbot/Tools");
            controller.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            controller.Request.Headers.TryAddWithoutValidation("Idempotency-Key", duplicate ? new[] { key, key } : new[] { key });
            return controller.Tools(body, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}

public sealed class FakeData : IChatbotData
{
    public bool Permission = true;
    public int DetailReads, ListReads;
    public int? ReportedTotal;
    public string LastSubject;
    private readonly List<RM_DigitalSalesModel> items = new List<RM_DigitalSalesModel>
    {
        new RM_DigitalSalesModel
        {
            DigitalSalesID = 1,
            Title = "ABC",
            BusinessType = 2, // Project
            CustomerID = 7,
            CustomerName = "Cust 7",
            StatusID = 1,
            StatusName = "Khoi tao",
            TotalExpectedRevenue = 100,
            TotalActualRevenue = 90,
            TrackingTasks = new List<RM_DigitalSalesTrackingModel>
            {
                new RM_DigitalSalesTrackingModel { TrackingID = 1, TaskName = "Task 1", Status = 3, TaskStatusName = "Hoan thanh", Deadline = DateTime.UtcNow.AddDays(5), CompletedDate = DateTime.UtcNow },
                new RM_DigitalSalesTrackingModel { TrackingID = 2, TaskName = "Task 2", Status = 1, TaskStatusName = "Chua lam" },
                new RM_DigitalSalesTrackingModel { TrackingID = 3, TaskName = "Task 3", Status = 2, TaskStatusName = "Dang lam" }
            },
            Products = new List<RM_DigitalSalesProductModel>
            {
                new RM_DigitalSalesProductModel { SalesProductID = 10, ProductServiceName = "Prod 1", ExpectedRevenue = 100, ActualRevenue = 90 }
            },
            Members = new List<RM_DigitalSalesMemberModel>
            {
                new RM_DigitalSalesMemberModel { MemberID = 20, FullName = "Member 1", RoleTitle = "AM" }
            }
        },
        new RM_DigitalSalesModel
        {
            DigitalSalesID = 2,
            Title = "ABC 2",
            BusinessType = 2, // Project
            CustomerID = 8,
            CustomerName = "Cust 8",
            StatusID = 2,
            StatusName = "Dang lam",
            TotalExpectedRevenue = 200,
            TotalActualRevenue = 150
        },
        new RM_DigitalSalesModel
        {
            DigitalSalesID = 11,
            Title = "Opp 1",
            BusinessType = 1, // Opportunity
            CustomerID = 7,
            StatusID = 1,
            StatusName = "Tiep can",
            TotalExpectedRevenue = 100,
            Activities = new List<RM_DigitalSalesActivityModel>
            {
                new RM_DigitalSalesActivityModel { ActivityID = 30, Content = "Gap khach hang", ActionDate = DateTime.UtcNow }
            },
            TrackingTasks = new List<RM_DigitalSalesTrackingModel>
            {
                new RM_DigitalSalesTrackingModel { TrackingID = 40, TaskName = "Lap phuong an", Status = 2 }
            }
        },
        new RM_DigitalSalesModel
        {
            DigitalSalesID = 12,
            Title = "Opp 2",
            BusinessType = 1, // Opportunity
            CustomerID = 8,
            StatusID = 2,
            StatusName = "Bao gia",
            TotalExpectedRevenue = 200
        }
    };

    public bool CanView(string subject, bool project) => Permission;

    public List<RM_DigitalSalesModel> DigitalSalesList(RM_DigitalSalesSearchModel filter, out int total)
    {
        LastSubject = filter.UserName;
        ListReads++;
        var filtered = filter.BusinessType > 0 ? items.Where(x => x.BusinessType == filter.BusinessType) : items.AsEnumerable();
        if (filter.CustomerID > 0) filtered = filtered.Where(x => x.CustomerID == filter.CustomerID);
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) filtered = filtered.Where(x => x.Title.Contains(filter.Keyword));
        var list = filtered.ToList();
        total = ReportedTotal ?? list.Count;
        return list;
    }

    public RM_DigitalSalesModel DigitalSalesDetail(int id, string subject)
    {
        DetailReads++;
        return items.Single(x => x.DigitalSalesID == id);
    }
}
