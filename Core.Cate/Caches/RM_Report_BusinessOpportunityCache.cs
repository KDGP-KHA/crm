using Core.Cate.Biz;
using Core.Cate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Models.Caching;
using TSFramework.Libs.Utils;

namespace Core.Cate.Caches
{
    public class RM_Report_BusinessOpportunityCache : CacheLayer
    {
        private RM_Report_BusinessOpportunityBiz _RM_AnniversaryTypeApi;
        protected override string[] MasterCacheKeyArray => new[] { "RM_BusinessOpportunity_ReportCache", "RM_DigitalSalesCache", "CENIT.APP.Cache" };

        private RM_Report_BusinessOpportunityBiz Api => _RM_AnniversaryTypeApi ?? (_RM_AnniversaryTypeApi = new RM_Report_BusinessOpportunityBiz());

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_Report_BusinessOpportunityModel> Get(out int total, RM_Report_BusinessOpportunitySearchModel model, BaseSearchModel search = null)
        {
            total = 0;
            return Api.LoadList(out total, model, search);
        }

        [DataObjectMethod(DataObjectMethodType.Select, true)]
        public List<RM_Report_BusinessOpportunityModel> GetAll(RM_Report_BusinessOpportunitySearchModel model)
        {
            return Api.GetAll(model);
        }
    }
}
