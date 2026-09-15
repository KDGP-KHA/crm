using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web.Helpers;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Cate.Biz
{
    public class RM_ReviewBatchItemBiz
    {
        private const string DATA_PROVIDER_NAME = "CenIT.Provider.Major";
        private readonly string _RM_Review_GetBusinessOpportunity = "RM_Review_GetBusinessOpportunity";
        private readonly string _RM_Review_GetProject = "RM_Review_GetProject";
        private readonly string _RM_DigitalSalesReview_GetList = "RM_DigitalSalesReview_GetList";

        private readonly string _RM_DigitalSalesReview_Save = "RM_DigitalSalesReview_Save";
        private readonly string _RM_ReviewBatch_GetHistory = "RM_ReviewBatch_GetHistory";
        private readonly string _RM_DigitalSalesReview_GetHistory = "RM_DigitalSalesReview_GetHistory";
        private readonly string _RM_ReviewHistory_Save = "RM_ReviewHistory_Save";
        private readonly string _RM_ReviewHistory_GetByID = "RM_ReviewHistory_GetByID";

        private readonly string _RM_ReviewHistoryFilePath_Get = "RM_ReviewHistoryFilePath_Get";
        private readonly string _RM_ReviewHistoryFilePath_GetByID = "RM_ReviewHistoryFilePath_GetByID";
        private readonly string _RM_ReviewHistoryFilePath_Save = "RM_ReviewHistoryFilePath_Save";
        private readonly string _RM_ReviewHistoryFilePath_Delete = "RM_ReviewHistoryFilePath_Delete";

        public List<RM_ReviewDigitalSalesModel> LoadDigitalSales(out int total, RM_ReviewDigitalSalesSearchModel model, BaseSearchModel search)
        {
            search = search ?? new BaseSearchModel
            {
                Search = null,
                Order = "1",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };

            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewDigitalSalesModel>(
                _RM_DigitalSalesReview_GetList,
                DATA_PROVIDER_NAME,
                model.ReviewBatchID,
                model.BusinessType,
                model.DepartmentID,
                model.EmployeeID,
                model.StatusID,
                model.IsReviewed,
                model.Keyword,
                search.Order,
                search.OrderDir,
                search.StartIndex,
                search.PageSize,
                model.UserName) ?? new List<RM_ReviewDigitalSalesModel>();

            total = data.Count > 0 ? Convert.ToInt32(data.First().TotalRow) : 0;
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách theo giá trị lọc
        /// </summary>
        /// <returns>Danh sách RM_ReviewBatchItem</returns>
        public List<RM_ReviewProjectModel> LoadProject(out int total, RM_ReviewProjectSearchModel model, BaseSearchModel search)
        {
            search = search ?? new BaseSearchModel
            {
                Search = null,
                Order = "0",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewProjectModel>(_RM_Review_GetProject,
                DATA_PROVIDER_NAME,
                model.ReviewBatchID,
                model.BoPhanID,
                model.EmployeeID,
                model.Status,
                model.IsReviewed,
                model.Keyword,
                search.Order,
                search.OrderDir,
                search.StartIndex,
                search.PageSize,
                model.UserName);
            total = 0;
            if (data != null && data.Count > 0)
                total = int.Parse(data.First()?.TotalRow.ToString() ?? "0");
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Danh sách RM_ReviewBatchItem</returns>
        public List<RM_ReviewProjectModel> GetAllProject()
        {
            int total;
            var model = new RM_ReviewProjectSearchModel();
            var list = LoadProject(out total, model, null);
            return list;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách theo giá trị lọc
        /// </summary>
        /// <returns>Danh sách RM_ReviewBatchItem</returns>
        public List<RM_ReviewBusinessOpportunityModel> LoadBusinessOpportunity(out int total, RM_ReviewBusinessOpportunitySearchModel model, BaseSearchModel search)
        {
            search = search ?? new BaseSearchModel
            {
                Search = null,
                Order = "0",
                OrderDir = "ASC",
                StartIndex = 0,
                PageSize = -1
            };
            var data = AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewBusinessOpportunityModel>(_RM_Review_GetBusinessOpportunity,
                DATA_PROVIDER_NAME,
                model.ReviewBatchID,
                model.BoPhanID,
                model.EmployeeID,
                model.StatusID,
                model.IsReviewed,
                model.Keyword,
                search.Order,
                search.OrderDir,
                search.StartIndex,
                search.PageSize,
                model.UserName);
            total = 0;
            if (data != null && data.Count > 0)
                total = int.Parse(data.First()?.TotalRow.ToString() ?? "0");
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Danh sách RM_ReviewBatchItem</returns>
        public List<RM_ReviewBusinessOpportunityModel> GetAllBusinessOpportunity()
        {
            int total;
            var model = new RM_ReviewBusinessOpportunitySearchModel();
            var list = LoadBusinessOpportunity(out total, model, null);
            return list;
        }

        /// <summary>
        /// Lưu thông tin rà soát
        /// </summary>
        public int Save(RM_ReviewFormModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_RM_DigitalSalesReview_Save, DATA_PROVIDER_NAME,
                model.ReviewBatchID,
                model.DigitalSalesID,
                model.ReviewComment,
                model.IsConfirmed,
                model.ReviewConclusion,
                username);
            return result.GetValueOrDefault(0);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách theo giá trị lọc
        /// </summary>
        /// <returns>Danh sách RM_ExchangeHistory</returns>
        public List<RM_ReviewHistoryModel> GetHistory(int objectType, int objectID)
        {
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewHistoryModel>(
                _RM_ReviewBatch_GetHistory,
                DATA_PROVIDER_NAME,
                objectType,
                objectID)
            ?? new List<RM_ReviewHistoryModel>();
        }

        public List<RM_ReviewHistoryModel> GetDigitalSalesHistory(int digitalSalesID)
        {
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewHistoryModel>(
                _RM_DigitalSalesReview_GetHistory,
                DATA_PROVIDER_NAME,
                digitalSalesID) ?? new List<RM_ReviewHistoryModel>();
        }

        /// <summary>
        /// Cập nhật lích sử rà soát
        /// </summary>
        public int SaveHistory(RM_ReviewFormModel model, string username)
        {
            var result = AppProcessor.ProcedureProvider.Execute(_RM_ReviewHistory_Save, DATA_PROVIDER_NAME,
                model.ReviewHistoryID,
                model.ReviewBatchItemID,
                model.ReviewComment,
                model.IsConfirmed,
                model.ReviewConclusion,
                username);
            return result.GetValueOrDefault(0);
        }

        /// <summary>
        /// Lấy RM_Task theo ID
        /// </summary>
        public RM_ReviewHistoryModel LoadDetailHistory(int ID)
        {
            return AppProcessor.ProcedureProvider.ExecuteScalarObject<RM_ReviewHistoryModel>(_RM_ReviewHistory_GetByID, DATA_PROVIDER_NAME, ID);
        }

        public List<RM_ReviewBatchFilePathModel> GetFilePaths(int reviewHistoryID)
        {
            return AppProcessor.ProcedureProvider.ExecuteTypedList<RM_ReviewBatchFilePathModel>(
                _RM_ReviewHistoryFilePath_Get, DATA_PROVIDER_NAME, reviewHistoryID) ?? new List<RM_ReviewBatchFilePathModel>();
        }

        public RM_ReviewBatchFilePathModel GetFilePathById(int id)
        {
            return AppProcessor.ProcedureProvider.ExecuteScalarObject<RM_ReviewBatchFilePathModel>(
                _RM_ReviewHistoryFilePath_GetByID, DATA_PROVIDER_NAME, id);
        }

        public int SaveFilePath(RM_ReviewBatchFilePathModel model, string username)
        {
            return AppProcessor.ProcedureProvider.Execute(_RM_ReviewHistoryFilePath_Save, DATA_PROVIDER_NAME,
                model.FilePathID, model.ReviewHistoryID, model.FilePath, username).GetValueOrDefault(0);
        }

        public int DeleteFilePath(int filePathID, string username)
        {
            return AppProcessor.ProcedureProvider.Execute(_RM_ReviewHistoryFilePath_Delete, DATA_PROVIDER_NAME,
                filePathID, username).GetValueOrDefault(0);
        }
    }
}
