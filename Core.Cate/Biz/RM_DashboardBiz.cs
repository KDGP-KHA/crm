using System;
using System.Collections.Generic;
using System.Linq;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_DashboardBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";
        private const string SP_GET_PLANS = "RM_Dashboard_Plan_Get";
        private const string SP_GET_PLANS_V2 = "RM_Dashboard_Plan_Get_V2";
        private const string SP_GET_SUMMARY = "RM_Dashboard_GetSummary";
        private const string SP_GET_OPPORTUNITY_BY_STATUS = "RM_Dashboard_GetOpportunityByStatus";
        private const string SP_GET_OPPORTUNITY_BY_SERVICE = "RM_Dashboard_GetOpportunityByService";
        private const string SP_GET_PROJECT_BY_STATUS = "RM_Dashboard_GetProjectByStatus";
        private const string SP_GET_PROJECTS = "RM_Dashboard_Project_Get";
        private const string SP_GET_PROJECTS_V2 = "RM_Dashboard_Project_Get_V2";
        private const string SP_GET_OPPORTUNITIES = "RM_Dashboard_Opportunity_Get";
        private const string SP_GET_OPPORTUNITIES_V2 = "RM_Dashboard_Opportunity_Get_V2";
        private const string SP_GET_DATAFOR_PIECHART = "RM_Dashboard_GetPieChart";
        private const string SP_GET_OPPORTUNITY_BY_GROUPSERVICE = "RM_Dashboard_GetOpportunityByGroupService";
        private const string SP_GET_PROJECT_BY_GROUPSERVICE = "RM_Dashboard_GetProjectByGroupService";
        private const string SP_GET_OPPORTUNITIES_BY_GROUPSERVICE = "RM_Dashboard_Opportunity_GetByGroupService";
        private const string SP_GET_PROJECTS_BY_GROUPSERVICE = "RM_Dashboard_Project_GetByGroupService";
        private const string SP_GET_STALE_UPDATES = "RM_Dashboard_StaleUpdates_Get";

        /// <summary>
        /// Lấy cơ hội và dự án đã từ ba ngày trở lên chưa có cập nhật trong phạm vi nhân sự được phép xem.
        /// </summary>
        public List<RM_StaleUpdateModel> GetStaleUpdates(string employeeIds)
        {
            try
            {
                return AppProcessor.ProcedureProvider
                    .ExecuteTypedList<RM_StaleUpdateModel>(
                        SP_GET_STALE_UPDATES, DATA_PROVIDER_NAME,
                        DateTime.Today, employeeIds)
                    ?? new List<RM_StaleUpdateModel>();
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return new List<RM_StaleUpdateModel>();
            }
        }

        public RM_OverviewDashboardModel GetOverview(DashboardSearchModel search)
        {
            var model = new RM_OverviewDashboardModel();

            model.Summary = AppProcessor.ProcedureProvider
                .ExecuteScalarObject<DashboardSummaryModel>(
                    SP_GET_SUMMARY, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, 0, search.EmployeeIds);
            model.FeasibilitySummary = AppProcessor.ProcedureProvider
                .ExecuteScalarObject<DashboardSummaryModel>(
                    SP_GET_SUMMARY, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, 80, search.EmployeeIds);

            model.OpportunityByStatus = AppProcessor.ProcedureProvider
                .ExecuteTypedList<OpportunityByStatusModel>(
                    SP_GET_OPPORTUNITY_BY_STATUS, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds);

            model.OpportunityByService = AppProcessor.ProcedureProvider
                .ExecuteTypedList<OpportunityByServiceModel>(
                    SP_GET_OPPORTUNITY_BY_SERVICE, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds);

            model.ProjectByStatus = AppProcessor.ProcedureProvider
                .ExecuteTypedList<ProjectByStatusModel>(
                    SP_GET_PROJECT_BY_STATUS, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds);

            return model;
        }

        /// <summary>
        /// Chart số cơ hội hoặc dự án theo nhóm dịch vụ.
        /// </summary>
        /// <param name="isProject">1 = dự án, 0 = cơ hội kinh doanh.</param>
        /// <param name="search">Điều kiện lọc thời gian và phạm vi nhân sự.</param>
        /// <returns>Danh sách nhóm dịch vụ kèm số lượng.</returns>
        public List<GroupServiceChartModel> GetGroupServiceChart(int isProject, DashboardSearchModel search)
        {
            var spName = isProject == 1
                ? SP_GET_PROJECT_BY_GROUPSERVICE
                : SP_GET_OPPORTUNITY_BY_GROUPSERVICE;

            // Truyền cả Username để SP áp cùng điều kiện quyền với SP danh sách chi tiết,
            // bảo đảm số trên chart khớp với popup khi click vào
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<GroupServiceChartModel>(
                    spName, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds, search.Username)
                ?? new List<GroupServiceChartModel>();

            return data;
        }

        /// <summary>
        /// Danh sách cơ hội kinh doanh thuộc một nhóm dịch vụ (popup chi tiết từ chart).
        /// </summary>
        public List<OpportunityDashboardModel> GetOpportunitiesByGroupService(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<OpportunityDashboardModel>(
                    SP_GET_OPPORTUNITIES_BY_GROUPSERVICE, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.GroupServiceID, search.Username)
                ?? new List<OpportunityDashboardModel>();

            total = data.Count;
            return data;
        }

        /// <summary>
        /// Danh sách dự án thuộc một nhóm dịch vụ (popup chi tiết từ chart).
        /// </summary>
        public List<ProjectDashboardModel> GetProjectsByGroupService(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<ProjectDashboardModel>(
                    SP_GET_PROJECTS_BY_GROUPSERVICE, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.GroupServiceID, search.Username)
                ?? new List<ProjectDashboardModel>();

            total = data.Count;
            return data;
        }

        public List<DataForPieChartModel> GetPieChart(int isProject, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<DataForPieChartModel>(
                    SP_GET_DATAFOR_PIECHART, DATA_PROVIDER_NAME,
                    isProject, search.FromDate, search.ToDate, search.EmployeeIds)
                ?? new List<DataForPieChartModel>();
            return data;
        }

        public List<ProjectDashboardModel> GetProjects(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<ProjectDashboardModel>(
                    SP_GET_PROJECTS, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds)
                ?? new List<ProjectDashboardModel>();

            total = data.Count;
            return data;
        }
        public List<ProjectDashboardModel> GetProjectsV2(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<ProjectDashboardModel>(
                    SP_GET_PROJECTS_V2, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.SuccessRate, search.Username, search.IsGreaterOrEqual)
                ?? new List<ProjectDashboardModel>();

            total = data.Count;
            return data;
        }

        public List<OpportunityDashboardModel> GetOpportunities(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<OpportunityDashboardModel>(
                    SP_GET_OPPORTUNITIES, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds, search.Type)
                ?? new List<OpportunityDashboardModel>();

            total = data.Count;
            return data;
        }

        public List<OpportunityDashboardModel> GetOpportunitiesV2(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<OpportunityDashboardModel>(
                    SP_GET_OPPORTUNITIES_V2, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.SuccessRate, search.Username, search.Type, search.IsGreaterOrEqual)
                ?? new List<OpportunityDashboardModel>();

            total = data.Count;
            return data;
        }
        /// <summary>
        /// Lấy danh sách kế hoạch kèm người liên quan (gộp sẵn trong SP);
        /// trả về kế hoạch thuộc các nhân sự trong EmployeeIds hoặc kế hoạch mà Username là người liên quan.
        /// </summary>
        public List<PlanDashboardModel> GetPlansV2(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<PlanDashboardModel>(
                    SP_GET_PLANS_V2, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds, search.Username)
                ?? new List<PlanDashboardModel>();

            total = data.Count;
            return data;
        }

        /// <summary>
        /// Lấy danh sách kế hoạch kinh doanh theo bộ lọc
        /// </summary>
        public List<PlanDashboardModel> GetPlans(out int total, DashboardSearchModel search)
        {
            var data = AppProcessor.ProcedureProvider
                .ExecuteTypedList<PlanDashboardModel>(
                    SP_GET_PLANS, DATA_PROVIDER_NAME,
                    search.FromDate, search.ToDate, search.EmployeeIds)
                ?? new List<PlanDashboardModel>();

            total = data.Count;
            return data;
        }

    }
}
