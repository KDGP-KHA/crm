using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using Core.Cate.Caches;
using Core.Cate.Models;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_DataMigrationBiz
    {
        private readonly RM_DigitalSalesBiz _salesBiz = new RM_DigitalSalesBiz();

        private string GetConnectionString()
        {
            var conn = ConfigurationManager.ConnectionStrings["TOC.Conn.Major"]?.ConnectionString;
            if (string.IsNullOrEmpty(conn))
                conn = ConfigurationManager.ConnectionStrings["TOC.Sys.Conn"]?.ConnectionString;
            if (string.IsNullOrEmpty(conn))
                conn = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=200; Pooling=true; Max Pool Size=200;";
            return conn;
        }

        #region Preview Methods

        public DataMigrationPreviewModel Preview(int sourceType, int sourceId)
        {
            var result = new DataMigrationPreviewModel
            {
                SourceType = sourceType,
                SourceID = sourceId,
                Success = false
            };

            if (sourceId <= 0)
            {
                result.Message = "Vui lòng nhập ID hợp lệ lớn hơn 0.";
                return result;
            }

            var connStr = GetConnectionString();
            if (string.IsNullOrEmpty(connStr))
            {
                result.Message = "Không tìm thấy chuỗi kết nối cơ sở dữ liệu.";
                return result;
            }

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    if (sourceType == 1) // Cơ hội kinh doanh
                    {
                        result.SourceTypeName = "Cơ hội kinh doanh";
                        result.SuggestedChecklist = "UNCAPTURED; APPROACHING";

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT bo.BusinessOpportunityID, bo.CodeOpportunity, bo.OpportunityName,
       bo.CustomerID, c.CustomerName, bo.ContactPerson_ID, cp.FullName AS ContactPersonName,
       bo.ExpectedValue, bo.ClosingProbability, bo.Description,
       bo.CreatedBy, bo.CreatedDate, u.FullName AS AMName
FROM RM_BusinessOpportunity bo
LEFT JOIN RM_Customer c ON bo.CustomerID = c.CustomerID
LEFT JOIN RM_ContactPersons cp ON bo.ContactPerson_ID = cp.ContactPerson_ID
LEFT JOIN Sys_Users u ON bo.CreatedBy = u.UserName
WHERE bo.BusinessOpportunityID = @ID AND bo.IsDeleted = 0";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;

                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read())
                                {
                                    result.Message = $"Không tìm thấy Cơ hội kinh doanh với ID = {sourceId}.";
                                    return result;
                                }

                                result.SourceCode = r["CodeOpportunity"]?.ToString();
                                if (string.IsNullOrWhiteSpace(result.SourceCode)) result.SourceCode = $"BO{sourceId}";
                                string rawOppName = r["OpportunityName"]?.ToString() ?? "";
                                result.Title = !string.IsNullOrWhiteSpace(result.SourceCode) && !rawOppName.EndsWith($"({result.SourceCode})")
                                    ? $"{rawOppName} ({result.SourceCode})".Trim()
                                    : rawOppName;
                                if (r["CustomerID"] != DBNull.Value) result.CustomerID = Convert.ToInt32(r["CustomerID"]);
                                result.CustomerName = r["CustomerName"]?.ToString();
                                if (r["ContactPerson_ID"] != DBNull.Value) result.ContactPersonID = Convert.ToInt32(r["ContactPerson_ID"]);
                                result.ContactPersonName = r["ContactPersonName"]?.ToString();
                                if (r["ExpectedValue"] != DBNull.Value) result.ExpectedRevenue = Convert.ToDecimal(r["ExpectedValue"]) * 1000000m; // Triệu -> VNĐ
                                result.AMName = r["AMName"]?.ToString() ?? r["CreatedBy"]?.ToString();
                                result.Description = CleanHtml(r["Description"]?.ToString());
                                result.CreatedBy = r["CreatedBy"]?.ToString();
                                if (r["CreatedDate"] != DBNull.Value) result.CreatedDate = Convert.ToDateTime(r["CreatedDate"]);
                            }
                        }

                        // Members
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT u.FullName, u.UserName,
       STUFF((
           SELECT DISTINCT ', ' + r.RoleName
           FROM RM_SalesTeamMembers stm2
           INNER JOIN RM_Roles r ON (';' + ISNULL(stm2.RoleID, '') + ';' LIKE '%;' + CAST(r.RoleID AS VARCHAR(10)) + ';%')
           WHERE stm2.BusinessOpportunityID = @ID AND stm2.EmployeeID = u.UserID AND stm2.IsDeleted = 0 AND r.IsDeleted = 0
           FOR XML PATH(''), TYPE
       ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS RoleNames
FROM (
    SELECT DISTINCT stm.EmployeeID
    FROM RM_SalesTeamMembers stm
    WHERE stm.BusinessOpportunityID = @ID AND stm.IsDeleted = 0
) m
INNER JOIN Sys_Users u ON m.EmployeeID = u.UserID
ORDER BY u.FullName";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string roles = r["RoleNames"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["RoleNames"].ToString())
                                        ? r["RoleNames"].ToString()
                                        : "Thành viên";
                                    result.MembersSummary.Add($"{r["FullName"]} ({r["UserName"]}) - {roles}");
                                }
                            }
                            result.MemberCount = result.MembersSummary.Count;
                        }

                        // Products
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT ProductServiceIDs FROM RM_BusinessOpportunity WHERE BusinessOpportunityID = @ID";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            string rawProdIds = cmd.ExecuteScalar()?.ToString();
                            if (!string.IsNullOrWhiteSpace(rawProdIds))
                            {
                                var pIdList = rawProdIds.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(s => s.Trim())
                                                        .Where(s => int.TryParse(s, out _))
                                                        .ToList();
                                if (pIdList.Count > 0)
                                {
                                    string inClause = string.Join(",", pIdList);
                                    using (var cmdP = conn.CreateCommand())
                                    {
                                        cmdP.CommandText = $"SELECT ProductServiceID, NameProduct FROM RM_ProductService WHERE ProductServiceID IN ({inClause})";
                                        using (var r = cmdP.ExecuteReader())
                                        {
                                            while (r.Read())
                                            {
                                                result.ProductsSummary.Add(r["NameProduct"]?.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                            result.ProductCount = result.ProductsSummary.Count;
                        }

                        // Activities & Attachments
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT COUNT(eh.ExchangeHistoryID) AS ActCount,
       COUNT(ef.FilePathID) AS FileCount
FROM RM_ExchangeHistory eh
LEFT JOIN RM_ExchangeHistoryFilePath ef ON eh.ExchangeHistoryID = ef.ExchangeHistoryID AND ef.IsDeleted = 0
WHERE eh.BusinessOpportunityID = @ID";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    result.ActivityCount = Convert.ToInt32(r["ActCount"]);
                                    result.AttachmentCount = Convert.ToInt32(r["FileCount"]);
                                }
                            }
                        }

                        // Attachments summary
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT ef.FilePath
FROM RM_ExchangeHistory eh
INNER JOIN RM_ExchangeHistoryFilePath ef ON eh.ExchangeHistoryID = ef.ExchangeHistoryID AND ef.IsDeleted = 0
WHERE eh.BusinessOpportunityID = @ID";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string fp = r["FilePath"]?.ToString();
                                    if (!string.IsNullOrWhiteSpace(fp))
                                    {
                                        result.AttachmentsSummary.Add(Path.GetFileName(fp));
                                    }
                                }
                            }
                        }

                        // Check already converted
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT TOP 1 DigitalSalesID, Code 
FROM RM_DigitalSales 
WHERE IsDeleted = 0 AND (
    Note LIKE '%Cơ hội ID ' + CAST(@ID AS VARCHAR(10)) + '%' OR 
    Note LIKE '%Cơ hội ' + CAST(@ID AS VARCHAR(10)) + '%' OR 
    Title = @Title
)";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 500).Value = (object)result.Title ?? "";
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    result.AlreadyConverted = true;
                                    result.ExistingDigitalSalesID = Convert.ToInt32(r["DigitalSalesID"]);
                                    result.ExistingDigitalSalesCode = r["Code"]?.ToString();
                                }
                            }
                        }
                    }
                    else // Dự án (RM_Project)
                    {
                        result.SourceTypeName = "Dự án";
                        result.SuggestedChecklist = "UNCAPTURED; APPROACHING; FORMATION; IMPLEMENTING";

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT p.ProjectID, p.ProjectName, p.CustomerID, c.CustomerName,
       bo.ContactPerson_ID, cp.FullName AS ContactPersonName,
       p.Note AS Description, p.CreatedBy, p.CreatedDate,
       (SELECT TOP 1 u.FullName FROM RM_ProjectMember pm 
        INNER JOIN RM_ProductProject pp ON pm.ProductProjectID = pp.ProductProjectID 
        INNER JOIN Sys_Users u ON pm.Employee_ID = u.UserID 
        WHERE pp.ProjectID = p.ProjectID AND (';' + ISNULL(pm.RoleID, '') + ';' LIKE '%;5;%') AND pm.IsDeleted = 0) AS AMName,
       (SELECT ISNULL(SUM(pp.ExpectedRevenue), 0) FROM RM_ProductProject pp WHERE pp.ProjectID = p.ProjectID AND pp.IsDeleted = 0) AS ExpectedRevenue
FROM RM_Project p
LEFT JOIN RM_Customer c ON p.CustomerID = c.CustomerID
LEFT JOIN RM_BusinessOpportunity bo ON p.BusinessOpportunityID = bo.BusinessOpportunityID
LEFT JOIN RM_ContactPersons cp ON bo.ContactPerson_ID = cp.ContactPerson_ID
WHERE p.ProjectID = @ID AND p.IsDeleted = 0";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;

                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read())
                                {
                                    result.Message = $"Không tìm thấy Dự án với ID = {sourceId}.";
                                    return result;
                                }

                                result.SourceCode = $"PRJ-{sourceId:D4}";
                                string rawPrjName = r["ProjectName"]?.ToString() ?? "";
                                result.Title = !string.IsNullOrWhiteSpace(result.SourceCode) && !rawPrjName.EndsWith($"({result.SourceCode})")
                                    ? $"{rawPrjName} ({result.SourceCode})".Trim()
                                    : rawPrjName;
                                if (r["CustomerID"] != DBNull.Value) result.CustomerID = Convert.ToInt32(r["CustomerID"]);
                                result.CustomerName = r["CustomerName"]?.ToString();
                                if (r["ContactPerson_ID"] != DBNull.Value) result.ContactPersonID = Convert.ToInt32(r["ContactPerson_ID"]);
                                result.ContactPersonName = r["ContactPersonName"]?.ToString();
                                if (r["ExpectedRevenue"] != DBNull.Value) result.ExpectedRevenue = Convert.ToDecimal(r["ExpectedRevenue"]);
                                result.AMName = r["AMName"]?.ToString() ?? r["CreatedBy"]?.ToString();
                                result.Description = CleanHtml(r["Description"]?.ToString());
                                result.CreatedBy = r["CreatedBy"]?.ToString();
                                if (r["CreatedDate"] != DBNull.Value) result.CreatedDate = Convert.ToDateTime(r["CreatedDate"]);
                            }
                        }

                        // Members
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT u.FullName, u.UserName,
       STUFF((
           SELECT DISTINCT ', ' + r.RoleName
           FROM RM_ProjectMember pm2
           INNER JOIN RM_ProductProject pp2 ON pm2.ProductProjectID = pp2.ProductProjectID
           INNER JOIN RM_Roles r ON (';' + ISNULL(pm2.RoleID, '') + ';' LIKE '%;' + CAST(r.RoleID AS VARCHAR(10)) + ';%')
           WHERE pp2.ProjectID = @ID AND pm2.Employee_ID = u.UserID AND pm2.IsDeleted = 0 AND r.IsDeleted = 0
           FOR XML PATH(''), TYPE
       ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS RoleNames
FROM (
    SELECT DISTINCT pm.Employee_ID
    FROM RM_ProjectMember pm
    INNER JOIN RM_ProductProject pp ON pm.ProductProjectID = pp.ProductProjectID
    WHERE pp.ProjectID = @ID AND pm.IsDeleted = 0
) m
INNER JOIN Sys_Users u ON m.Employee_ID = u.UserID
ORDER BY u.FullName";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string roles = r["RoleNames"] != DBNull.Value && !string.IsNullOrWhiteSpace(r["RoleNames"].ToString())
                                        ? r["RoleNames"].ToString()
                                        : "Thành viên";
                                    result.MembersSummary.Add($"{r["FullName"]} ({r["UserName"]}) - {roles}");
                                }
                            }
                            result.MemberCount = result.MembersSummary.Count;
                        }

                        // Products
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT pp.ProductProjectID, ps.ProductServiceID, ps.NameProduct, pp.ExpectedRevenue
FROM RM_ProductProject pp
LEFT JOIN RM_ProductService ps ON pp.ProductServiceID = ps.ProductServiceID
WHERE pp.ProjectID = @ID AND pp.IsDeleted = 0";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    result.ProductsSummary.Add(r["NameProduct"]?.ToString());
                                }
                            }
                            result.ProductCount = result.ProductsSummary.Count;
                        }

                        // Activities & Tasks
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT 
    (SELECT COUNT(*) FROM RM_TaskManagement tm 
     WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
       AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)) +
    (SELECT COUNT(*) FROM RM_Comment c 
     INNER JOIN RM_TaskManagement tm ON c.TaskManagementID = tm.TaskManagementID 
     WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
       AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL)
       AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)) AS ActCount,
    (SELECT COUNT(*) FROM RM_LogTaskFilePath lf 
     INNER JOIN RM_TaskManagement tm ON lf.TaskManagementID = tm.TaskManagementID 
     WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
       AND (lf.IsDeleted = 0 OR lf.IsDeleted IS NULL)
       AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)) AS FileCount";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    result.ActivityCount = Convert.ToInt32(r["ActCount"]);
                                    result.AttachmentCount = Convert.ToInt32(r["FileCount"]);
                                }
                            }
                        }

                        // Attachments summary for Project
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT lf.FilePath
FROM RM_LogTaskFilePath lf
INNER JOIN RM_TaskManagement tm ON lf.TaskManagementID = tm.TaskManagementID
WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
  AND (lf.IsDeleted = 0 OR lf.IsDeleted IS NULL)
  AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string fp = r["FilePath"]?.ToString();
                                    if (!string.IsNullOrWhiteSpace(fp))
                                    {
                                        result.AttachmentsSummary.Add(Path.GetFileName(fp));
                                    }
                                }
                            }
                        }

                        // Check already converted for Project
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
SELECT TOP 1 DigitalSalesID, Code 
FROM RM_DigitalSales 
WHERE IsDeleted = 0 AND (
    Note LIKE '%Dự án ID ' + CAST(@ID AS VARCHAR(10)) + '%' OR 
    Note LIKE '%Dự án ' + CAST(@ID AS VARCHAR(10)) + '%' OR 
    Title = @Title
)";
                            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = sourceId;
                            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 500).Value = (object)result.Title ?? "";
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    result.AlreadyConverted = true;
                                    result.ExistingDigitalSalesID = Convert.ToInt32(r["DigitalSalesID"]);
                                    result.ExistingDigitalSalesCode = r["Code"]?.ToString();
                                }
                            }
                        }
                    }

                    result.Success = true;
                    result.Message = "Tìm thấy dữ liệu nguồn hợp lệ.";
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex);
                result.Message = "Lỗi khi tra cứu dữ liệu: " + ex.Message;
            }

            return result;
        }

        #endregion

        #region Execution Methods

        public DataMigrationResultModel ExecuteMigration(DataMigrationRequestModel model, string username)
        {
            var result = new DataMigrationResultModel
            {
                Success = false
            };

            if (model == null || model.SourceID <= 0)
            {
                result.Message = "Thông tin yêu cầu không hợp lệ.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(model.StatusTransitionCodes))
            {
                result.Message = "Vui lòng nhập chuỗi mã trạng thái checklist (ví dụ: UNCAPTURED; APPROACHING).";
                return result;
            }

            var connStr = GetConnectionString();
            if (string.IsNullOrEmpty(connStr))
            {
                result.Message = "Không tìm thấy cấu hình chuỗi kết nối cơ sở dữ liệu.";
                return result;
            }

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // 1. Validate & parse checklist status codes
                    var rawCodes = model.StatusTransitionCodes.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim().ToUpperInvariant())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();

                    if (rawCodes.Count == 0)
                    {
                        result.Message = "Danh sách mã trạng thái không hợp lệ.";
                        return result;
                    }

                    var statusList = new List<RM_DigitalSalesStatusModel>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT StatusID, BusinessType, StatusCode, StatusName, SortOrder FROM RM_DigitalSalesStatus WHERE IsDeleted = 0";
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                statusList.Add(new RM_DigitalSalesStatusModel
                                {
                                    StatusID = Convert.ToInt32(r["StatusID"]),
                                    BusinessType = Convert.ToByte(r["BusinessType"]),
                                    StatusCode = r["StatusCode"]?.ToString(),
                                    StatusName = r["StatusName"]?.ToString(),
                                    SortOrder = Convert.ToInt32(r["SortOrder"])
                                });
                            }
                        }
                    }

                    var resolvedStatuses = new List<RM_DigitalSalesStatusModel>();
                    foreach (var code in rawCodes)
                    {
                        var s = statusList.FirstOrDefault(x => string.Equals(x.StatusCode, code, StringComparison.OrdinalIgnoreCase));
                        if (s == null)
                        {
                            result.Message = $"Mã trạng thái '{code}' không tồn tại trong danh mục RM_DigitalSalesStatus. Các mã hợp lệ: {string.Join(", ", statusList.Select(x => x.StatusCode))}";
                            return result;
                        }
                        resolvedStatuses.Add(s);
                    }

                    // Last status determines current status and business type of the DigitalSales record
                    var finalStatus = resolvedStatuses.Last();
                    byte finalBusinessType = finalStatus.BusinessType;
                    int finalStatusId = finalStatus.StatusID;

                    // Generate next code
                    var newCode = _salesBiz.GenerateNextCode();
                    result.StepLogs.Add($"[1/7] Đã sinh mã SPDV Số tiếp theo: {newCode}");

                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            int newSalesId = 0;
                            DateTime originCreatedDate = DateTime.Now;
                            string originCreatedBy = username;
                            int assignedEmployeeId = 0;

                            if (model.SourceType == 1) // Cơ hội kinh doanh
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
SELECT bo.OpportunityName, bo.CodeOpportunity, bo.CustomerID, bo.ContactPerson_ID,
       ISNULL(bo.ExpectedValue, 0) * 1000000.0 AS ExpectedRevenue,
       ISNULL(bo.ClosingProbability, 50.0) AS ClosingProb,
       bo.Description, bo.CreatedDate, bo.CreatedBy, bo.LastModifiedDate, bo.LastModifiedBy,
       ISNULL((SELECT TOP 1 stm.EmployeeID FROM RM_SalesTeamMembers stm WHERE stm.BusinessOpportunityID = bo.BusinessOpportunityID AND (';' + ISNULL(stm.RoleID, '') + ';' LIKE '%;5;%') AND stm.IsDeleted = 0), ISNULL(u.UserID, 0)) AS AssignedUserID
FROM RM_BusinessOpportunity bo
LEFT JOIN Sys_Users u ON bo.CreatedBy = u.UserName
WHERE bo.BusinessOpportunityID = @ID";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;

                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (!r.Read()) throw new Exception($"Không tìm thấy Cơ hội với ID = {model.SourceID}");

                                        string rawTitle = r["OpportunityName"]?.ToString() ?? "";
                                        string oppCode = r["CodeOpportunity"]?.ToString();
                                        if (string.IsNullOrWhiteSpace(oppCode)) oppCode = $"BO{model.SourceID}";
                                        string title = rawTitle;
                                        if (!string.IsNullOrWhiteSpace(oppCode) && !rawTitle.EndsWith($"({oppCode})"))
                                        {
                                            title = $"{rawTitle} ({oppCode})".Trim();
                                        }
                                        if (title.Length > 500) title = title.Substring(0, 500);
                                        int? customerId = r["CustomerID"] != DBNull.Value ? (int?)Convert.ToInt32(r["CustomerID"]) : null;
                                        int? contactId = r["ContactPerson_ID"] != DBNull.Value ? (int?)Convert.ToInt32(r["ContactPerson_ID"]) : null;
                                        decimal expRev = Convert.ToDecimal(r["ExpectedRevenue"]);
                                        decimal closingProb = Convert.ToDecimal(r["ClosingProb"]);
                                        string desc = CleanHtml(r["Description"]?.ToString());
                                        originCreatedDate = Convert.ToDateTime(r["CreatedDate"]);
                                        originCreatedBy = r["CreatedBy"]?.ToString();
                                        assignedEmployeeId = Convert.ToInt32(r["AssignedUserID"]);
                                        DateTime lastModDate = r["LastModifiedDate"] != DBNull.Value ? Convert.ToDateTime(r["LastModifiedDate"]) : originCreatedDate;
                                        string lastModBy = r["LastModifiedBy"]?.ToString() ?? originCreatedBy;

                                        r.Close();

                                        using (var cmdIns = conn.CreateCommand())
                                        {
                                            cmdIns.Transaction = tran;
                                            cmdIns.CommandText = @"
INSERT INTO RM_DigitalSales (
    Code, Title, BusinessType, StatusID, CustomerID, ContactPerson_ID,
    TotalExpectedRevenue, TotalActualRevenue, ClosingProbability,
    AssignedEmployeeID, Note, IsDeleted, IsKeyProject,
    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy,
    ActionTime, ApplyYear
)
VALUES (
    @Code, @Title, @BusinessType, @StatusID, @CustomerID, @ContactID,
    @ExpectedRev, 0.00, @ClosingProb,
    @AssignedID, @Note, 0, 0,
    @CreatedDate, @CreatedBy, @LastModifiedDate, @LastModifiedBy,
    @ActionTime, @ApplyYear
);
SELECT SCOPE_IDENTITY();";
                                            cmdIns.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = newCode;
                                            cmdIns.Parameters.Add("@Title", SqlDbType.NVarChar, 500).Value = title ?? "";
                                            cmdIns.Parameters.Add("@BusinessType", SqlDbType.TinyInt).Value = finalBusinessType;
                                            cmdIns.Parameters.Add("@StatusID", SqlDbType.Int).Value = finalStatusId;
                                            cmdIns.Parameters.Add("@CustomerID", SqlDbType.Int).Value = (object)customerId ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@ContactID", SqlDbType.Int).Value = (object)contactId ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@ExpectedRev", SqlDbType.Decimal).Value = expRev;
                                            cmdIns.Parameters.Add("@ClosingProb", SqlDbType.Decimal).Value = closingProb;
                                            cmdIns.Parameters.Add("@AssignedID", SqlDbType.Int).Value = assignedEmployeeId > 0 ? (object)assignedEmployeeId : DBNull.Value;
                                            cmdIns.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = (object)desc ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = originCreatedDate;
                                            cmdIns.Parameters.Add("@CreatedBy", SqlDbType.VarChar, 150).Value = originCreatedBy;
                                            cmdIns.Parameters.Add("@LastModifiedDate", SqlDbType.DateTime).Value = lastModDate;
                                            cmdIns.Parameters.Add("@LastModifiedBy", SqlDbType.VarChar, 150).Value = lastModBy;
                                            cmdIns.Parameters.Add("@ActionTime", SqlDbType.DateTime).Value = originCreatedDate;
                                            cmdIns.Parameters.Add("@ApplyYear", SqlDbType.Int).Value = originCreatedDate.Year;

                                            newSalesId = Convert.ToInt32(cmdIns.ExecuteScalar());
                                        }
                                    }
                                }

                                result.StepLogs.Add($"[2/7] Đã khởi tạo hồ sơ RM_DigitalSales ID = {newSalesId}, Loại = {finalBusinessType}, Trạng thái = {finalStatus.StatusName}");

                                // Members
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
INSERT INTO RM_DigitalSalesMember (DigitalSalesID, UserID, RoleTitle, IsAM, Note, IsActive, CreatedDate, CreatedBy)
SELECT 
    @NewSalesID,
    m.EmployeeID,
    LEFT(ISNULL(STUFF((
        SELECT DISTINCT ', ' + r.RoleName
        FROM RM_SalesTeamMembers stm2
        INNER JOIN RM_Roles r ON (';' + ISNULL(stm2.RoleID, '') + ';' LIKE '%;' + CAST(r.RoleID AS VARCHAR(10)) + ';%')
        WHERE stm2.BusinessOpportunityID = @SourceID AND stm2.EmployeeID = m.EmployeeID AND stm2.IsDeleted = 0 AND r.IsDeleted = 0
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), N'Thành viên'), 150),
    MAX(CASE WHEN ';' + ISNULL(m.RoleID, '') + ';' LIKE '%;5;%' THEN 1 ELSE 0 END),
    MAX(CASE WHEN ';' + ISNULL(m.RoleID, '') + ';' LIKE '%;5;%' THEN N'AM phụ trách' ELSE N'Chuyển từ Cơ hội ' + CAST(@SourceID AS VARCHAR(10)) END),
    1,
    MIN(m.CreatedDate),
    MIN(m.CreatedBy)
FROM RM_SalesTeamMembers m
WHERE m.BusinessOpportunityID = @SourceID AND m.IsDeleted = 0
GROUP BY m.EmployeeID";
                                    cmd.Parameters.Add("@NewSalesID", SqlDbType.Int).Value = newSalesId;
                                    cmd.Parameters.Add("@SourceID", SqlDbType.Int).Value = model.SourceID;
                                    int memRows = cmd.ExecuteNonQuery();
                                    result.MigratedMembers = memRows;
                                    result.StepLogs.Add($"[3/7] Đã ánh xạ {memRows} thành viên với vai trò chính xác từ RM_Roles");
                                }

                                // Products
                                string rawProdIds = null;
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = "SELECT ProductServiceIDs FROM RM_BusinessOpportunity WHERE BusinessOpportunityID = @ID";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;
                                    rawProdIds = cmd.ExecuteScalar()?.ToString();
                                }

                                if (!string.IsNullOrWhiteSpace(rawProdIds))
                                {
                                    var pIdList = rawProdIds.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => s.Trim())
                                                            .Where(s => int.TryParse(s, out _))
                                                            .ToList();
                                    if (pIdList.Count > 0)
                                    {
                                        string inClause = string.Join(",", pIdList);
                                        using (var cmd = conn.CreateCommand())
                                        {
                                            cmd.Transaction = tran;
                                            cmd.CommandText = $@"
INSERT INTO RM_DigitalSalesProduct (
    DigitalSalesID, ProductServiceID, ExpectedRevenue, ActualRevenue,
    PackageName, Quantity, IsDeleted, CreatedDate, CreatedBy
)
SELECT 
    @NewSalesID,
    ps.ProductServiceID,
    10000000.00, 0.00,
    ps.NameProduct, 1, 0,
    ISNULL(ps.DateCreated, GETDATE()),
    ISNULL(ps.UserCreated, @Username)
FROM RM_ProductService ps
WHERE ps.ProductServiceID IN ({inClause})";
                                            cmd.Parameters.Add("@NewSalesID", SqlDbType.Int).Value = newSalesId;
                                            cmd.Parameters.Add("@Username", SqlDbType.VarChar, 150).Value = username;
                                            int pRows = cmd.ExecuteNonQuery();
                                            result.StepLogs.Add($"[4/7] Đã ánh xạ {pRows} sản phẩm dịch vụ từ RM_ProductService");
                                        }
                                    }
                                }
                                else
                                {
                                    result.StepLogs.Add("[4/7] Cơ hội không có cấu hình sản phẩm dịch vụ.");
                                }
                            }
                            else // Dự án (RM_Project)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
SELECT p.ProjectName, p.CustomerID, bo.ContactPerson_ID,
       (SELECT ISNULL(SUM(pp.ExpectedRevenue), 0) FROM RM_ProductProject pp WHERE pp.ProjectID = p.ProjectID AND pp.IsDeleted = 0) AS ExpectedRevenue,
       p.Note AS Description, p.CreatedDate, p.CreatedBy, p.LastModifiedDate, p.LastModifiedBy,
       ISNULL((SELECT TOP 1 pm.Employee_ID FROM RM_ProjectMember pm INNER JOIN RM_ProductProject pp ON pm.ProductProjectID = pp.ProductProjectID WHERE pp.ProjectID = p.ProjectID AND (';' + ISNULL(pm.RoleID, '') + ';' LIKE '%;5;%') AND pm.IsDeleted = 0), 0) AS AssignedUserID
FROM RM_Project p
LEFT JOIN RM_BusinessOpportunity bo ON p.BusinessOpportunityID = bo.BusinessOpportunityID
WHERE p.ProjectID = @ID";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;

                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (!r.Read()) throw new Exception($"Không tìm thấy Dự án với ID = {model.SourceID}");

                                        string rawTitle = r["ProjectName"]?.ToString() ?? "";
                                        string prjCode = $"PRJ-{model.SourceID:D4}";
                                        string title = rawTitle;
                                        if (!string.IsNullOrWhiteSpace(prjCode) && !rawTitle.EndsWith($"({prjCode})"))
                                        {
                                            title = $"{rawTitle} ({prjCode})".Trim();
                                        }
                                        if (title.Length > 500) title = title.Substring(0, 500);
                                        int? customerId = r["CustomerID"] != DBNull.Value ? (int?)Convert.ToInt32(r["CustomerID"]) : null;
                                        int? contactId = r["ContactPerson_ID"] != DBNull.Value ? (int?)Convert.ToInt32(r["ContactPerson_ID"]) : null;
                                        decimal expRev = Convert.ToDecimal(r["ExpectedRevenue"]);
                                        string desc = CleanHtml(r["Description"]?.ToString());
                                        originCreatedDate = Convert.ToDateTime(r["CreatedDate"]);
                                        originCreatedBy = r["CreatedBy"]?.ToString();
                                        assignedEmployeeId = Convert.ToInt32(r["AssignedUserID"]);
                                        DateTime lastModDate = r["LastModifiedDate"] != DBNull.Value ? Convert.ToDateTime(r["LastModifiedDate"]) : originCreatedDate;
                                        string lastModBy = r["LastModifiedBy"]?.ToString() ?? originCreatedBy;

                                        r.Close();

                                        using (var cmdIns = conn.CreateCommand())
                                        {
                                            cmdIns.Transaction = tran;
                                            cmdIns.CommandText = @"
INSERT INTO RM_DigitalSales (
    Code, Title, BusinessType, StatusID, CustomerID, ContactPerson_ID,
    TotalExpectedRevenue, TotalActualRevenue, ClosingProbability,
    AssignedEmployeeID, Note, IsDeleted, IsKeyProject,
    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy,
    ActionTime, ApplyYear
)
VALUES (
    @Code, @Title, @BusinessType, @StatusID, @CustomerID, @ContactID,
    @ExpectedRev, 0.00, 100.00,
    @AssignedID, @Note, 0, 0,
    @CreatedDate, @CreatedBy, @LastModifiedDate, @LastModifiedBy,
    @ActionTime, @ApplyYear
);
SELECT SCOPE_IDENTITY();";
                                            cmdIns.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = newCode;
                                            cmdIns.Parameters.Add("@Title", SqlDbType.NVarChar, 500).Value = title ?? "";
                                            cmdIns.Parameters.Add("@BusinessType", SqlDbType.TinyInt).Value = finalBusinessType;
                                            cmdIns.Parameters.Add("@StatusID", SqlDbType.Int).Value = finalStatusId;
                                            cmdIns.Parameters.Add("@CustomerID", SqlDbType.Int).Value = (object)customerId ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@ContactID", SqlDbType.Int).Value = (object)contactId ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@ExpectedRev", SqlDbType.Decimal).Value = expRev;
                                            cmdIns.Parameters.Add("@AssignedID", SqlDbType.Int).Value = assignedEmployeeId > 0 ? (object)assignedEmployeeId : DBNull.Value;
                                            cmdIns.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = (object)desc ?? DBNull.Value;
                                            cmdIns.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = originCreatedDate;
                                            cmdIns.Parameters.Add("@CreatedBy", SqlDbType.VarChar, 150).Value = originCreatedBy;
                                            cmdIns.Parameters.Add("@LastModifiedDate", SqlDbType.DateTime).Value = lastModDate;
                                            cmdIns.Parameters.Add("@LastModifiedBy", SqlDbType.VarChar, 150).Value = lastModBy;
                                            cmdIns.Parameters.Add("@ActionTime", SqlDbType.DateTime).Value = originCreatedDate;
                                            cmdIns.Parameters.Add("@ApplyYear", SqlDbType.Int).Value = originCreatedDate.Year;

                                            newSalesId = Convert.ToInt32(cmdIns.ExecuteScalar());
                                        }
                                    }
                                }

                                result.StepLogs.Add($"[2/7] Đã khởi tạo hồ sơ RM_DigitalSales ID = {newSalesId}, Loại = {finalBusinessType}, Trạng thái = {finalStatus.StatusName}");

                                // Members with combined roles using STUFF FOR XML PATH
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
INSERT INTO RM_DigitalSalesMember (DigitalSalesID, UserID, RoleTitle, IsAM, Note, IsActive, CreatedDate, CreatedBy)
SELECT 
    @NewSalesID,
    pm.Employee_ID,
    LEFT(ISNULL(STUFF((
        SELECT DISTINCT ', ' + r2.RoleName
        FROM RM_ProjectMember pm2
        INNER JOIN RM_ProductProject pp2 ON pm2.ProductProjectID = pp2.ProductProjectID
        INNER JOIN RM_Roles r2 ON (';' + ISNULL(pm2.RoleID, '') + ';' LIKE '%;' + CAST(r2.RoleID AS VARCHAR(10)) + ';%')
        WHERE pp2.ProjectID = @SourceID 
          AND pm2.Employee_ID = pm.Employee_ID
          AND pm2.IsDeleted = 0
          AND r2.IsDeleted = 0
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), N'Thành viên'), 150),
    MAX(CASE WHEN ';' + ISNULL(pm.RoleID, '') + ';' LIKE '%;5;%' THEN 1 ELSE 0 END),
    MAX(CASE WHEN ';' + ISNULL(pm.RoleID, '') + ';' LIKE '%;5;%' THEN N'AM phụ trách' ELSE N'Chuyển từ Dự án ' + CAST(@SourceID AS VARCHAR(10)) END),
    1,
    MIN(pm.CreatedDate),
    MIN(pm.CreatedBy)
FROM RM_ProjectMember pm
INNER JOIN RM_ProductProject pp ON pm.ProductProjectID = pp.ProductProjectID
WHERE pp.ProjectID = @SourceID AND pm.IsDeleted = 0
GROUP BY pm.Employee_ID";
                                    cmd.Parameters.Add("@NewSalesID", SqlDbType.Int).Value = newSalesId;
                                    cmd.Parameters.Add("@SourceID", SqlDbType.Int).Value = model.SourceID;
                                    int memRows = cmd.ExecuteNonQuery();
                                    result.MigratedMembers = memRows;
                                    result.StepLogs.Add($"[3/7] Đã ánh xạ {memRows} thành viên dự án và chuẩn hóa vai trò từ RM_Roles");
                                }

                                // Products
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
INSERT INTO RM_DigitalSalesProduct (
    DigitalSalesID, ProductServiceID, ExpectedRevenue, ActualRevenue,
    PackageName, Quantity, IsDeleted, CreatedDate, CreatedBy
)
SELECT 
    @NewSalesID,
    ISNULL(pp.ProductServiceID, 0),
    ISNULL(pp.ExpectedRevenue, 0), 0.00,
    ISNULL(ps.NameProduct, N'Sản phẩm dự án'), 1, 0,
    ISNULL(pp.CreatedDate, GETDATE()),
    ISNULL(pp.CreatedBy, @Username)
FROM RM_ProductProject pp
LEFT JOIN RM_ProductService ps ON pp.ProductServiceID = ps.ProductServiceID
WHERE pp.ProjectID = @SourceID AND pp.IsDeleted = 0";
                                    cmd.Parameters.Add("@NewSalesID", SqlDbType.Int).Value = newSalesId;
                                    cmd.Parameters.Add("@SourceID", SqlDbType.Int).Value = model.SourceID;
                                    cmd.Parameters.Add("@Username", SqlDbType.VarChar, 150).Value = username;
                                    int pRows = cmd.ExecuteNonQuery();
                                    result.StepLogs.Add($"[4/7] Đã ánh xạ {pRows} sản phẩm thuộc dự án sang RM_DigitalSalesProduct");
                                }
                            }

                            // 5. Timeline milestones from resolved checklist statuses
                            for (int i = 0; i < resolvedStatuses.Count; i++)
                            {
                                var currentS = resolvedStatuses[i];
                                int? fromStatusId = i > 0 ? (int?)resolvedStatuses[i - 1].StatusID : null;
                                byte fromBType = i > 0 ? resolvedStatuses[i - 1].BusinessType : currentS.BusinessType;
                                byte toBType = currentS.BusinessType;

                                DateTime milestoneDate = originCreatedDate.AddDays(i * 3); // Giãn cách ngày theo tiến trình
                                if (i == resolvedStatuses.Count - 1 && resolvedStatuses.Count > 1)
                                    milestoneDate = DateTime.Now;

                                string milestoneNote = i == 0
                                    ? (model.SourceType == 1 ? "Khởi tạo Cơ hội kinh doanh" : "Khởi tạo hồ sơ ban đầu")
                                    : $"Chuyển trạng thái sang {currentS.StatusName}";

                                using (var cmdT = conn.CreateCommand())
                                {
                                    cmdT.Transaction = tran;
                                    cmdT.CommandText = @"
INSERT INTO RM_DigitalSalesTimeline (DigitalSalesID, FromStatusID, ToStatusID, FromBusinessType, ToBusinessType, ActionDate, ActionBy, Note)
VALUES (@SalesID, @FromStatusID, @ToStatusID, @FromBType, @ToBType, @ActionDate, @ActionBy, @Note)";
                                    cmdT.Parameters.Add("@SalesID", SqlDbType.Int).Value = newSalesId;
                                    cmdT.Parameters.Add("@FromStatusID", SqlDbType.Int).Value = (object)fromStatusId ?? DBNull.Value;
                                    cmdT.Parameters.Add("@ToStatusID", SqlDbType.Int).Value = currentS.StatusID;
                                    cmdT.Parameters.Add("@FromBType", SqlDbType.TinyInt).Value = fromBType;
                                    cmdT.Parameters.Add("@ToBType", SqlDbType.TinyInt).Value = toBType;
                                    cmdT.Parameters.Add("@ActionDate", SqlDbType.DateTime).Value = milestoneDate;
                                    cmdT.Parameters.Add("@ActionBy", SqlDbType.VarChar, 150).Value = originCreatedBy;
                                    cmdT.Parameters.Add("@Note", SqlDbType.NVarChar, 500).Value = milestoneNote;
                                    cmdT.ExecuteNonQuery();
                                }
                            }
                            result.StepLogs.Add($"[5/7] Đã tạo {resolvedStatuses.Count} mốc thời gian chuyển trạng thái trong RM_DigitalSalesTimeline");

                            // 6. Tracking processes (First process per status, no progress tasks)
                            foreach (var s in resolvedStatuses)
                            {
                                int processId = 0;
                                using (var cmdProc = conn.CreateCommand())
                                {
                                    cmdProc.Transaction = tran;
                                    cmdProc.CommandText = "SELECT TOP 1 ProcessID FROM RM_DigitalSalesProcess WHERE StatusID = @StatusID AND IsDeleted = 0 ORDER BY SortOrder ASC";
                                    cmdProc.Parameters.Add("@StatusID", SqlDbType.Int).Value = s.StatusID;
                                    var pObj = cmdProc.ExecuteScalar();
                                    if (pObj != null && pObj != DBNull.Value)
                                    {
                                        processId = Convert.ToInt32(pObj);
                                    }
                                }

                                if (processId > 0)
                                {
                                    bool isCurrentStatus = (s.StatusID == finalStatusId);
                                    byte trackingStatus = isCurrentStatus ? (byte)1 : (byte)2; // 1: Đang thực hiện, 2: Hoàn thành

                                    using (var cmdTrk = conn.CreateCommand())
                                    {
                                        cmdTrk.Transaction = tran;
                                        cmdTrk.CommandText = @"
INSERT INTO RM_DigitalSalesTracking (DigitalSalesID, ProcessID, ProgressID, TaskName, Status, StartDate, CompletedDate, CreatedDate, CreatedBy)
VALUES (@SalesID, @ProcessID, NULL, NULL, @Status, @StartDate, @CompletedDate, @CreatedDate, @CreatedBy)";
                                        cmdTrk.Parameters.Add("@SalesID", SqlDbType.Int).Value = newSalesId;
                                        cmdTrk.Parameters.Add("@ProcessID", SqlDbType.Int).Value = processId;
                                        cmdTrk.Parameters.Add("@Status", SqlDbType.TinyInt).Value = trackingStatus;
                                        cmdTrk.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = originCreatedDate;
                                        cmdTrk.Parameters.Add("@CompletedDate", SqlDbType.DateTime).Value = isCurrentStatus ? (object)DBNull.Value : DateTime.Now;
                                        cmdTrk.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = originCreatedDate;
                                        cmdTrk.Parameters.Add("@CreatedBy", SqlDbType.VarChar, 150).Value = originCreatedBy;
                                        cmdTrk.ExecuteNonQuery();
                                    }
                                }
                            }
                            result.StepLogs.Add($"[6/7] Đã khởi tạo quy trình đầu tiên cho {resolvedStatuses.Count} trạng thái (không thêm tiến trình con)");

                            // 7. Migrate Activities & Files
                            int migratedActs = 0;
                            int migratedFiles = 0;

                            if (model.SourceType == 1) // Cơ hội
                            {
                                var dtEx = new DataTable();
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
SELECT eh.ExchangeHistoryID, eh.ExchangeDate, eh.CreatedBy, eh.ExchangeContent,
       ISNULL(u.FullName, eh.CreatedBy) AS FullName, ef.FilePath
FROM RM_ExchangeHistory eh
LEFT JOIN Sys_Users u ON eh.CreatedBy = u.UserName
LEFT JOIN RM_ExchangeHistoryFilePath ef ON eh.ExchangeHistoryID = ef.ExchangeHistoryID AND ef.IsDeleted = 0
WHERE eh.BusinessOpportunityID = @ID
ORDER BY eh.ExchangeDate ASC, eh.ExchangeHistoryID ASC";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        dtEx.Load(r);
                                    }
                                }

                                foreach (DataRow row in dtEx.Rows)
                                {
                                    DateTime exDate = (DateTime)row["ExchangeDate"];
                                    string exBy = row["CreatedBy"].ToString();
                                    string exByName = row["FullName"].ToString();
                                    string rawHtml = row["ExchangeContent"]?.ToString() ?? "";
                                    string rawFile = row["FilePath"] != DBNull.Value ? row["FilePath"].ToString() : "";

                                    string clean = CleanHtml(rawHtml);
                                    string dateStr = exDate.ToString("dd/MM/yyyy");
                                    string cardHtml = $@"<div class=""ds-migrated-exchange""><div class=""d-flex align-items-center mb-2""><span class=""badge bgc-purple-l4 text-purple-d2 border-1 brc-purple-m3 px-2 py-05 radius-1 font-600""><i class=""fa fa-comments mr-1""></i> Trao đổi Cơ hội</span><span class=""font-weight-bold text-secondary-d2 ml-2 text-90"">Lịch sử trao đổi ngày {dateStr}</span></div><div class=""ds-migrated-content"">{clean}</div></div>";

                                    string attachJson = null;
                                    if (!string.IsNullOrWhiteSpace(rawFile) && model.MoveAttachments)
                                    {
                                        string newRelPath = MoveFileSafely(rawFile);
                                        if (!string.IsNullOrEmpty(newRelPath))
                                        {
                                            string fileName = Path.GetFileName(newRelPath);
                                            string ext = Path.GetExtension(newRelPath).ToLowerInvariant();
                                            bool isImg = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(ext);
                                            string boolStr = isImg ? "true" : "false";
                                            attachJson = "[{\"FileName\":\"" + fileName + "\",\"FilePath\":\"" + newRelPath + "\",\"Extension\":\"" + ext + "\",\"IsImage\":" + boolStr + "}]";
                                            migratedFiles++;
                                        }
                                    }

                                    using (var cmdIns = conn.CreateCommand())
                                    {
                                        cmdIns.Transaction = tran;
                                        cmdIns.CommandText = @"
INSERT INTO RM_DigitalSalesActivity (DigitalSalesID, ActivityType, Content, Attachments, ActionDate, ActionBy, ActionByName, IsDeleted)
VALUES (@SalesID, 1, @Content, @Attachments, @ActionDate, @ActionBy, @ActionByName, 0)";
                                        cmdIns.Parameters.Add("@SalesID", SqlDbType.Int).Value = newSalesId;
                                        cmdIns.Parameters.Add("@Content", SqlDbType.NVarChar, -1).Value = cardHtml;
                                        cmdIns.Parameters.Add("@Attachments", SqlDbType.NVarChar, -1).Value = (object)attachJson ?? DBNull.Value;
                                        cmdIns.Parameters.Add("@ActionDate", SqlDbType.DateTime).Value = exDate;
                                        cmdIns.Parameters.Add("@ActionBy", SqlDbType.VarChar, 150).Value = exBy;
                                        cmdIns.Parameters.Add("@ActionByName", SqlDbType.NVarChar, 250).Value = exByName;
                                        cmdIns.ExecuteNonQuery();
                                        migratedActs++;
                                    }
                                }
                            }
                            else // Dự án (RM_Project)
                            {
                                // 1. Task initialization
                                var dtTasks = new DataTable();
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
SELECT tm.TaskManagementID, tm.TaskName, tm.Description, tm.CreatedDate, tm.CreatedBy,
       ISNULL(u.FullName, tm.CreatedBy) AS FullName,
       uAssignee.FullName AS FirstAssigneeName,
       p.PriorityName,
       stuff((
           SELECT '|' + convert(nvarchar(500), f.FilePath)
           FROM RM_LogTaskFilePath f
           WHERE f.TaskManagementID = tm.TaskManagementID 
             AND (f.CommentID IS NULL OR f.CommentID = 0) 
             AND (f.IsDeleted = 0 OR f.IsDeleted IS NULL)
           FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)')
       , 1, 1, '') AS FilePaths
FROM RM_TaskManagement tm
LEFT JOIN (
    SELECT TaskManagementID, Employee_ID,
           ROW_NUMBER() OVER(PARTITION BY TaskManagementID ORDER BY TaskAssigneeID ASC) AS rn
    FROM RM_TaskAssignee
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL) AND Employee_ID > 0
) ta_first ON tm.TaskManagementID = ta_first.TaskManagementID AND ta_first.rn = 1
LEFT JOIN Sys_Users uAssignee ON ta_first.Employee_ID = uAssignee.UserId
LEFT JOIN RM_Priority p ON tm.PriorityID = p.PriorityID
LEFT JOIN Sys_Users u ON (tm.CreatedBy = u.UserName OR tm.CreatedBy = CAST(u.UserId AS NVARCHAR(50)))
WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
  AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)
ORDER BY tm.CreatedDate ASC, tm.TaskManagementID ASC";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        dtTasks.Load(r);
                                    }
                                }

                                foreach (DataRow row in dtTasks.Rows)
                                {
                                    int taskId = Convert.ToInt32(row["TaskManagementID"]);
                                    string tName = row["TaskName"]?.ToString() ?? "";
                                    string tDesc = CleanHtml(row["Description"]?.ToString());
                                    DateTime cDate = (DateTime)row["CreatedDate"];
                                    string cBy = row["CreatedBy"].ToString();
                                    string cByName = row["FullName"].ToString();
                                    string firstAssignee = row["FirstAssigneeName"] != DBNull.Value ? row["FirstAssigneeName"].ToString() : "";
                                    string priority = row["PriorityName"] != DBNull.Value ? row["PriorityName"].ToString() : "";
                                    string rawFiles = row["FilePaths"] != DBNull.Value ? row["FilePaths"].ToString() : "";

                                    string headerInfo = $"#{taskId} - {tName}";
                                    if (!string.IsNullOrWhiteSpace(firstAssignee)) headerInfo += $" | {firstAssignee}";
                                    if (!string.IsNullOrWhiteSpace(priority)) headerInfo += $" | {priority}";

                                    string cardHtml = $@"<div class=""ds-migrated-task""><div class=""d-flex align-items-center mb-2""><span class=""badge bgc-blue-l4 text-blue-d2 border-1 brc-blue-m3 px-2 py-05 radius-1 font-600""><i class=""fa fa-tasks mr-1""></i> Công việc Dự án</span><span class=""font-weight-bold text-primary-d1 ml-2 text-90"">{headerInfo}</span></div>{(string.IsNullOrWhiteSpace(tDesc) ? "" : $@"<div class=""ds-migrated-content"">{tDesc}</div>")}<div class=""ds-migrated-meta""><i class=""far fa-clock mr-1""></i> Khởi tạo: {cDate:dd/MM/yyyy HH:mm} | Người tạo: {cByName}</div></div>";

                                    string attachJson = null;
                                    if (!string.IsNullOrWhiteSpace(rawFiles) && model.MoveAttachments)
                                    {
                                        var fileList = new List<string>();
                                        foreach (var singleFile in rawFiles.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                                        {
                                            string newRelPath = MoveFileSafely(singleFile);
                                            if (!string.IsNullOrEmpty(newRelPath))
                                            {
                                                string fileName = Path.GetFileName(newRelPath);
                                                string ext = Path.GetExtension(newRelPath).ToLowerInvariant();
                                                bool isImg = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(ext);
                                                fileList.Add($"{{\"FileName\":\"{fileName}\",\"FilePath\":\"{newRelPath}\",\"Extension\":\"{ext}\",\"IsImage\":{(isImg ? "true" : "false")}}}");
                                                migratedFiles++;
                                            }
                                        }
                                        if (fileList.Count > 0)
                                        {
                                            attachJson = $"[{string.Join(",", fileList)}]";
                                        }
                                    }

                                    using (var cmdIns = conn.CreateCommand())
                                    {
                                        cmdIns.Transaction = tran;
                                        cmdIns.CommandText = @"
INSERT INTO RM_DigitalSalesActivity (DigitalSalesID, ActivityType, Content, Attachments, ActionDate, ActionBy, ActionByName, IsDeleted)
VALUES (@SalesID, 1, @Content, @Attachments, @ActionDate, @ActionBy, @ActionByName, 0)";
                                        cmdIns.Parameters.Add("@SalesID", SqlDbType.Int).Value = newSalesId;
                                        cmdIns.Parameters.Add("@Content", SqlDbType.NVarChar, -1).Value = cardHtml;
                                        cmdIns.Parameters.Add("@Attachments", SqlDbType.NVarChar, -1).Value = (object)attachJson ?? DBNull.Value;
                                        cmdIns.Parameters.Add("@ActionDate", SqlDbType.DateTime).Value = cDate;
                                        cmdIns.Parameters.Add("@ActionBy", SqlDbType.VarChar, 150).Value = cBy;
                                        cmdIns.Parameters.Add("@ActionByName", SqlDbType.NVarChar, 250).Value = cByName;
                                        cmdIns.ExecuteNonQuery();
                                        migratedActs++;
                                    }
                                }

                                // 2. Comments inside tasks
                                var dtComments = new DataTable();
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    cmd.CommandText = @"
SELECT c.CommentID, c.TaskManagementID, tm.TaskName, c.Content AS CommentText, c.CreatedDate, c.CreatedBy,
       ISNULL(u.FullName, c.CreatedBy) AS FullName,
       uAssignee.FullName AS FirstAssigneeName,
       p.PriorityName,
       stuff((
           SELECT '|' + convert(nvarchar(500), f.FilePath)
           FROM RM_LogTaskFilePath f
           WHERE f.CommentID = c.CommentID 
             AND (f.IsDeleted = 0 OR f.IsDeleted IS NULL)
           FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)')
       , 1, 1, '') AS FilePaths
FROM RM_Comment c
INNER JOIN RM_TaskManagement tm ON c.TaskManagementID = tm.TaskManagementID
LEFT JOIN (
    SELECT TaskManagementID, Employee_ID,
           ROW_NUMBER() OVER(PARTITION BY TaskManagementID ORDER BY TaskAssigneeID ASC) AS rn
    FROM RM_TaskAssignee
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL) AND Employee_ID > 0
) ta_first ON tm.TaskManagementID = ta_first.TaskManagementID AND ta_first.rn = 1
LEFT JOIN Sys_Users uAssignee ON ta_first.Employee_ID = uAssignee.UserId
LEFT JOIN RM_Priority p ON tm.PriorityID = p.PriorityID
LEFT JOIN Sys_Users u ON (c.Employee_ID > 0 AND c.Employee_ID = u.UserId) OR (c.CreatedBy = u.UserName)
WHERE (tm.ProjectID = @ID OR tm.ProductProjectID IN (SELECT pp.ProductProjectID FROM RM_ProductProject pp WHERE pp.ProjectID = @ID))
  AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL)
  AND (tm.IsDeleted = 0 OR tm.IsDeleted IS NULL)
ORDER BY c.CreatedDate ASC, c.CommentID ASC";
                                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = model.SourceID;
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        dtComments.Load(r);
                                    }
                                }

                                foreach (DataRow row in dtComments.Rows)
                                {
                                    int taskId = Convert.ToInt32(row["TaskManagementID"]);
                                    string tName = row["TaskName"]?.ToString() ?? "";
                                    string cText = CleanHtml(row["CommentText"]?.ToString());
                                    DateTime cDate = (DateTime)row["CreatedDate"];
                                    string cBy = row["CreatedBy"].ToString();
                                    string cByName = row["FullName"].ToString();
                                    string firstAssignee = row["FirstAssigneeName"] != DBNull.Value ? row["FirstAssigneeName"].ToString() : "";
                                    string priority = row["PriorityName"] != DBNull.Value ? row["PriorityName"].ToString() : "";
                                    string rawFiles = row["FilePaths"] != DBNull.Value ? row["FilePaths"].ToString() : "";

                                    string headerInfo = $"#{taskId} - {tName}";
                                    if (!string.IsNullOrWhiteSpace(firstAssignee)) headerInfo += $" | {firstAssignee}";
                                    if (!string.IsNullOrWhiteSpace(priority)) headerInfo += $" | {priority}";

                                    string cardHtml = $@"<div class=""ds-migrated-task-comment""><div class=""d-flex align-items-center mb-2 flex-wrap"" style=""gap: 6px;""><span class=""badge bgc-blue-l4 text-blue-d2 border-1 brc-blue-m3 px-2 py-05 radius-1 font-600""><i class=""fa fa-tasks mr-1""></i> Công việc Dự án</span><span class=""font-weight-bold text-primary-d1 text-90"">{headerInfo}</span><span class=""badge bgc-grey-l3 text-secondary-d2 font-normal text-75 radius-1""><i class=""far fa-comment-dots mr-1""></i> Bình luận</span></div><div class=""ds-migrated-content"">{cText}</div></div>";

                                    string attachJson = null;
                                    if (!string.IsNullOrWhiteSpace(rawFiles) && model.MoveAttachments)
                                    {
                                        var fileList = new List<string>();
                                        foreach (var singleFile in rawFiles.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                                        {
                                            string newRelPath = MoveFileSafely(singleFile);
                                            if (!string.IsNullOrEmpty(newRelPath))
                                            {
                                                string fileName = Path.GetFileName(newRelPath);
                                                string ext = Path.GetExtension(newRelPath).ToLowerInvariant();
                                                bool isImg = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(ext);
                                                fileList.Add($"{{\"FileName\":\"{fileName}\",\"FilePath\":\"{newRelPath}\",\"Extension\":\"{ext}\",\"IsImage\":{(isImg ? "true" : "false")}}}");
                                                migratedFiles++;
                                            }
                                        }
                                        if (fileList.Count > 0)
                                        {
                                            attachJson = $"[{string.Join(",", fileList)}]";
                                        }
                                    }

                                    using (var cmdIns = conn.CreateCommand())
                                    {
                                        cmdIns.Transaction = tran;
                                        cmdIns.CommandText = @"
INSERT INTO RM_DigitalSalesActivity (DigitalSalesID, ActivityType, Content, Attachments, ActionDate, ActionBy, ActionByName, IsDeleted)
VALUES (@SalesID, 1, @Content, @Attachments, @ActionDate, @ActionBy, @ActionByName, 0)";
                                        cmdIns.Parameters.Add("@SalesID", SqlDbType.Int).Value = newSalesId;
                                        cmdIns.Parameters.Add("@Content", SqlDbType.NVarChar, -1).Value = cardHtml;
                                        cmdIns.Parameters.Add("@Attachments", SqlDbType.NVarChar, -1).Value = (object)attachJson ?? DBNull.Value;
                                        cmdIns.Parameters.Add("@ActionDate", SqlDbType.DateTime).Value = cDate;
                                        cmdIns.Parameters.Add("@ActionBy", SqlDbType.VarChar, 150).Value = cBy;
                                        cmdIns.Parameters.Add("@ActionByName", SqlDbType.NVarChar, 250).Value = cByName;
                                        cmdIns.ExecuteNonQuery();
                                        migratedActs++;
                                    }
                                }
                            }

                            result.StepLogs.Add($"[7/7] Đã chuyển đổi {migratedActs} hoạt động/trao đổi và di chuyển an toàn {migratedFiles} tệp đính kèm");

                            tran.Commit();

                            // Invalidate in-memory cache if needed
                            try { System.Web.HttpContext.Current?.Cache?.Remove("RM_DigitalSales_GetList_Default"); } catch { }

                            result.Success = true;
                            result.NewDigitalSalesID = newSalesId;
                            result.NewCode = newCode;
                            result.DetailUrl = $"/Cate/DigitalSales/Detail/{newSalesId}";
                            result.Message = $"Chuyển đổi thành công! Đã tạo hồ sơ SPDV Số mới: {newCode} (ID: {newSalesId}).";
                        }
                        catch (Exception exInner)
                        {
                            tran.Rollback();
                            throw exInner;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex);
                result.Success = false;
                result.Message = "Lỗi trong quá trình chuyển đổi dữ liệu: " + ex.Message;
            }

            return result;
        }

        #endregion

        #region Recent Migrations

        public List<RecentMigrationItemModel> GetRecentMigrations(int top = 10)
        {
            var list = new List<RecentMigrationItemModel>();
            var connStr = GetConnectionString();
            if (string.IsNullOrEmpty(connStr)) return list;

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT TOP (@Top) 
    ds.DigitalSalesID, ds.Code, ds.Title, ds.BusinessType, ds.StatusID,
    s.StatusName, ds.TotalExpectedRevenue, u.FullName AS AMName,
    ds.CreatedDate, ds.CreatedBy, ds.CustomerID, c.CustomerName
FROM RM_DigitalSales ds
LEFT JOIN RM_DigitalSalesStatus s ON ds.StatusID = s.StatusID
LEFT JOIN Sys_Users u ON ds.AssignedEmployeeID = u.UserID
LEFT JOIN RM_Customer c ON ds.CustomerID = c.CustomerID
ORDER BY ds.DigitalSalesID DESC";
                        cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top > 0 ? top : 10;

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new RecentMigrationItemModel
                                {
                                    DigitalSalesID = Convert.ToInt32(r["DigitalSalesID"]),
                                    Code = r["Code"]?.ToString(),
                                    Title = r["Title"]?.ToString(),
                                    CustomerName = r["CustomerName"]?.ToString() ?? "",
                                    SourceType = Convert.ToInt32(r["BusinessType"]),
                                    SourceId = Convert.ToInt32(r["DigitalSalesID"]),
                                    BusinessType = Convert.ToByte(r["BusinessType"]),
                                    BusinessTypeName = Convert.ToByte(r["BusinessType"]) == 1 ? "Cơ hội KD DVS" : "Dự án KD DVS",
                                    StatusID = Convert.ToInt32(r["StatusID"]),
                                    StatusName = r["StatusName"]?.ToString(),
                                    TotalExpectedRevenue = r["TotalExpectedRevenue"] != DBNull.Value ? Convert.ToDecimal(r["TotalExpectedRevenue"]) : 0,
                                    AMName = r["AMName"]?.ToString() ?? r["CreatedBy"]?.ToString(),
                                    CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex);
            }

            return list;
        }

        #endregion

        #region Helper Methods

        private string CleanHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            string clean = WebUtility.HtmlDecode(WebUtility.HtmlDecode(html));
            clean = Regex.Replace(clean, @"\s*style=""[^""]*""", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\s*class=""[^""]*""", "", RegexOptions.IgnoreCase);
            clean = Regex.Replace(clean, @"\s*lang=""[^""]*""", "", RegexOptions.IgnoreCase);
            return clean.Trim();
        }

        private string MoveFileSafely(string oldRelPath)
        {
            if (string.IsNullOrWhiteSpace(oldRelPath)) return null;

            try
            {
                string rootWeb = HostingEnvironment.MapPath("~") ?? AppDomain.CurrentDomain.BaseDirectory;
                string oldClean = oldRelPath.Replace("/", "\\").TrimStart('\\');

                // Search potential source locations
                string fileName = Path.GetFileName(oldRelPath);
                string yyyyMM = DateTime.Now.ToString("yyyyMM");
                string targetDirRel = $"/Contents/Uploads/DigitalSales/{yyyyMM}";
                string targetDirPhys = Path.Combine(rootWeb, "Contents", "Uploads", "DigitalSales", yyyyMM);

                if (!Directory.Exists(targetDirPhys)) Directory.CreateDirectory(targetDirPhys);

                string targetFilePath = Path.Combine(targetDirPhys, fileName);
                string sourceFilePath = Path.Combine(rootWeb, oldClean);

                if (!File.Exists(sourceFilePath))
                {
                    // Fallback to Source_Prod or d:\SVN\crm\Source_Prod
                    string alt1 = Path.Combine("d:\\SVN\\crm\\Source_Prod", oldClean);
                    if (File.Exists(alt1)) sourceFilePath = alt1;
                }

                if (File.Exists(sourceFilePath))
                {
                    long sLen = new FileInfo(sourceFilePath).Length;

                    // Copy to WebApp target
                    File.Copy(sourceFilePath, targetFilePath, true);

                    // Copy to publish_source as well if available
                    string pubDir = Path.Combine("d:\\SVN\\crm\\publish_source\\Contents\\Uploads\\DigitalSales", yyyyMM);
                    if (Directory.Exists("d:\\SVN\\crm\\publish_source"))
                    {
                        if (!Directory.Exists(pubDir)) Directory.CreateDirectory(pubDir);
                        File.Copy(sourceFilePath, Path.Combine(pubDir, fileName), true);
                    }

                    // Verify bytes
                    if (File.Exists(targetFilePath) && new FileInfo(targetFilePath).Length == sLen && sLen > 0)
                    {
                        try { File.Delete(sourceFilePath); } catch { }
                        return $"{targetDirRel}/{fileName}";
                    }
                }
                else if (File.Exists(targetFilePath))
                {
                    // Already moved
                    return $"{targetDirRel}/{fileName}";
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex);
            }

            return oldRelPath;
        }

        public List<RM_DigitalSalesStatusModel> GetActiveStatuses()
        {
            var list = new List<RM_DigitalSalesStatusModel>();
            var connStr = GetConnectionString();
            if (string.IsNullOrEmpty(connStr)) return list;

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT StatusID, StatusCode, StatusName, BusinessType, SortOrder, IsActive, Description
FROM RM_DigitalSalesStatus
WHERE IsActive = 1
ORDER BY SortOrder ASC";
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new RM_DigitalSalesStatusModel
                                {
                                    StatusID = Convert.ToInt32(r["StatusID"]),
                                    StatusCode = r["StatusCode"]?.ToString(),
                                    StatusName = r["StatusName"]?.ToString(),
                                    BusinessType = r["BusinessType"] != DBNull.Value ? Convert.ToByte(r["BusinessType"]) : (byte)1,
                                    SortOrder = r["SortOrder"] != DBNull.Value ? Convert.ToInt32(r["SortOrder"]) : 0,
                                    IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                                    Description = r["Description"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex);
            }

            return list;
        }

        private void SafeLogError(Exception ex)
        {
            try
            {
                if (System.Web.Hosting.HostingEnvironment.IsHosted)
                {
                    AppProcessor.Logger.Error(ex);
                }
            }
            catch { }
        }

        #endregion
    }
}
