using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Web.Http;
using System.Xml;
using Core.Cate.Models;
using Modules.Cate.Chatbot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugable.SQLProcedureAuthority;
using Plugable.SQLProcedureProcessor;
using TSFramework.Libs.Interfaces;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Providers;

public static class DatabaseTests
{
    private static int passed, failed;
    private static string stage = "bootstrap";
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            foreach (var dir in args.Skip(1))
            {
                var path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { Run(args[0]); }
        catch (Exception ex) { Fail(ex); }
        Console.WriteLine("DATABASE CHECKS: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException(name); passed++; }
    private static void Fail(Exception ex)
    {
        failed++;
        var message = ex.GetBaseException().Message;
        foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
        {
            if (!string.IsNullOrEmpty(item.ConnectionString)) message = message.Replace(item.ConnectionString, "[connection redacted]");
            try { var password = new SqlConnectionStringBuilder(item.ConnectionString).Password; if (!string.IsNullOrEmpty(password)) message = message.Replace(password, "[redacted]"); } catch { }
        }
        Console.WriteLine("FAIL " + stage + ": " + ex.GetBaseException().GetType().Name + " - " + message);
    }

    private static void Bootstrap(string webRoot)
    {
        var maps = new Dictionary<string, Dictionary<string, string>>();
        foreach (var path in Directory.GetFiles(Path.Combine(webRoot, "App_Data"), "*.xml", SearchOption.AllDirectories))
        {
            var xml = new XmlDocument(); xml.Load(path);
            foreach (XmlElement group in xml.SelectNodes("/Provider/StoredProcedures"))
            {
                string provider = group.GetAttribute("ProviderName");
                if (!maps.ContainsKey(provider)) maps[provider] = new Dictionary<string, string>();
                foreach (XmlElement proc in group.SelectNodes("Procedure"))
                    if (!maps[provider].ContainsKey(proc.GetAttribute("Name"))) maps[provider].Add(proc.GetAttribute("Name"), proc.GetAttribute("Value"));
            }
        }
        var providers = maps.ToDictionary(x => x.Key, x => new SQLProcProcessor().Instance(x.Key, x.Value));
        var store = new StoreProcedureProvider();
        typeof(StoreProcedureProvider).GetProperty("DicStoreProceduresProvider").SetValue(store, providers, null);
        typeof(AppProcessor).GetField("_storeProceduror", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, store);
        var auth = (AuthorityProvider)FormatterServices.GetUninitializedObject(typeof(AuthorityProvider));
        typeof(AuthorityProvider).GetField("_authenticator", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(auth, new SQLProcAuthority { ProcedureProvider = providers["CenIT.Provider.Sys"] });
        typeof(AppProcessor).GetField("_author", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, auth);
        Environment.SetEnvironmentVariable("Chatbot_BotId", "crm-database-test");
    }

    private static void Run(string webRoot)
    {
        Bootstrap(webRoot);
        var users = new List<string>();
        using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["TOC.Conn.Major"].ConnectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandTimeout = 12;
                command.CommandText = @"SELECT TOP (20) u.UserName FROM Sys_Users u
WHERE u.IsActive=1 AND ISNULL(u.IsDeleted,0)=0
ORDER BY (SELECT COUNT(*) FROM RM_DigitalSales ds WHERE ds.CreatedBy=u.UserName AND ds.IsDeleted=0) DESC, u.UserId";
                using (var reader = command.ExecuteReader()) while (reader.Read()) users.Add(reader.GetString(0));
            }
        }
        var data = new ChatbotData();
        var tested = new List<Tuple<string, HashSet<int>, HashSet<int>>>();
        int userIndex = 0;
        foreach (var user in users)
        {
            stage = "select eligible account";
            bool canProject = data.CanView(user, true), canOpportunity = data.CanView(user, false);
            if (!canProject || !canOpportunity) continue;
            if (++userIndex > 3) break;
            string alias = "U" + userIndex;
            int projectTotal, opportunityTotal;
            stage = alias + " scoped list procedures";
            var projects = data.DigitalSalesList(new RM_DigitalSalesSearchModel { BusinessType = 2, UserName = user, PageNumber = 1, PageSize = 5001, ApplyYear = 0 }, out projectTotal) ?? new List<RM_DigitalSalesModel>();
            var opportunities = data.DigitalSalesList(new RM_DigitalSalesSearchModel { BusinessType = 1, UserName = user, PageNumber = 1, PageSize = 5001, ApplyYear = 0 }, out opportunityTotal) ?? new List<RM_DigitalSalesModel>();
            Check(projects != null && opportunities != null, "List procedures must return a collection");
            Check(projectTotal == projects.Count && opportunityTotal == opportunities.Count, "Complete scoped lists");
            tested.Add(Tuple.Create(user, new HashSet<int>(projects.Select(x => x.DigitalSalesID)), new HashSet<int>(opportunities.Select(x => x.DigitalSalesID))));
            int projectId = RichRecord(projects.Select(x => x.DigitalSalesID), true);
            int opportunityId = RichRecord(opportunities.Select(x => x.DigitalSalesID), false);
            foreach (var tool in ChatbotToolService.AllowedTools)
            {
                stage = alias + " " + tool;
                try
                {
                    bool isDigitalSales = tool.StartsWith("get_digitalsales_");
                    bool project = tool.StartsWith("get_project_"), detail = tool.EndsWith("_detail");
                    var input = new JObject { ["limit"] = 1 };
                    if (detail)
                    {
                        int idToUse = isDigitalSales ? (projectId > 0 ? projectId : opportunityId) : (project ? projectId : opportunityId);
                        if (idToUse <= 0) { Console.WriteLine("SKIP " + stage + ": no accessible record"); continue; }
                        input["id"] = idToUse;
                    }
                    var watch = Stopwatch.StartNew();
                    var result = new ChatbotToolService().Execute(tool, user, ChatbotToolInput.Parse(tool, input), CancellationToken.None);
                    var obj = JObject.FromObject(result);
                    Check(watch.ElapsedMilliseconds < 12000, "Within executor time budget");
                    if (!detail)
                    {
                        int expectedTotal = isDigitalSales ? (projectTotal + opportunityTotal) : (project ? projectTotal : opportunityTotal);
                        Check((int)obj["total"] == expectedTotal, "Summary count matches scoped procedure");
                        Check(obj["items"].Count() <= 1, "Page size respected");
                        if (!project && !isDigitalSales) Check((decimal)obj["summary"]["totalExpectedValue"] == opportunities.Sum(x => x.TotalExpectedRevenue ?? 0m), "Expected value across all rows");
                    }
                    else
                    {
                        Check((string)obj["status"] == "ok", "Accessible detail returns ok");
                        int targetId = (int)input["id"];
                        bool treatAsProject = project || (isDigitalSales && targetId == projectId);
                        if (treatAsProject)
                        {
                            var detailModel = data.DigitalSalesDetail(projectId, user);
                            var tasks = (detailModel?.TrackingTasks ?? new List<RM_DigitalSalesTrackingModel>()).Where(x => x.TrackingID > 0 && !string.IsNullOrWhiteSpace(x.TaskName)).ToList();
                            Check((int)obj["taskSummary"]["totalTasks"] == tasks.Count, "Real work-item count");
                            Check((int)obj["tasks"]["total"] == tasks.Count && obj["tasks"]["items"].Count() <= 1, "Real task pagination");
                            Console.WriteLine("DETAIL " + alias + ": tasks=" + tasks.Count + ", products=" + obj["products"]["total"] + ", members=" + obj["members"]["total"]);
                        }
                        else
                        {
                            var detailModel = data.DigitalSalesDetail(opportunityId, user);
                            var activities = (detailModel?.Activities ?? new List<RM_DigitalSalesActivityModel>()).ToList();
                            Check((int)obj["activities"]["total"] == activities.Count && obj["activities"]["items"].Count() <= 1, "Real activity pagination");
                            Console.WriteLine("DETAIL " + alias + ": activities=" + activities.Count + ", plans=" + obj["plans"]["total"] + ", members=" + obj["members"]["total"]);
                        }
                    }
                    int bytes = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(new { result }));
                    Check(bytes < 256*1024, "Response within 256 KB");
                    Console.WriteLine("PASS " + stage + ": " + watch.ElapsedMilliseconds + "ms, " + bytes + " bytes, scope=" + (project ? projectTotal : opportunityTotal));
                    using (var controller = new ChatbotToolsController())
                    {
                        DateTimeOffset expires;
                        var token = ChatbotIntegration.IssueCapability(user, out expires);
                        controller.Configuration = new HttpConfiguration();
                        controller.Request = new HttpRequestMessage(HttpMethod.Post, "https://crm.test/api/Chatbot/Tools");
                        controller.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        controller.Request.Headers.Add("Idempotency-Key", "db-test");
                        var response = controller.Tools(new ChatbotToolRequest { Version=1, BotId="crm-database-test", SubjectId=user, ToolCallId="db-test", ToolName=tool, Input=input }, CancellationToken.None).GetAwaiter().GetResult();
                        Check(response.StatusCode == HttpStatusCode.OK, "Controller status " + response.StatusCode);
                        var envelope = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        Check(envelope["result"] != null, "Controller result envelope");
                    }
                }
                catch (Exception ex) { Fail(ex); }
            }
            foreach (bool project in new[] { true, false })
            {
                var tool = project ? "get_project_summary" : "get_opportunity_summary";
                stage = alias + " " + tool + " filters/pagination";
                var expectedIds = project ? projects.Select(x => x.DigitalSalesID).OrderBy(x => x).ToArray() : opportunities.Select(x => x.DigitalSalesID).OrderBy(x => x).ToArray();
                var secondPage = JObject.FromObject(new ChatbotToolService().Execute(tool, user,
                    ChatbotToolInput.Parse(tool, new JObject { ["limit"] = 1, ["offset"] = 1 }), CancellationToken.None));
                if (expectedIds.Length > 1) Check((int)secondPage["items"][0]["id"] == expectedIds[1], "Second page matches full scoped list");
                if (expectedIds.Length == 0) continue;
                int status = project ? projects[0].StatusID : opportunities[0].StatusID;
                if (status <= 0) continue;
                var filtered = JObject.FromObject(new ChatbotToolService().Execute(tool, user,
                    ChatbotToolInput.Parse(tool, new JObject { ["statusId"] = status }), CancellationToken.None));
                Check((int)filtered["total"] == (project ? projects.Count(x => x.StatusID == status) : opportunities.Count(x => x.StatusID == status)), "Status filter agrees with unfiltered scope");
            }
        }
        Check(tested.Count >= 2, "At least two real accounts must be tested");
        foreach (bool project in new[] { true, false })
        {
            bool checkedOutside = false;
            foreach (var target in tested)
            {
                var own = project ? target.Item2 : target.Item3;
                var outside = tested.SelectMany(x => project ? x.Item2 : x.Item3).FirstOrDefault(id => !own.Contains(id));
                if (outside == 0) continue;
                stage = project ? "project outside user scope" : "opportunity outside user scope";
                var name = project ? "get_project_detail" : "get_opportunity_detail";
                var result = JObject.FromObject(new ChatbotToolService().Execute(name, target.Item1,
                    ChatbotToolInput.Parse(name, new JObject { ["id"] = outside }), CancellationToken.None));
                Check((string)result["status"] == "not_found", "Outside-scope ID must not expose details");
                Console.WriteLine("PASS " + stage); checkedOutside = true; break;
            }
            if (!checkedOutside) Console.WriteLine("SKIP outside-scope check: sampled accounts share this scope");
        }
    }

    private static int RichRecord(IEnumerable<int> ids, bool project)
    {
        var allowed = ids.ToArray();
        if (allowed.Length == 0) return 0;
        using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["TOC.Conn.Major"].ConnectionString))
        using (var command = connection.CreateCommand())
        {
            connection.Open(); command.CommandTimeout = 12;
            string idList = string.Join(",", allowed.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            command.CommandText = project
                ? "SELECT TOP (1) ds.DigitalSalesID FROM RM_DigitalSales ds WHERE ds.DigitalSalesID IN (" + idList + ") ORDER BY (SELECT COUNT(*) FROM RM_DigitalSalesTracking t WHERE t.DigitalSalesID=ds.DigitalSalesID)+(SELECT COUNT(*) FROM RM_DigitalSalesProduct p WHERE p.DigitalSalesID=ds.DigitalSalesID) DESC, ds.DigitalSalesID"
                : "SELECT TOP (1) ds.DigitalSalesID FROM RM_DigitalSales ds WHERE ds.DigitalSalesID IN (" + idList + ") ORDER BY (SELECT COUNT(*) FROM RM_DigitalSalesActivity a WHERE a.DigitalSalesID=ds.DigitalSalesID)+(SELECT COUNT(*) FROM RM_DigitalSalesTracking t WHERE t.DigitalSalesID=ds.DigitalSalesID) DESC, ds.DigitalSalesID";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
