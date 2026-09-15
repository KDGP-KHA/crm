using System.Collections.Generic;
using System.ComponentModel;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Models.Caching;
using Core.Cate.Biz;
using TSFramework.Libs.Utils;

namespace Core.Cate.Caches
{
    public class RM_ReviewBatchItemCache : CacheLayer
    {
        private RM_ReviewBatchItemBiz _RM_ReviewBatchItemApi;
        protected override string[] MasterCacheKeyArray => new[] { "RM_ReviewBatchItemCache", "CENIT.APP.Cache" };

        private RM_ReviewBatchItemBiz Api => _RM_ReviewBatchItemApi ?? (_RM_ReviewBatchItemApi = new RM_ReviewBatchItemBiz());

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewDigitalSalesModel> LoadDigitalSales(out int total, RM_ReviewDigitalSalesSearchModel model, BaseSearchModel search = null)
        {
            var rawKey = string.Concat("GetSearch_RM_ReviewDigitalSales", UtilEncrypt.FromObject(model), UtilEncrypt.FromObject(search));
            var rawKeyTotal = string.Concat(rawKey, "-Total");
            total = (int?)GetCacheItem(rawKeyTotal) ?? 0;
            var data = GetCacheItem(rawKey) as List<RM_ReviewDigitalSalesModel>;
            if (data != null) return data;
            data = Api.LoadDigitalSales(out total, model, search);
            AddCacheItem(rawKey, data);
            AddCacheItem(rawKeyTotal, total);
            return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewProjectModel> GetAllProject()
        {
            var rawKey = string.Concat("GetAllRM_ReviewProject");
            var data = GetCacheItem(rawKey) as List<RM_ReviewProjectModel>;
            if (data != null) return data;
            data = Api.GetAllProject();
            AddCacheItem(rawKey, data); return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewProjectModel> LoadProject(out int total, RM_ReviewProjectSearchModel model, BaseSearchModel search = null)
        {
            var rawKey = string.Concat("GetSearch_RM_ReviewProject", UtilEncrypt.FromObject(model), UtilEncrypt.FromObject(search));
            var rawKeyTotal = string.Concat(rawKey, "-Total");
            total = 0;
            var cacheTotal = (int?)GetCacheItem(rawKeyTotal); total = cacheTotal ?? 0;
            var data = GetCacheItem(rawKey) as List<RM_ReviewProjectModel>;
            if (data != null) return data;
            data = Api.LoadProject(out total, model, search);
            AddCacheItem(rawKey, data); AddCacheItem(rawKeyTotal, total); return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewBusinessOpportunityModel> GetAllBusinessOpportunity()
        {
            var rawKey = string.Concat("GetAllRM_ReviewBusinessOpportunity");
            var data = GetCacheItem(rawKey) as List<RM_ReviewBusinessOpportunityModel>;
            if (data != null) return data;
            data = Api.GetAllBusinessOpportunity();
            AddCacheItem(rawKey, data); return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RM_ReviewBatchItem
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewBusinessOpportunityModel> LoadBusinessOpportunity(out int total, RM_ReviewBusinessOpportunitySearchModel model, BaseSearchModel search = null)
        {
            var rawKey = string.Concat("GetSearch_RM_ReviewBusinessOpportunity", UtilEncrypt.FromObject(model), UtilEncrypt.FromObject(search));
            var rawKeyTotal = string.Concat(rawKey, "-Total");
            total = 0;
            var cacheTotal = (int?)GetCacheItem(rawKeyTotal); total = cacheTotal ?? 0;
            var data = GetCacheItem(rawKey) as List<RM_ReviewBusinessOpportunityModel>;
            if (data != null) return data;
            data = Api.LoadBusinessOpportunity(out total, model, search);
            AddCacheItem(rawKey, data); AddCacheItem(rawKeyTotal, total); return data;
        }

        /// <summary>
        /// Lưu thông tin rà soát.
        /// </summary>
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(RM_ReviewFormModel model, string username)
        {
            var isSaved = Api.Save(model, username);
            if (isSaved > 0) InvalidateCache();
            return isSaved;
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử rà soát
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewHistoryModel> GetHistory(int objectType, int objectID)
        {
            var rawKey = string.Concat("RM_ReviewBatch_GetHistory", objectType, objectID);
            var data = GetCacheItem(rawKey) as List<RM_ReviewHistoryModel>;
            if (data != null) return data;
            data = Api.GetHistory(objectType, objectID);
            AddCacheItem(rawKey, data); return data;
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_ReviewHistoryModel> GetDigitalSalesHistory(int digitalSalesID)
        {
            var rawKey = string.Concat("RM_DigitalSalesReview_GetHistory", digitalSalesID);
            var data = GetCacheItem(rawKey) as List<RM_ReviewHistoryModel>;
            if (data != null) return data;
            data = Api.GetDigitalSalesHistory(digitalSalesID);
            AddCacheItem(rawKey, data);
            return data;
        }

        /// <summary>
        /// Cập nhật lịch sử rà soát.
        /// </summary>
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int SaveHistory(RM_ReviewFormModel model, string username)
        {
            var isSaved = Api.SaveHistory(model, username);
            if (isSaved > 0) InvalidateCache();
            return isSaved;
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public RM_ReviewHistoryModel GetHistoryById(int id)
        {
            var key = $"Get_ReviewHistoryByID_{id}";
            var data = GetCacheItem(key) as RM_ReviewHistoryModel;
            if (data != null) return data;
            data = Api.LoadDetailHistory(id);
            AddCacheItem(key, data);
            return data;
        }
    }
}
