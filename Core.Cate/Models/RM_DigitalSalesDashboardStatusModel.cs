using System;
using System.Collections.Generic;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesDashboardStatusModel : BaseModel
    {
        public int StatusID { get; set; }
        public string StatusCode { get; set; }
        public string StatusName { get; set; }
        public byte BusinessType { get; set; } // 1: Cơ hội, 2: Dự án
        public int SortOrder { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public decimal TotalActualRevenue { get; set; }
    }

    public class DigitalSalesDashboardOverviewModel : BaseModel
    {
        public int ApplyYear { get; set; }
        public List<RM_DigitalSalesDashboardStatusModel> StatusList { get; set; } = new List<RM_DigitalSalesDashboardStatusModel>();
        public int TotalCountAll { get; set; }
        public decimal TotalExpectedRevenueAll { get; set; }
        public decimal TotalActualRevenueAll { get; set; }
        public List<RM_DigitalSalesModel> KeyProjects { get; set; } = new List<RM_DigitalSalesModel>();
        public List<RM_DigitalSalesModel> FollowedOpportunities { get; set; } = new List<RM_DigitalSalesModel>();
        public List<RM_DigitalSalesModel> StaleActionTimeSales { get; set; } = new List<RM_DigitalSalesModel>();
    }
}
