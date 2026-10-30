using System.Collections.Generic;
using System.ComponentModel;
using Core.Sys.Biz.Sys;
using Core.Cate.Models;
using TSFramework.Libs.Models.Caching;
using Core.Sys.Models.Sys;

namespace Core.Sys.Caches.Sys
{
    [DataObject]
    public class SysUserBoPhanCache : CacheLayer
    {
        private SysUserBoPhanBiz _api;

        protected override string[] MasterCacheKeyArray => new[]
        {
            "SysUserBoPhanCache", "CENIT.APP.Cache"
        };

        private SysUserBoPhanBiz Api => _api ?? (_api = new SysUserBoPhanBiz());

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(string email, string boPhanIDs, string maBophans, string savedBy)
        {
            var result = Api.Save(email, boPhanIDs, maBophans, savedBy);
            if (result > 0) InvalidateCache();
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int Save(string email, string maBophans, string savedBy)
        {
            return Save(email, null, maBophans, savedBy);
        }

        [DataObjectMethod(DataObjectMethodType.Insert, true)]
        public int PermitReview(UserPermitReviewModel model, string savedBy)
        {
            var result = Api.PermitReview(model, savedBy);
            if (result > 0) InvalidateCache();
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<MN_BoPhanModel> GetByEmail(string email)
        {
            return Api.GetByEmail(email);
        }
        public List<MN_BoPhanModel> GetAll()
        {
            var rawKey = "UserBoPhan-GetAll";
            if (GetCacheItem(rawKey) is List<MN_BoPhanModel> cached) return cached;
            var data = Api.GetAll();
            if (data != null) AddCacheItem(rawKey, data);
            return data;
        }
    }
}
