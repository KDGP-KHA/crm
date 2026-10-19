using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Cate.Biz;
using Core.Cate.Models;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Models.Caching;
using System.ComponentModel;
using TSFramework.Libs.Utils;


namespace Core.Cate.Caches
{
    public class OpportunityProjectWithoutPGPCache : CacheLayer
    {
        private OpportunityProjectWithoutPGPBiz _OpportunityProjectWithoutPGPApi;
        protected override string[] MasterCacheKeyArray => new[] { "OpportunityProjectWithoutPGPCache", "RM_DigitalSalesCache", "CENIT.APP.Cache" };
        private OpportunityProjectWithoutPGPBiz Api => _OpportunityProjectWithoutPGPApi ?? (_OpportunityProjectWithoutPGPApi = new OpportunityProjectWithoutPGPBiz());

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<OpportunityProjectWithoutPGPModel> Get(out int total, OpportunityProjectWithoutPGSearchPModel searchModel = null, BaseSearchModel search = null)
        {
            total = 0;
            return Api.LoadList(out total, searchModel, search);
        }
    }
}
