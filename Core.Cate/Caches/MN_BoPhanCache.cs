using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Web;
using Core.Cate.Models;
using Core.Cate.Biz;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Models.Caching;
using TSFramework.Libs.Utils;

namespace Core.Cate.Caches
{
    public class MN_BoPhanCache : CacheLayer
    {
        private MN_BoPhanBiz _MN_BoPhanApi;
        protected override string[] MasterCacheKeyArray => new[] { "MN_BoPhanCache", "CENIT.APP.Cache" };

        private MN_BoPhanBiz Api => _MN_BoPhanApi ?? (_MN_BoPhanApi = new MN_BoPhanBiz());
        /// <summary>
        /// Xóa theo ID
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Delete, false)]
        public int Delete(MN_BoPhanModel model, string deletedBy)
        {
            var isDeleted = Api.Delete(model.BoPhan_ID, deletedBy);
            if (isDeleted > 0) InvalidateCache();
            return isDeleted;
        }

        /// <summary>
        /// Lưu thông tin MN_BoPhan
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(MN_BoPhanModel model, string savedBy)
        {
            var isSaved = Api.Save(model, savedBy);
            if (isSaved > 0) InvalidateCache();
            return isSaved;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách MN_BoPhan
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<MN_BoPhanModel> GetAll()
        {
            var rawKey = string.Concat("GetAllMN_BoPhan");
            var data = GetCacheItem(rawKey) as List<MN_BoPhanModel>;
            if (data != null && data.Count() > 0) return data;
            data = Api.GetAll();
            AddCacheItem(rawKey, data); return data;
        }

        /// <summary>
        /// Lấy thông tinMN_BoPhan theo ID
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public MN_BoPhanModel GetById(int ID)
        {
            if (ID < 0) return null;
            var rawKey = string.Concat("GetMN_BoPhanByID_", ID);
            var data = GetCacheItem(rawKey) as MN_BoPhanModel;
            if (data != null) return data;
            data = Api.LoadDetail(ID);
            AddCacheItem(rawKey, data); return data;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách MN_BoPhan
        /// </summary>
        /// <returns>Kết quả thực hiện</returns>
        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<MN_BoPhanModel> Get(string search = "")
        {
            var rawKey = string.Concat("GetSearch_MN_BoPhan", UtilEncrypt.FromObject(search));
            var data = GetCacheItem(rawKey) as List<MN_BoPhanModel>;
            if (data != null) return data;

            data = Api.LoadList(search);

            AddCacheItem(rawKey, data);

            return data;
        }

        public List<MN_BoPhanExportModel> Export(MN_BoPhanSearchModel search)
        {
            var rawKey = string.Concat("ExportSearch_MN_BoPhan", UtilEncrypt.FromObject(search));
            var data = GetCacheItem(rawKey) as List<MN_BoPhanExportModel>;
            if (data != null) return data;

            data = Api.Export(search);

            AddCacheItem(rawKey, data);

            return data;
        }
    }
}
