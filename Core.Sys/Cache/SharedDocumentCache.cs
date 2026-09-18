using System;
using System.Collections.Generic;
using System.ComponentModel;
using Core.Sys.BLL;
using Core.Sys.Models;
using TSFramework.Libs.Models.Caching;

namespace Core.Sys.Cache
{
    [DataObject]
    public class SharedDocumentCache : CacheLayer
    {
        private SharedDocumentBiz _api;
        private SharedDocumentBiz Api => _api ?? (_api = new SharedDocumentBiz());

        protected override string[] MasterCacheKeyArray => new[]
        {
            "SharedDocumentsCache", "SharedDocumentCategoriesCache", "CENIT.APP.Cache"
        };

        #region Categories

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<SharedDocumentCategoryModel> GetCategories(bool? isActive = true)
        {
            var rawKey = $"SharedDocCategories-{(isActive.HasValue ? isActive.Value.ToString() : "all")}";
            if (GetCacheItem(rawKey) is List<SharedDocumentCategoryModel> cached)
            {
                return cached;
            }

            var list = Api.GetCategories(isActive);
            if (list != null)
            {
                AddCacheItem(rawKey, list);
            }
            return list;
        }

        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public SharedDocumentCategoryModel GetCategoryById(int categoryId)
        {
            if (categoryId <= 0) return null;
            var rawKey = $"SharedDocCategory-{categoryId}";
            if (GetCacheItem(rawKey) is SharedDocumentCategoryModel cached)
            {
                return cached;
            }

            var item = Api.GetCategoryById(categoryId);
            if (item != null)
            {
                AddCacheItem(rawKey, item);
            }
            return item;
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int SaveCategory(SharedDocumentCategoryModel model, string userName)
        {
            var result = Api.SaveCategory(model, userName);
            if (result > 0)
            {
                InvalidateCache();
            }
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Delete, true)]
        public int DeleteCategory(int categoryId, string userName)
        {
            var result = Api.DeleteCategory(categoryId, userName);
            if (result > 0)
            {
                InvalidateCache();
            }
            return result;
        }

        #endregion

        #region Shared Documents

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<SharedDocumentModel> GetList(out int total, SharedDocumentSearchModel search)
        {
            search = search ?? new SharedDocumentSearchModel();
            return GetList(out total, search.Keyword, search.CategoryId, search.FromDate, search.ToDate, search.PageNumber, search.PageSize);
        }

        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public List<SharedDocumentModel> GetList(out int total, string keyword, int? categoryId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize)
        {
            var rawKey = string.Format("SharedDocList-{0}-{1}-{2:yyyyMMdd}-{3:yyyyMMdd}-{4}-{5}",
                keyword ?? string.Empty,
                categoryId ?? 0,
                fromDate,
                toDate,
                pageNumber,
                pageSize);
            var rawKeyTotal = rawKey + "-Total";

            var cacheTotal = (int?)GetCacheItem(rawKeyTotal);
            total = cacheTotal ?? 0;

            if (GetCacheItem(rawKey) is List<SharedDocumentModel> cachedList)
            {
                return cachedList;
            }

            var list = Api.GetList(out total, keyword, categoryId, fromDate, toDate, pageNumber, pageSize);
            if (list != null)
            {
                AddCacheItem(rawKey, list);
                AddCacheItem(rawKeyTotal, total);
            }
            return list;
        }

        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public SharedDocumentModel GetById(int documentId)
        {
            if (documentId <= 0) return null;
            var rawKey = $"SharedDocById-{documentId}";
            if (GetCacheItem(rawKey) is SharedDocumentModel cached)
            {
                return cached;
            }

            var item = Api.GetById(documentId);
            if (item != null)
            {
                AddCacheItem(rawKey, item);
            }
            return item;
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(SharedDocumentModel model, string userName)
        {
            var result = Api.Save(model, userName);
            if (result > 0)
            {
                InvalidateCache();
            }
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Delete, true)]
        public int Delete(int documentId, string userName)
        {
            var result = Api.Delete(documentId, userName);
            if (result > 0)
            {
                InvalidateCache();
            }
            return result;
        }

        public int TrackDownload(int documentId, string downloadedBy)
        {
            return Api.TrackDownload(documentId, downloadedBy);
        }

        #endregion
    }
}
