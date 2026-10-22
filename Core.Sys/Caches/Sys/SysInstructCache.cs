using Core.Sys.Biz.Sys;
using Core.Sys.Models.Sys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSFramework.Libs.Models.Caching;

namespace Core.Sys.Caches.Sys
{
    public class SysInstructCache : CacheLayer
    {
        private SysInstructBiz _instructApi;
        private SysInstructBiz Api => _instructApi ?? (_instructApi = new SysInstructBiz());
        protected override string[] MasterCacheKeyArray => new[] { "SysInstructCache", "CENIT.APP.Cache" };

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<SysInstructModel> Get(out int total, TSFramework.Libs.Models.Base.BaseSearchModel search = null)
        {
            total = 0;
            var instructs = Api.Get(out total, search);
            return instructs;
        }

        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public SysInstructModel GetById(int instructId)
        {
            if (instructId < 0) return null;
            return Api.LoadDetail(instructId);
        }

        [DataObjectMethod(DataObjectMethodType.Select, false)]
        public List<SysInstructModel> GetAll()
        {
            return Api.Get(out _, null);
        }

        [DataObjectMethod(DataObjectMethodType.Delete, false)]
        public int Delete(SysInstructModel model, string username)
        {
            var result = Api.Delete(model, username);
            if (result > 0) InvalidateCache();
            return result;
        }

        [DataObjectMethod(DataObjectMethodType.Update, false)]
        public int Save(SysInstructModel model, string username)
        {
            var instructId = Api.Save(model, username);
            // Invalidate the cache
            InvalidateCache();
            return instructId;
        }
    }
}
