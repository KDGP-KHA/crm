using Core.Cate.Biz;
using Core.Cate.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSFramework.Libs.Models.Caching;
using TSFramework.Libs.Utils;

namespace Core.Cate.Caches
{
    public class RM_DigitalSalesCache : CacheLayer
    {
        private RM_DigitalSalesBiz _api;
        protected override string[] MasterCacheKeyArray => new[] { "RM_DigitalSalesCache", "CENIT.APP.Cache" };
        private RM_DigitalSalesBiz Api => _api ?? (_api = new RM_DigitalSalesBiz());

        private string BuildSearchCacheKey(RM_DigitalSalesSearchModel model)
        {
            if (model == null) return "RM_DigitalSales_GetList_Default";
            return string.Concat("RM_DigitalSales_GetList_", model.Keyword, "_", model.BusinessType, "_", model.StatusID, "_", model.StatusIDs, "_", model.CustomerID, "_", model.ProductServiceID, "_", model.DepartmentID, "_", model.EmployeeID, "_", model.FromDate, "_", model.ToDate, "_", model.PageNumber, "_", model.PageSize, "_", model.UserName, "_", model.IsKeyProject, "_", model.IsFollowed, "_", model.ApplyYear);
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_DigitalSalesModel> LoadList(out int total, RM_DigitalSalesSearchModel model)
        {
            var rawKey = BuildSearchCacheKey(model);
            var rawKeyTotal = string.Concat(rawKey, "_Total");
            total = 0;
            var cacheTotal = (int?)GetCacheItem(rawKeyTotal);
            total = cacheTotal ?? 0;
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesModel>;
            if (data != null) return data;

            data = Api.LoadList(out total, model);
            AddCacheItem(rawKey, data);
            AddCacheItem(rawKeyTotal, total);
            return data;
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public RM_DigitalSalesModel GetByID(int id)
        {
            return GetByID(id, null);
        }

        public RM_DigitalSalesModel GetByID(int id, string userName)
        {
            if (id <= 0) return null;
            var rawKey = string.IsNullOrEmpty(userName)
                ? string.Concat("RM_DigitalSales_GetByID_", id)
                : string.Concat("RM_DigitalSales_GetByID_", id, "_", userName);
            var data = GetCacheItem(rawKey) as RM_DigitalSalesModel;
            if (data != null) return data;

            data = Api.GetByID(id, userName);
            AddCacheItem(rawKey, data);
            return data;
        }

        public bool ToggleKeyProject(int id, bool isKeyProject, string username)
        {
            var result = Api.ToggleKeyProject(id, isKeyProject, username);
            if (result) InvalidateCache();
            return result;
        }

        public bool ToggleFollow(int id, bool isFollowed, string username)
        {
            var result = Api.ToggleFollow(id, isFollowed, username);
            if (result) InvalidateCache();
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(RM_DigitalSalesModel model, string username)
        {
            var result = Api.Save(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Delete, true)]
        public int Delete(int digitalSalesId, string username)
        {
            var result = Api.Delete(digitalSalesId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int ChangeStatus(int digitalSalesId, int newStatusId, string note, string attachmentPath, string username)
        {
            var result = Api.ChangeStatus(digitalSalesId, newStatusId, note, attachmentPath, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public List<RM_DigitalSalesProductModel> GetProductsBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesProductModel>();
            var rawKey = string.Concat("RM_DigitalSalesProduct_GetBySalesID_", digitalSalesId);
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesProductModel>;
            if (data != null) return data;

            data = Api.GetProductsBySalesID(digitalSalesId);
            AddCacheItem(rawKey, data);
            return data;
        }

        public List<RM_ContractsModel> GetProductContracts(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_ContractsModel>();
            return Api.GetProductContracts(salesProductId);
        }

        public int LinkProductContract(int salesProductId, int contractId, string username)
        {
            var result = Api.LinkProductContract(salesProductId, contractId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int SaveProduct(RM_DigitalSalesProductModel model, string username)
        {
            var result = Api.SaveProduct(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public List<RM_DigitalSalesProductCostModel> GetProductCosts(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductCostModel>();
            return Api.GetProductCosts(salesProductId);
        }

        public List<RM_DigitalSalesProductRevenueModel> GetProductRevenues(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductRevenueModel>();
            return Api.GetProductRevenues(salesProductId);
        }

        public List<RM_DigitalSalesProductMemberModel> GetProductMembers(int salesProductId)
        {
            if (salesProductId <= 0) return new List<RM_DigitalSalesProductMemberModel>();
            return Api.GetProductMembers(salesProductId);
        }

        public int SaveProductDetail(RM_DigitalSalesProductModel model, string username)
        {
            var result = Api.SaveProductDetail(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int DeleteProduct(int salesProductId, string username)
        {
            var result = Api.DeleteProduct(salesProductId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public List<RM_DigitalSalesTrackingModel> GetTrackingTasks(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTrackingModel>();
            var rawKey = string.Concat("RM_DigitalSalesTracking_GetBySalesID_", digitalSalesId);
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesTrackingModel>;
            if (data != null) return data;

            data = Api.GetTrackingTasks(digitalSalesId);
            AddCacheItem(rawKey, data);
            return data;
        }

        public int UpdateTrackingStatus(int trackingId, byte status, string resultNote, string attachmentFile, int? assignedUserId, DateTime? deadline, string username)
        {
            var result = Api.UpdateTrackingStatus(trackingId, status, resultNote, attachmentFile, assignedUserId, deadline, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int SaveTracking(RM_DigitalSalesTrackingModel model, string username)
        {
            var result = Api.SaveTracking(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int DeleteTracking(int trackingId, string username)
        {
            var result = Api.DeleteTracking(trackingId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int ChangeProcessOfStatus(int digitalSalesId, int statusId, int newProcessId, string username)
        {
            var result = Api.ChangeProcessOfStatus(digitalSalesId, statusId, newProcessId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public List<RM_DigitalSalesMemberModel> GetMembersBySalesID(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesMemberModel>();
            var rawKey = string.Concat("RM_DigitalSalesMember_GetBySalesID_", digitalSalesId);
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesMemberModel>;
            if (data != null) return data;

            data = Api.GetMembersBySalesID(digitalSalesId);
            AddCacheItem(rawKey, data);
            return data;
        }

        public int SaveMember(RM_DigitalSalesMemberModel model, string username)
        {
            var result = Api.SaveMember(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int DeleteMember(int memberId, string username)
        {
            var result = Api.DeleteMember(memberId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public List<RM_DigitalSalesTimelineModel> GetTimeline(int digitalSalesId)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesTimelineModel>();
            var rawKey = string.Concat("RM_DigitalSales_GetTimeline_", digitalSalesId);
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesTimelineModel>;
            if (data != null) return data;

            data = Api.GetTimeline(digitalSalesId);
            AddCacheItem(rawKey, data);
            return data;
        }

        public List<RM_DigitalSalesStatusModel> GetStatusList(byte? businessType = null)
        {
            var rawKey = string.Concat("RM_DigitalSalesStatus_GetAll_", businessType);
            var data = GetCacheItem(rawKey) as List<RM_DigitalSalesStatusModel>;
            if (data != null) return data;

            data = Api.GetStatusList(businessType);
            AddCacheItem(rawKey, data);
            return data;
        }

        public string GenerateNextCode()
        {
            return Api.GenerateNextCode();
        }

        public List<RM_DigitalSalesActivityModel> GetActivitiesBySalesID(int digitalSalesId, byte? activityType = null)
        {
            if (digitalSalesId <= 0) return new List<RM_DigitalSalesActivityModel>();
            return Api.GetActivitiesBySalesID(digitalSalesId, activityType);
        }

        public List<RM_DigitalSalesActivityModel> GetActivitiesByTrackingID(int digitalSalesId, int trackingId)
        {
            if (digitalSalesId <= 0 || trackingId <= 0) return new List<RM_DigitalSalesActivityModel>();
            return Api.GetActivitiesByTrackingID(digitalSalesId, trackingId);
        }

        public int AddActivity(RM_DigitalSalesActivityModel model, string username)
        {
            var result = Api.AddActivity(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int DeleteActivity(int activityId, string username)
        {
            var result = Api.DeleteActivity(activityId, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int UpdateLatestStatusChangeActivityAttachments(int digitalSalesId, string attachmentsJson, string username)
        {
            var result = Api.UpdateLatestStatusChangeActivityAttachments(digitalSalesId, attachmentsJson, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        public int? GetDepartmentByUserID(int userId)
        {
            if (userId <= 0) return null;
            var rawKey = string.Concat("RM_DigitalSales_GetDepartmentByUserID_", userId);
            var cached = GetCacheItem(rawKey);
            if (cached != null) return (int)cached;

            var data = Api.GetDepartmentByUserID(userId);
            if (data.HasValue)
            {
                AddCacheItem(rawKey, data.Value);
            }
            return data;
        }

        public List<RM_DigitalSalesUserModel> GetAccessibleEmployees(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return new List<RM_DigitalSalesUserModel>();
            var rawKey = string.Concat("RM_DigitalSales_GetAccessibleEmployees_", userName.ToLower().Trim());
            if (GetCacheItem(rawKey) is List<RM_DigitalSalesUserModel> cached) return cached;

            var data = Api.GetAccessibleEmployees(userName);
            if (data != null)
            {
                AddCacheItem(rawKey, data);
            }
            return data;
        }

        public DigitalSalesDashboardOverviewModel GetDashboardStatusStats(int applyYear, string userName = null)
        {
            if (applyYear <= 0) applyYear = DateTime.Now.Year;
            var rawKey = string.Concat("RM_DigitalSales_GetDashboardStatusStats_", applyYear, "_", userName ?? "ALL");
            var data = GetCacheItem(rawKey) as DigitalSalesDashboardOverviewModel;
            if (data != null) return data;

            data = Api.GetDashboardStatusStats(applyYear, userName);
            if (data != null)
            {
                AddCacheItem(rawKey, data);
            }
            return data;
        }
    }
}

