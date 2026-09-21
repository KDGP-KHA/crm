using Core.Cate.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_DigitalSalesBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";

        private readonly string _spGetList = "RM_DigitalSales_GetList";
        private readonly string _spGetByID = "RM_DigitalSales_GetByID";
        private readonly string _spSave = "RM_DigitalSales_Save";
        private readonly string _spProductGetBySalesID = "RM_DigitalSalesProduct_GetBySalesID";
        private readonly string _spProductSave = "RM_DigitalSalesProduct_Save";
        private readonly string _spProductDelete = "RM_DigitalSalesProduct_Delete";
        private readonly string _spProductDetailSave = "RM_DigitalSalesProduct_SaveDetail";
        private readonly string _spProductCostGetByProductID = "RM_DigitalSalesProductCost_GetByProductID";
        private readonly string _spProductRevenueGetByProductID = "RM_DigitalSalesProductRevenue_GetByProductID";
        private readonly string _spProductMemberGetByProductID = "RM_DigitalSalesProductMember_GetByProductID";
        private readonly string _spProductContractGetByProductID = "RM_DigitalSalesProductContract_GetByProductID";
        private readonly string _spProductContractLink = "RM_DigitalSalesProductContract_Link";
        private readonly string _spChangeStatus = "RM_DigitalSales_ChangeStatus";
        private readonly string _spTrackingGetBySalesID = "RM_DigitalSalesTracking_GetBySalesID";
        private readonly string _spTrackingSave = "RM_DigitalSalesTracking_Save";
        private readonly string _spTrackingDelete = "RM_DigitalSalesTracking_Delete";
        private readonly string _spTrackingUpdateStatus = "RM_DigitalSalesTracking_UpdateStatus";
        private readonly string _spMemberGetBySalesID = "RM_DigitalSalesMember_GetBySalesID";
        private readonly string _spMemberSave = "RM_DigitalSalesMember_Save";
        private readonly string _spMemberDelete = "RM_DigitalSalesMember_Delete";
        private readonly string _spGetTimeline = "RM_DigitalSales_GetTimeline";
        private readonly string _spDelete = "RM_DigitalSales_Delete";
        private readonly string _spStatusGetAll = "RM_DigitalSalesStatus_GetAll";
        private readonly string _spToggleKeyProject = "RM_DigitalSales_ToggleKeyProject";
        private readonly string _spToggleFollow = "RM_DigitalSales_ToggleFollow";
        private readonly string _spActivityAdd = "RM_DigitalSalesActivity_Add";
        private readonly string _spActivityGetList = "RM_DigitalSalesActivity_GetList";
        private readonly string _spActivityDelete = "RM_DigitalSalesActivity_Delete";
        private readonly string _spActivityUpdateLatestStatusChange = "RM_DigitalSalesActivity_UpdateLatestStatusChangeAttachments";
        private readonly string _spGetDepartmentByUserID = "RM_DigitalSales_GetDepartmentByUserID";
        private readonly string _spGetAccessibleEmployees = "RM_DigitalSales_GetAccessibleEmployees";
        private readonly string _spGetDashboardStatusStats = "RM_DigitalSales_GetDashboardStatusStats";
        private readonly string _spGetStaleActionTime = "RM_DigitalSales_GetStaleActionTime";

        public DigitalSalesDashboardOverviewModel GetDashboardStatusStats(int applyYear)
        {
            return GetDashboardStatusStats(applyYear, null, null);
        }

        public DigitalSalesDashboardOverviewModel GetDashboardStatusStats(int applyYear, string userName)
        {
            return GetDashboardStatusStats(applyYear, userName, null);
        }

        public DigitalSalesDashboardOverviewModel GetDashboardStatusStats(int applyYear, string userName, string keyword)
        {
            if (applyYear <= 0) applyYear = DateTime.Now.Year;

            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesDashboardStatusModel>(
                _spGetDashboardStatusStats,
                DATA_PROVIDER_NAME,
                applyYear,
                userName
            ) ?? new List<RM_DigitalSalesDashboardStatusModel>();

            int totalKey = 0;
            var keyProjects = LoadList(out totalKey, new RM_DigitalSalesSearchModel
            {
                ApplyYear = applyYear,
                Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
                IsKeyProject = true,
                UserName = !string.IsNullOrWhiteSpace(userName) ? userName : null,
                PageNumber = 1,
                PageSize = 50
            }) ?? new List<RM_DigitalSalesModel>();

            int totalFollowed = 0;
            var followedOpportunities = LoadList(out totalFollowed, new RM_DigitalSalesSearchModel
            {
                ApplyYear = applyYear,
                Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
                IsFollowed = true,
                UserName = !string.IsNullOrWhiteSpace(userName) ? userName : null,
                PageNumber = 1,
                PageSize = 50
            }) ?? new List<RM_DigitalSalesModel>();

            var staleSales = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesModel>(
                _spGetStaleActionTime,
                DATA_PROVIDER_NAME,
                applyYear,
                72,
                0,
                userName
            ) ?? new List<RM_DigitalSalesModel>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                staleSales = staleSales.Where(x =>
                    (x.Title != null && x.Title.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.CustomerName != null && x.CustomerName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Code != null && x.Code.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }

            return new DigitalSalesDashboardOverviewModel
            {
                ApplyYear = applyYear,
                Keyword = keyword,
                StatusList = list,
                TotalCountAll = list.Sum(x => x.TotalCount),
                TotalExpectedRevenueAll = list.Sum(x => x.TotalExpectedRevenue),
                TotalActualRevenueAll = list.Sum(x => x.TotalActualRevenue),
                KeyProjects = keyProjects,
                FollowedOpportunities = followedOpportunities,
                StaleActionTimeSales = staleSales
            };
        }

        public List<RM_DigitalSalesModel> LoadList(out int total, RM_DigitalSalesSearchModel model)
        {
            total = 0;
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(model.FromDate))
            {
                if (DateTime.TryParseExact(model.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fd))
                    fromDate = fd;
            }

            if (!string.IsNullOrWhiteSpace(model.ToDate))
            {
                if (DateTime.TryParseExact(model.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime td))
                    toDate = td;
            }

            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesModel>(
                _spGetList,
                DATA_PROVIDER_NAME,
                string.IsNullOrWhiteSpace(model.Keyword) ? null : model.Keyword.Trim(),
                model.BusinessType,
                model.StatusID,
                model.CustomerID,
                model.ProductServiceID,
                model.DepartmentID,
                model.EmployeeID,
                fromDate,
                toDate,
                model.PageNumber <= 0 ? 1 : model.PageNumber,
                model.PageSize <= 0 ? 20 : model.PageSize,
                model.UserName,
                model.IsKeyProject.HasValue && model.IsKeyProject.Value ? 1 : 0,
                model.IsFollowed.HasValue && model.IsFollowed.Value ? 1 : 0,
                string.IsNullOrWhiteSpace(model.StatusIDs) ? null : model.StatusIDs.Trim(),
                model.ApplyYear.GetValueOrDefault(0)
            );

            if (data != null && data.Count > 0)
            {
                total = data.First().TotalCount.GetValueOrDefault(0);
            }

            return data ?? new List<RM_DigitalSalesModel>();
        }

        public RM_DigitalSalesModel GetByID(int id)
        {
            return GetByID(id, null);
        }

        public RM_DigitalSalesModel GetByID(int id, string userName)
        {
            if (id <= 0) return null;
            var model = AppProcessor.ProcedureProvider.ExecuteScalarObject<RM_DigitalSalesModel>(_spGetByID, DATA_PROVIDER_NAME, id, userName);
            if (model != null)
            {
                model.Title = FixVietnameseMojibake(model.Title);
                model.CustomerName = FixVietnameseMojibake(model.CustomerName);
                model.BusinessTypeName = FixVietnameseMojibake(model.BusinessTypeName);
                model.StatusName = FixVietnameseMojibake(model.StatusName);
                model.Note = FixVietnameseMojibake(model.Note);
                try
                {
                    model.Products = GetProductsBySalesID(id);
                    // Giá trị hợp đồng được nhập theo triệu VNĐ; TotalActualRevenue của hồ sơ dùng VNĐ.
                    model.TotalActualRevenue = model.Products.Sum(item => item.ContractRevenueMillion) * 1000000m;
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                    model.Products = new List<RM_DigitalSalesProductModel>();
                }
                try { model.Members = GetMembersBySalesID(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Members = new List<RM_DigitalSalesMemberModel>(); }
                try { model.TrackingTasks = GetTrackingTasks(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.TrackingTasks = new List<RM_DigitalSalesTrackingModel>(); }
                try { model.Timelines = GetTimeline(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Timelines = new List<RM_DigitalSalesTimelineModel>(); }
                try { model.Activities = GetActivitiesBySalesID(id); } catch (Exception ex) { AppProcessor.Logger.Error(ex); model.Activities = new List<RM_DigitalSalesActivityModel>(); }
                try { EnsureCurrentStatusTrackingPlaceholder(model); } catch (Exception ex) { AppProcessor.Logger.Error(ex); }
                try
                {
                    var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["TOC.Conn.Major"]?.ConnectionString;
                    if (!string.IsNullOrEmpty(connStr))
                    {
                        using (var conn = new System.Data.SqlClient.SqlConnection(connStr))
                        using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT ApplyYear FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            conn.Open();
                            var val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                            {
                                model.ApplyYear = Convert.ToInt32(val);
                            }
                        }
                    }
                }
                catch { }

                if (!model.ApplyYear.HasValue || model.ApplyYear.Value <= 0)
                {
                    model.ApplyYear = model.CreatedDate.Year > 1900 ? model.CreatedDate.Year : DateTime.Now.Year;
                }
            }
            return model;
        }

        public bool ToggleKeyProject(int id, bool isKeyProject, string userName)
        {
            if (id <= 0) return false;
            try
            {
                var res = AppProcessor.ProcedureProvider.Execute(
                    _spToggleKeyProject,
                    DATA_PROVIDER_NAME,
                    id,
                    isKeyProject,
                    userName
                );
                if (res.GetValueOrDefault(0) > 0) return true;

                var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                    _spToggleKeyProject,
                    DATA_PROVIDER_NAME,
                    id,
                    isKeyProject,
                    userName
                );
                if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
            }
            catch
            {
                try
                {
                    var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                        _spToggleKeyProject,
                        DATA_PROVIDER_NAME,
                        id,
                        isKeyProject,
                        userName
                    );
                    if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
                }
                catch { }
            }
            return false;
        }

        public bool ToggleFollow(int id, bool isFollowed, string userName)
        {
            if (id <= 0 || string.IsNullOrEmpty(userName)) return false;
            try
            {
                var res = AppProcessor.ProcedureProvider.Execute(
                    _spToggleFollow,
                    DATA_PROVIDER_NAME,
                    id,
                    userName,
                    isFollowed
                );
                if (res.GetValueOrDefault(0) > 0) return true;

                var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                    _spToggleFollow,
                    DATA_PROVIDER_NAME,
                    id,
                    userName,
                    isFollowed
                );
                if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
            }
            catch
            {
                try
                {
                    var scalar = AppProcessor.ProcedureProvider.ExecuteScalar(
                        _spToggleFollow,
                        DATA_PROVIDER_NAME,
                        id,
                        userName,
                        isFollowed
                    );
                    if (scalar != null && Convert.ToInt32(scalar) > 0) return true;
                }
                catch { }
            }
            return false;
        }

        public int Save(RM_DigitalSalesModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spSave,
                DATA_PROVIDER_NAME,
                model.DigitalSalesID,
                model.Code,
                model.Title,
                model.BusinessType,
                model.StatusID,
                model.CustomerID,
                model.ContactPerson_ID,
                model.ClosingProbability,
                model.ExpectedDate,
                model.StartDate,
                model.EndDate,
                model.ContractID,
                model.ContractNo,
                model.ContractValue,
                model.ContractSignDate,
                model.AssignedEmployeeID,
                model.DepartmentID,
                model.Note,
                model.FileAttach,
                username,
                model.ApplyYear.HasValue && model.ApplyYear.Value > 0 ? model.ApplyYear.Value : DateTime.Now.Year
            );

            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesProductModel> GetProductsBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesProductModel>();
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesProductModel>(
                _spProductGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            data = data ?? new List<RM_DigitalSalesProductModel>();
            foreach (var product in data)
            {
                product.ProductServiceName = FixVietnameseMojibake(product.ProductServiceName);
                product.PackageName = FixVietnameseMojibake(product.PackageName);
                product.Note = FixVietnameseMojibake(product.Note);
                try
                {
                    product.Contracts = GetProductContracts(product.SalesProductID);
                    product.ContractCount = product.Contracts.Count;
                    // Contract TotalAmount is already stored in million VND, matching the product summary unit.
                    product.ContractRevenueMillion = product.Contracts.Sum(item => Convert.ToDecimal(item.TotalAmount > 0 ? item.TotalAmount : item.ContractValue));
                    product.ActualRevenue = product.ContractRevenueMillion * 1000000m;
                }
                catch (Exception ex)
                {
                    // A missing or not-yet-registered contract procedure must not hide the product list.
                    AppProcessor.Logger.Error(ex);
                    product.Contracts = new List<RM_ContractsModel>();
                    product.ContractCount = 0;
                    product.ContractRevenueMillion = 0m;
                }
            }
            return data;
        }

        public List<RM_ContractsModel> GetProductContracts(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_ContractsModel>();
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ContractsModel>(
                _spProductContractGetByProductID,
                DATA_PROVIDER_NAME,
                salesProductId
            ) ?? new List<RM_ContractsModel>();
        }

        public int LinkProductContract(int salesProductId, int contractId, string username)
        {
            if (salesProductId <= 0 || contractId <= 0) return 0;
            return AppProcessor.ProcedureProvider.Execute(
                _spProductContractLink,
                DATA_PROVIDER_NAME,
                salesProductId,
                contractId,
                username
            ).GetValueOrDefault(0);
        }

        public int SaveProduct(RM_DigitalSalesProductModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spProductSave,
                DATA_PROVIDER_NAME,
                model.SalesProductID,
                model.DigitalSalesID,
                model.ProductServiceID,
                model.ExpectedRevenue,
                model.ActualRevenue,
                model.PackageName,
                model.Quantity <= 0 ? 1 : model.Quantity,
                model.StartDate,
                model.EndDate,
                model.Note,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesProductCostModel> GetProductCosts(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductCostModel>();
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesProductCostModel>(
                _spProductCostGetByProductID,
                DATA_PROVIDER_NAME,
                salesProductId
            ) ?? new List<RM_DigitalSalesProductCostModel>();
        }

        public List<RM_DigitalSalesProductRevenueModel> GetProductRevenues(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductRevenueModel>();
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesProductRevenueModel>(
                _spProductRevenueGetByProductID,
                DATA_PROVIDER_NAME,
                salesProductId
            ) ?? new List<RM_DigitalSalesProductRevenueModel>();
        }

        public List<RM_DigitalSalesProductMemberModel> GetProductMembers(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductMemberModel>();
            var members = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesProductMemberModel>(
                _spProductMemberGetByProductID,
                DATA_PROVIDER_NAME,
                salesProductId
            ) ?? new List<RM_DigitalSalesProductMemberModel>();

            foreach (var member in members)
            {
                member.RoleIDs = string.IsNullOrWhiteSpace(member.RoleIDsText)
                    ? new int[0]
                    : member.RoleIDsText.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value =>
                        {
                            int roleId;
                            return int.TryParse(value.Trim(), out roleId) ? roleId : 0;
                        })
                        .Where(roleId => roleId > 0)
                        .Distinct()
                        .ToArray();
            }

            return members;
        }

        public int SaveProductDetail(RM_DigitalSalesProductModel model, string username)
        {
            if (model == null) return 0;

            var costs = model.Costs ?? new List<RM_DigitalSalesProductCostModel>();
            var revenues = model.Revenues ?? new List<RM_DigitalSalesProductRevenueModel>();

            model.ActualRevenue = revenues.Sum(item => item.Amount ?? 0m) * 1000000m;

            var costsXml = new XElement("Items",
                costs.Select(item => new XElement("Item",
                    new XAttribute("ID", item.SalesProductCostID),
                    new XAttribute("CostTypeID", item.CostTypeID),
                    new XAttribute("Amount", (item.Amount ?? 0m).ToString(CultureInfo.InvariantCulture)),
                    item.PaymentDate.HasValue ? new XAttribute("PaymentDate", item.PaymentDate.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)) : null,
                    new XElement("Note", item.Note ?? string.Empty))));

            var revenuesXml = new XElement("Items",
                revenues.Select(item => new XElement("Item",
                    new XAttribute("ID", item.SalesProductRevenueID),
                    new XAttribute("Amount", (item.Amount ?? 0m).ToString(CultureInfo.InvariantCulture)),
                    item.ReceivedDate.HasValue ? new XAttribute("ReceivedDate", item.ReceivedDate.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)) : null,
                    item.ReceivedTime.HasValue ? new XAttribute("ReceivedTime", item.ReceivedTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)) : null,
                    new XElement("Note", item.Note ?? string.Empty))));

            var result = AppProcessor.ProcedureProvider.Execute(
                _spProductDetailSave,
                DATA_PROVIDER_NAME,
                model.SalesProductID,
                model.DigitalSalesID,
                model.ProductServiceID,
                model.ExpectedRevenue,
                model.PackageName,
                model.Quantity <= 0 ? 1 : model.Quantity,
                model.StartDate,
                model.EndDate,
                model.Note,
                costsXml.ToString(SaveOptions.DisableFormatting),
                revenuesXml.ToString(SaveOptions.DisableFormatting),
                "<Items />",
                username
            );

            return result.GetValueOrDefault(0);
        }

        public int DeleteProduct(int salesProductId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spProductDelete,
                DATA_PROVIDER_NAME,
                salesProductId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int ChangeStatus(int digitalSalesId, int newStatusId, string note, string attachmentPath, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spChangeStatus,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                newStatusId,
                note,
                attachmentPath,
                username
            );

            return result.GetValueOrDefault(0);
        }

        public void EnsureCurrentStatusTrackingPlaceholder(RM_DigitalSalesModel model)
        {
            if (model == null || model.DigitalSalesID <= 0) return;
            if (model.TrackingTasks == null) model.TrackingTasks = new List<RM_DigitalSalesTrackingModel>();

            var workflowBiz = new RM_DigitalSalesWorkflowBiz();

            // 1. Duyệt toàn bộ các mốc chuyển trạng thái trong lịch sử Timelines
            // Nếu một trạng thái từng được chuyển tới nhưng không có tiến trình nào (0 tiến trình),
            // bắt buộc phải giữ lại placeholder cho mốc TimelineID đó để không bị biến mất khỏi Checklist!
            if (model.Timelines != null && model.Timelines.Count > 0)
            {
                var transitions = model.Timelines
                    .Where(tl => tl.ToStatusID > 0 && (!tl.FromStatusID.HasValue || tl.FromStatusID.Value != tl.ToStatusID))
                    .OrderBy(tl => tl.ActionDate).ThenBy(tl => tl.TimelineID)
                    .ToList();
                var validTimelineIds = new HashSet<int>(transitions.Select(tl => tl.TimelineID));

                // Chuẩn hóa TimelineID cho tất cả các tiến trình hiện tại:
                // Nếu 1 task có TimelineID là null hoặc trỏ vào timeline không phải chuyển trạng thái,
                // thì tự động map lại vào TimelineID chuyển trạng thái hợp lệ gần nhất của Status đó.
                foreach (var t in model.TrackingTasks)
                {
                    if (t.StatusID.HasValue && t.StatusID.Value > 0)
                    {
                        if (!t.TimelineID.HasValue || !validTimelineIds.Contains(t.TimelineID.Value))
                        {
                            var matchingTl = transitions.Where(tl => tl.ToStatusID == t.StatusID.Value).LastOrDefault();
                            if (matchingTl != null)
                            {
                                t.TimelineID = matchingTl.TimelineID;
                            }
                        }
                    }
                }

                foreach (var tl in transitions)
                {
                    bool hasTaskForTimeline = model.TrackingTasks.Any(t => t.TimelineID == tl.TimelineID);
                    if (!hasTaskForTimeline)
                    {
                        int totalProcs = 0;
                        var procs = workflowBiz.GetProcesses(out totalProcs, statusId: tl.ToStatusID);
                        var activeProcs = procs?.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
                        var defaultProc = activeProcs?.FirstOrDefault();

                        var placeholder = new RM_DigitalSalesTrackingModel
                        {
                            TrackingID = 0,
                            DigitalSalesID = model.DigitalSalesID,
                            ParentID = null,
                            ProcessID = defaultProc?.ProcessID ?? 0,
                            ProcessName = defaultProc?.ProcessName ?? "Quy trình thực hiện",
                            StatusID = tl.ToStatusID,
                            SalesStatusName = !string.IsNullOrEmpty(tl.ToStatusName) ? tl.ToStatusName : "Trạng thái",
                            ProcessCountOfStatus = activeProcs?.Count ?? 0,
                            ProgressID = null,
                            TaskName = null,
                            Status = 1,
                            TimelineID = tl.TimelineID
                        };
                        model.TrackingTasks.Add(placeholder);
                    }
                }

                // Xóa placeholder rỗng nếu đã có tiến trình thực tế cùng TimelineID
                var timelineWithRealTasks = new HashSet<int>(model.TrackingTasks.Where(t => !string.IsNullOrWhiteSpace(t.TaskName) && t.TimelineID.HasValue).Select(t => t.TimelineID.Value));
                model.TrackingTasks.RemoveAll(t => t.TrackingID == 0 && string.IsNullOrWhiteSpace(t.TaskName) && t.TimelineID.HasValue && timelineWithRealTasks.Contains(t.TimelineID.Value));
            }

            // 2. Đồng thời kiểm tra trạng thái hiện tại (model.StatusID) nếu chưa có trong TrackingTasks
            if (model.StatusID > 0 && !model.TrackingTasks.Any(t => t.StatusID == model.StatusID))
            {
                int totalProcs = 0;
                var procs = workflowBiz.GetProcesses(out totalProcs, statusId: model.StatusID);
                var activeProcs = procs?.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList();
                var defaultProc = activeProcs?.FirstOrDefault();

                int? latestTimelineId = model.Timelines?
                    .Where(tl => tl.ToStatusID == model.StatusID && (!tl.FromStatusID.HasValue || tl.FromStatusID.Value != tl.ToStatusID))
                    .OrderByDescending(tl => tl.ActionDate).ThenByDescending(tl => tl.TimelineID)
                    .FirstOrDefault()?.TimelineID;

                var placeholder = new RM_DigitalSalesTrackingModel
                {
                    TrackingID = 0,
                    DigitalSalesID = model.DigitalSalesID,
                    ParentID = null,
                    ProcessID = defaultProc?.ProcessID ?? 0,
                    ProcessName = defaultProc?.ProcessName ?? "Quy trình thực hiện",
                    StatusID = model.StatusID,
                    SalesStatusName = !string.IsNullOrEmpty(model.StatusName) ? model.StatusName : "Trạng thái",
                    ProcessCountOfStatus = activeProcs?.Count ?? 0,
                    ProgressID = null,
                    TaskName = null,
                    Status = 1,
                    TimelineID = latestTimelineId
                };
                model.TrackingTasks.Add(placeholder);
            }
        }

        public List<RM_DigitalSalesTrackingModel> GetTrackingTasks(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTrackingModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesTrackingModel>(
                _spTrackingGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    item.TaskName = FixVietnameseMojibake(item.TaskName);
                    item.ResultNote = FixVietnameseMojibake(item.ResultNote);
                    item.AssignedUserName = FixVietnameseMojibake(item.AssignedUserName);
                    item.CompletedByName = FixVietnameseMojibake(item.CompletedByName);
                    item.LastModifiedByName = FixVietnameseMojibake(item.LastModifiedByName);
                    item.CreatedByName = FixVietnameseMojibake(item.CreatedByName);
                    item.ProcessName = FixVietnameseMojibake(item.ProcessName);
                    item.SalesStatusName = FixVietnameseMojibake(item.SalesStatusName);
                }
                var parents = list.Where(t => !t.ParentID.HasValue || t.ParentID.Value <= 0).ToList();
                var children = list.Where(t => t.ParentID.HasValue && t.ParentID.Value > 0).ToList();
                foreach (var p in parents)
                {
                    p.TodoList = children.Where(c => c.ParentID == p.TrackingID).OrderBy(c => c.SortOrder).ThenBy(c => c.StartDate).ThenBy(c => c.TrackingID).ToList();
                }
            }
            return list ?? new List<RM_DigitalSalesTrackingModel>();
        }

        public int UpdateTrackingStatus(int trackingId, byte status, string resultNote, string attachmentFile, int? assignedUserId, DateTime? deadline, string username)
        {
            resultNote = FixVietnameseMojibake(resultNote);
            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingUpdateStatus,
                DATA_PROVIDER_NAME,
                trackingId,
                status,
                resultNote,
                attachmentFile,
                assignedUserId.HasValue ? (object)assignedUserId.Value : DBNull.Value,
                deadline.HasValue ? (object)deadline.Value : DBNull.Value,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int SaveTracking(RM_DigitalSalesTrackingModel model, string username)
        {
            if (model != null)
            {
                model.TaskName = FixVietnameseMojibake(model.TaskName);
                model.ResultNote = FixVietnameseMojibake(model.ResultNote);
            }
            // Nếu thêm tiến trình thực tế mới vào quy trình đang có placeholder rỗng, xóa placeholder đi
            if (model.TrackingID <= 0 && model.ProcessID.HasValue && model.ProcessID.Value > 0 && !string.IsNullOrWhiteSpace(model.TaskName) && (!model.ParentID.HasValue || model.ParentID.Value <= 0))
            {
                try
                {
                    var existingTasks = GetTrackingTasks(model.DigitalSalesID);
                    var emptyPlaceholders = existingTasks.Where(t => t.ProcessID == model.ProcessID.Value 
                        && (!t.ParentID.HasValue || t.ParentID.Value <= 0) 
                        && string.IsNullOrWhiteSpace(t.TaskName)
                        && (!model.TimelineID.HasValue || t.TimelineID == model.TimelineID.Value)).ToList();
                    foreach (var ep in emptyPlaceholders)
                    {
                        DeleteTracking(ep.TrackingID, username);
                    }
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                }
            }

            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingSave,
                DATA_PROVIDER_NAME,
                model.TrackingID,
                model.DigitalSalesID,
                model.ProcessID.HasValue ? (object)model.ProcessID.Value : DBNull.Value,
                model.ProgressID.HasValue ? (object)model.ProgressID.Value : DBNull.Value,
                model.TaskName,
                model.AssignedUserID.HasValue ? (object)model.AssignedUserID.Value : DBNull.Value,
                model.StartDate,
                model.Deadline.HasValue ? (object)model.Deadline.Value : DBNull.Value,
                model.Status,
                model.ResultNote,
                model.AttachmentFile,
                model.IsCustomTask,
                model.SortOrder,
                username,
                model.ParentID.HasValue ? (object)model.ParentID.Value : DBNull.Value,
                model.DurationDays.HasValue ? (object)model.DurationDays.Value : DBNull.Value,
                model.TimelineID.HasValue ? (object)model.TimelineID.Value : DBNull.Value
            );
            return result.GetValueOrDefault(0);
        }

        public int ChangeProcessOfStatus(int digitalSalesId, int statusId, int newProcessId, string username)
        {
            if (digitalSalesId <= 0 || statusId <= 0 || newProcessId <= 0) return 0;

            var currentTasks = GetTrackingTasks(digitalSalesId);
            var oldTasks = currentTasks.Where(t => t.StatusID == statusId && (!t.ParentID.HasValue || t.ParentID.Value <= 0)).ToList();
            int? currentTimelineId = oldTasks.FirstOrDefault(t => t.TimelineID.HasValue)?.TimelineID;
            if (!currentTimelineId.HasValue)
            {
                var sales = GetByID(digitalSalesId, username);
                currentTimelineId = sales?.Timelines?
                    .Where(tl => tl.ToStatusID == statusId && (!tl.FromStatusID.HasValue || tl.FromStatusID.Value != tl.ToStatusID))
                    .OrderByDescending(tl => tl.ActionDate).ThenByDescending(tl => tl.TimelineID)
                    .FirstOrDefault()?.TimelineID;
            }

            foreach (var ot in oldTasks)
            {
                DeleteTracking(ot.TrackingID, username);
            }

            var progressList = new RM_DigitalSalesWorkflowBiz().GetProgressesByProcess(newProcessId);
            if (progressList != null && progressList.Count > 0)
            {
                int sort = 1;
                foreach (var pg in progressList.OrderBy(p => p.SortOrder))
                {
                    int duration = pg.DefaultDurationDays > 0 ? pg.DefaultDurationDays : 3;
                    var task = new RM_DigitalSalesTrackingModel
                    {
                        TrackingID = 0,
                        DigitalSalesID = digitalSalesId,
                        ProcessID = newProcessId,
                        ProgressID = pg.ProgressID,
                        TaskName = pg.ProgressName,
                        DurationDays = duration,
                        DefaultDurationDays = duration,
                        StartDate = DateTime.Now,
                        Deadline = DateTime.Now.AddDays(duration),
                        Status = 1,
                        IsCustomTask = false,
                        SortOrder = sort++,
                        TimelineID = currentTimelineId
                    };
                    SaveTracking(task, username);
                }
            }
            else
            {
                // Nếu quy trình được chọn chưa có tiến trình mẫu nào, lưu 1 bản ghi tiến trình rỗng
                // để giữ quy trình hiển thị trên bảng Checklist và cho phép bấm "Thêm tiến trình"
                var emptyTask = new RM_DigitalSalesTrackingModel
                {
                    TrackingID = 0,
                    DigitalSalesID = digitalSalesId,
                    ProcessID = newProcessId,
                    ProgressID = null,
                    TaskName = string.Empty,
                    DurationDays = 3,
                    DefaultDurationDays = 3,
                    StartDate = DateTime.Now,
                    Deadline = DateTime.Now.AddDays(3),
                    Status = 1,
                    IsCustomTask = true,
                    SortOrder = 1,
                    TimelineID = currentTimelineId
                };
                SaveTracking(emptyTask, username);
            }

            return 1;
        }

        public int DeleteTracking(int trackingId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spTrackingDelete,
                DATA_PROVIDER_NAME,
                trackingId,
                username
            );
            return result.GetValueOrDefault(0);
        }


        public List<RM_DigitalSalesMemberModel> GetMembersBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesMemberModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesMemberModel>(
                _spMemberGetBySalesID,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            if (list != null)
            {
                foreach (var item in list)
                {
                    item.FullName = FixVietnameseMojibake(item.FullName);
                    item.RoleTitle = FixVietnameseMojibake(item.RoleTitle);
                    item.Note = FixVietnameseMojibake(item.Note);
                }
            }
            return list ?? new List<RM_DigitalSalesMemberModel>();
        }

        public int SaveMember(RM_DigitalSalesMemberModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spMemberSave,
                DATA_PROVIDER_NAME,
                model.MemberID,
                model.DigitalSalesID,
                model.UserID,
                model.RoleTitle,
                model.IsAM,
                model.Note,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteMember(int memberId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spMemberDelete,
                DATA_PROVIDER_NAME,
                memberId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesTimelineModel> GetTimeline(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTimelineModel>();
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesTimelineModel>(
                _spGetTimeline,
                DATA_PROVIDER_NAME,
                digitalSalesId
            );
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    item.Note = FixVietnameseMojibake(item.Note);
                    item.ActionByName = FixVietnameseMojibake(item.ActionByName);
                    item.FromStatusName = FixVietnameseMojibake(item.FromStatusName);
                    item.ToStatusName = FixVietnameseMojibake(item.ToStatusName);
                }
            }
            return list ?? new List<RM_DigitalSalesTimelineModel>();
        }

        public int Delete(int digitalSalesId, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(
                _spDelete,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public List<RM_DigitalSalesStatusModel> GetStatusList(byte? businessType = null)
        {
            var list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesStatusModel>(
                _spStatusGetAll,
                DATA_PROVIDER_NAME,
                businessType.HasValue ? (object)businessType.Value : DBNull.Value
            );
            return list ?? new List<RM_DigitalSalesStatusModel>();
        }

        public string GenerateNextCode()
        {
            try
            {
                var currentYear = DateTime.Now.Year.ToString();
                int total = 0;
                var latest = LoadList(out total, new RM_DigitalSalesSearchModel());
                int maxId = 0;
                if (latest != null && latest.Count > 0)
                {
                    maxId = latest.Max(x => x.DigitalSalesID);
                }
                int nextSeq = maxId + 1;
                return $"SPDV-{currentYear}-{nextSeq:D4}";
            }
            catch
            {
                return $"SPDV-{DateTime.Now.Year}-0001";
            }
        }

        public List<RM_DigitalSalesActivityModel> GetActivitiesBySalesID(int digitalSalesId, byte? activityType = null)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesActivityModel>();
            List<RM_DigitalSalesActivityModel> list = null;
            try
            {
                list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesActivityModel>(
                    _spActivityGetList,
                    DATA_PROVIDER_NAME,
                    digitalSalesId,
                    activityType.HasValue ? (object)activityType.Value : DBNull.Value
                );
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return new List<RM_DigitalSalesActivityModel>();
            }

            if (list != null && list.Count > 0)
            {
                if (!activityType.HasValue)
                {
                    // Lọc bỏ hoàn toàn các hoạt động checklist (3, 4, 5) khỏi dòng thảo luận/hoạt động
                    list = list.Where(a => a.ActivityType != 3 && a.ActivityType != 4 && a.ActivityType != 5).ToList();
                }

                foreach (var item in list)
                {
                    item.Content = FixVietnameseMojibake(item.Content);
                    item.ActionByName = FixVietnameseMojibake(item.ActionByName);

                    if (!string.IsNullOrWhiteSpace(item.Attachments))
                    {
                        try
                        {
                            if (item.Attachments.TrimStart().StartsWith("["))
                            {
                                item.AttachmentList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActivityAttachmentItem>>(item.Attachments) ?? new List<ActivityAttachmentItem>();
                            }
                            else
                            {
                                var parts = item.Attachments.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    var trimmed = p?.Trim();
                                    if (string.IsNullOrEmpty(trimmed)) continue;
                                    string ext = "";
                                    string fname = trimmed;
                                    try
                                    {
                                        ext = System.IO.Path.GetExtension(trimmed)?.ToLowerInvariant() ?? "";
                                        fname = System.IO.Path.GetFileName(trimmed);
                                    }
                                    catch
                                    {
                                        var lastSlash = trimmed.LastIndexOfAny(new[] { '/', '\\' });
                                        if (lastSlash >= 0 && lastSlash < trimmed.Length - 1) fname = trimmed.Substring(lastSlash + 1);
                                        var lastDot = fname.LastIndexOf('.');
                                        if (lastDot >= 0) ext = fname.Substring(lastDot).ToLowerInvariant();
                                    }
                                    item.AttachmentList.Add(new ActivityAttachmentItem
                                    {
                                        FileName = fname,
                                        FilePath = trimmed,
                                        Extension = ext,
                                        IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext)
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parse error
                        }
                    }
                }
            }

            return list ?? new List<RM_DigitalSalesActivityModel>();
        }

        public List<RM_DigitalSalesActivityModel> GetActivitiesByTrackingID(int digitalSalesId, int trackingId)
        {
            if (digitalSalesId <= 0 || trackingId <= 0) return new List<RM_DigitalSalesActivityModel>();
            List<RM_DigitalSalesActivityModel> list = null;
            try
            {
                list = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesActivityModel>(
                    _spActivityGetList,
                    DATA_PROVIDER_NAME,
                    digitalSalesId,
                    (byte)255
                );
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return new List<RM_DigitalSalesActivityModel>();
            }

            if (list != null && list.Count > 0)
            {
                var targetIds = new HashSet<int> { trackingId };
                try
                {
                    var allTasks = GetTrackingTasks(digitalSalesId);
                    var parentTask = allTasks.FirstOrDefault(t => t.TrackingID == trackingId);
                    if (parentTask != null && parentTask.TodoList != null && parentTask.TodoList.Count > 0)
                    {
                        foreach (var child in parentTask.TodoList)
                        {
                            targetIds.Add(child.TrackingID);
                        }
                    }
                }
                catch { }

                list = list.Where(a => a.ReferenceID.HasValue && targetIds.Contains(a.ReferenceID.Value)).OrderByDescending(a => a.ActionDate).ToList();

                foreach (var item in list)
                {
                    item.Content = FixVietnameseMojibake(item.Content);
                    item.ActionByName = FixVietnameseMojibake(item.ActionByName);

                    if (!string.IsNullOrWhiteSpace(item.Attachments))
                    {
                        try
                        {
                            if (item.Attachments.TrimStart().StartsWith("["))
                            {
                                item.AttachmentList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActivityAttachmentItem>>(item.Attachments) ?? new List<ActivityAttachmentItem>();
                            }
                            else
                            {
                                var parts = item.Attachments.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var p in parts)
                                {
                                    var trimmed = p?.Trim();
                                    if (string.IsNullOrEmpty(trimmed)) continue;
                                    string ext = "";
                                    string fname = trimmed;
                                    try
                                    {
                                        ext = System.IO.Path.GetExtension(trimmed)?.ToLowerInvariant() ?? "";
                                        fname = System.IO.Path.GetFileName(trimmed);
                                    }
                                    catch
                                    {
                                        var lastSlash = trimmed.LastIndexOfAny(new[] { '/', '\\' });
                                        if (lastSlash >= 0 && lastSlash < trimmed.Length - 1) fname = trimmed.Substring(lastSlash + 1);
                                        var lastDot = fname.LastIndexOf('.');
                                        if (lastDot >= 0) ext = fname.Substring(lastDot).ToLowerInvariant();
                                    }
                                    item.AttachmentList.Add(new ActivityAttachmentItem
                                    {
                                        FileName = fname,
                                        FilePath = trimmed,
                                        Extension = ext,
                                        IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext)
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parse error
                        }
                    }
                }
            }

            return list ?? new List<RM_DigitalSalesActivityModel>();
        }

        public int AddActivity(RM_DigitalSalesActivityModel model, string username)
        {
            if (model == null || model.DigitalSalesID <= 0 || string.IsNullOrWhiteSpace(model.Content)) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityAdd,
                DATA_PROVIDER_NAME,
                model.DigitalSalesID,
                model.ActivityType,
                model.Content,
                string.IsNullOrWhiteSpace(model.Attachments) ? (object)DBNull.Value : model.Attachments,
                string.IsNullOrWhiteSpace(model.MentionedUserIDs) ? (object)DBNull.Value : model.MentionedUserIDs,
                string.IsNullOrWhiteSpace(model.MentionedNames) ? (object)DBNull.Value : model.MentionedNames,
                model.ReferenceID.HasValue ? (object)model.ReferenceID.Value : DBNull.Value,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int DeleteActivity(int activityId, string username)
        {
            if (activityId <= 0) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityDelete,
                DATA_PROVIDER_NAME,
                activityId,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int UpdateLatestStatusChangeActivityAttachments(int digitalSalesId, string attachmentsJson, string username)
        {
            if (digitalSalesId <= 0 || string.IsNullOrWhiteSpace(attachmentsJson)) return 0;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spActivityUpdateLatestStatusChange,
                DATA_PROVIDER_NAME,
                digitalSalesId,
                attachmentsJson,
                username
            );
            return result.GetValueOrDefault(0);
        }

        public int? GetDepartmentByUserID(int userId)
        {
            if (userId <= 0) return null;
            var result = AppProcessor.ProcedureProvider.Execute(
                _spGetDepartmentByUserID,
                DATA_PROVIDER_NAME,
                userId);
            return (result.HasValue && result.Value > 0) ? result.Value : (int?)null;
        }

        public List<RM_DigitalSalesUserModel> GetAccessibleEmployees(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return new List<RM_DigitalSalesUserModel>();
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_DigitalSalesUserModel>(
                _spGetAccessibleEmployees,
                DATA_PROVIDER_NAME,
                userName.Trim()) ?? new List<RM_DigitalSalesUserModel>();
        }

        private static readonly Dictionary<char, byte> _win1252Map = new Dictionary<char, byte>()
        {
            { '\u20AC', 0x80 }, { '\u201A', 0x82 }, { '\u0192', 0x83 }, { '\u201E', 0x84 },
            { '\u2026', 0x85 }, { '\u2020', 0x86 }, { '\u2021', 0x87 }, { '\u02C6', 0x88 },
            { '\u2030', 0x89 }, { '\u0160', 0x8A }, { '\u2039', 0x8B }, { '\u0152', 0x8C },
            { '\u017D', 0x8E }, { '\u2018', 0x91 }, { '\u2019', 0x92 }, { '\u201C', 0x93 },
            { '\u201D', 0x94 }, { '\u2022', 0x95 }, { '\u2013', 0x96 }, { '\u2014', 0x97 },
            { '\u02DC', 0x98 }, { '\u2122', 0x99 }, { '\u0161', 0x9A }, { '\u203A', 0x9B },
            { '\u0153', 0x9C }, { '\u017E', 0x9E }, { '\u0178', 0x9F }
        };

        public static bool HasMojibakeSignature(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.Contains("áº") || input.Contains("á»") ||
                   input.Contains("Ä‘") || input.Contains("Ä‚") || input.Contains("Äƒ") ||
                   input.Contains("Æ°") || input.Contains("Æ¡") || input.Contains("Æ¯") || input.Contains("Æ") ||
                   input.Contains("Ã¡") || input.Contains("Ã ") || input.Contains("Ã£") || input.Contains("Ã©") ||
                   input.Contains("Ã¨") || input.Contains("Ã³") || input.Contains("Ã²") || input.Contains("Ãº") ||
                   input.Contains("Ã¹") || input.Contains("Ã½") || input.Contains("Ã´") || input.Contains("Ãª") ||
                   input.Contains("Ã¢") || input.Contains("CÆ") || input.Contains("Dá»") || input.Contains("Tráº") ||
                   input.Contains("NgÆ") || input.Contains("Chuyá»") || input.Contains("Chá»§") || input.Contains("Bá» ");
        }

        public static string FixVietnameseMojibake(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (!HasMojibakeSignature(input))
            {
                return input;
            }

            input = input
                .Replace("AM (Chá»§ trÃ¬ kinh doanh)", "AM (Chủ trì kinh doanh)")
                .Replace("Chá»§ trÃ¬ kinh doanh", "Chủ trì kinh doanh")
                .Replace("Chá»§ trÃ¬", "Chủ trì")
                .Replace("NgÆ°á» i táº¡o há»“ sÆ¡ cÆ¡ há»™i", "Người tạo hồ sơ cơ hội")
                .Replace("NgÆ°á»\u009di táº¡o há»“ sÆ¡ cÆ¡ há»™i", "Người tạo hồ sơ cơ hội")
                .Replace("Ng\u00C6\u00B0\u00E1\u00BB\u009Di t\u00E1\u00BA\u00A1o h\u00E1\u00BB\u201C s\u00C6\u00A1 c\u00C6\u00A1 h\u00E1\u00BB\u2122i", "Người tạo hồ sơ cơ hội")
                .Replace("Chuyá»ƒn Ä‘á»•i thÃ nh cÃ´ng tá»« CÆ  Há»˜I sang Dá»° Ã N. Tráº¡ng thÃ¡i má»›i:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                .Replace("Chuyá»ƒn Ä'á»•i thÃ nh cÃ´ng tá»« CÆ  Há»™i sang Dá»± Ã¡N. Tráº¡ng thÃ¡i má»›i:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                .Replace("Chuyá»ƒn Ä‘á»•i thÃ nh cÃ´ng tá»« CÆ  Há»˜I sang Dá»° Ã N. Trạng thái mới:", "Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới:")
                .Replace("Chuyá»ƒn tráº¡ng thÃ¡i sang:", "Chuyển trạng thái sang:")
                .Replace("Chuyá»ƒn tráº¡ng thÃ¡i", "Chuyển trạng thái")
                .Replace("Ghi chÃº:", "Ghi chú:")
                .Replace("Ä Ã¡nh dáº¥u lÃ  Dá»± Ã¡n trá» ng Ä‘iá»ƒm", "Đánh dấu là Dự án trọng điểm")
                .Replace("ÄÃ¡nh dáº¥u lÃ  Dá»± Ã¡n trá»ng Ä'iá»ƒm", "Đánh dấu là Dự án trọng điểm")
                .Replace("Ä Ã¡nh dáº¥u lÃ  Dá»± Ã¡n", "Đánh dấu là Dự án")
                .Replace("Bá»  Ä‘Ã¡nh dáº¥u Dá»± Ã¡n trá» ng Ä‘iá»ƒm", "Bỏ đánh dấu Dự án trọng điểm")
                .Replace("Bá»  Ä‘Ã¡nh dáº¥u", "Bỏ đánh dấu")
                .Replace("Ä Ã£ hoÃ n thÃ nh cÃ´ng viá»‡c checklist:", "Đã hoàn thành công việc checklist:")
                .Replace("Ä Ã£ hoÃ n thÃ nh 100% cÃ¡c cÃ´ng viá»‡c trong quy trÃ¬nh:", "Đã hoàn thành 100% các công việc trong quy trình:")
                .Replace("Cáº­p nháº­t tiáº¿n Ä‘á»™ cÃ´ng viá»‡c", "Cập nhật tiến độ công việc")
                .Replace("Káº¿t quáº£:", "Kết quả:");

            if (!HasMojibakeSignature(input))
            {
                return input;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                List<byte> byteBuffer = new List<byte>();

                Action flushBytes = () =>
                {
                    if (byteBuffer.Count > 0)
                    {
                        try
                        {
                            string decoded = Encoding.UTF8.GetString(byteBuffer.ToArray());
                            sb.Append(decoded);
                        }
                        catch
                        {
                            foreach (byte bVal in byteBuffer) sb.Append((char)bVal);
                        }
                        byteBuffer.Clear();
                    }
                };

                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    byte b;
                    if (c <= 0xFF)
                    {
                        byteBuffer.Add((byte)c);
                    }
                    else if (_win1252Map.TryGetValue(c, out b))
                    {
                        byteBuffer.Add(b);
                    }
                    else
                    {
                        flushBytes();
                        sb.Append(c);
                    }
                }
                flushBytes();

                var result = sb.ToString();
                if (!string.IsNullOrEmpty(result) && result.Contains('\uFFFD') && !input.Contains('\uFFFD'))
                {
                    return input;
                }
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch
            {
                return input;
            }
        }

        private readonly string _spGetGroupServiceChart = "RM_DigitalSales_GetGroupServiceChart";

        public List<GroupServiceChartModel> GetGroupServiceChart(int? applyYear, string username, string employeeIds)
        {
            try
            {
                var data = AppProcessor.ProcedureProvider.ExecuteTypedList<GroupServiceChartModel>(
                    _spGetGroupServiceChart,
                    DATA_PROVIDER_NAME,
                    applyYear,
                    null,
                    null,
                    employeeIds,
                    username
                );
                return data ?? new List<GroupServiceChartModel>();
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return new List<GroupServiceChartModel>();
            }
        }
    }
}

