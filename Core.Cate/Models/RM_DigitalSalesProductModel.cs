using System;
using System.Collections.Generic;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesProductModel : BaseModel
    {
        public int SalesProductID { get; set; }
        public int DigitalSalesID { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSalesProduct_ProductService_Label")]
        public int ProductServiceID { get; set; }
        public string ProductServiceName { get; set; }
        public string ProductServiceCode { get; set; }

        [CustomDisplayName("DigitalSalesProduct_ExpectedRevenue_Label")]
        public decimal? ExpectedRevenue { get; set; }

        [CustomDisplayName("DigitalSalesProduct_ExpectedRevenueMillion_Label")]
        public decimal? ExpectedRevenueMillion
        {
            get { return ExpectedRevenue.HasValue ? ExpectedRevenue.Value / 1000000m : (decimal?)null; }
            set { ExpectedRevenue = value.HasValue ? value.Value * 1000000m : (decimal?)null; }
        }

        [CustomDisplayName("DigitalSalesProduct_ActualRevenue_Label")]
        public decimal? ActualRevenue { get; set; }

        [CustomDisplayName("DigitalSalesProduct_ActualRevenueMillion_Label")]
        public decimal ActualRevenueMillion
        {
            get { return (ActualRevenue ?? 0m) / 1000000m; }
        }

        [CustomDisplayName("DigitalSalesProduct_PackageName_Label")]
        public string PackageName { get; set; }

        [CustomDisplayName("DigitalSalesProduct_Quantity_Label")]
        public int Quantity { get; set; } = 1;

        [CustomDisplayName("DigitalSalesProduct_StartDate_Label")]
        public DateTime? StartDate { get; set; }

        [CustomDisplayName("DigitalSalesProduct_EndDate_Label")]
        public DateTime? EndDate { get; set; }

        [CustomDisplayName("DigitalSalesProduct_Note_Label")]
        public string Note { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        public decimal TotalCostMillion { get; set; }
        public decimal ProfitMillion
        {
            get { return ActualRevenueMillion - TotalCostMillion; }
        }
        public int MemberCount { get; set; }
        public int RevenueCount { get; set; }
        public decimal ContractRevenueMillion { get; set; }
        public int ContractCount { get; set; }
        public List<RM_ContractsModel> Contracts { get; set; } = new List<RM_ContractsModel>();

        public List<RM_DigitalSalesProductCostModel> Costs { get; set; } = new List<RM_DigitalSalesProductCostModel>();
        public List<RM_DigitalSalesProductRevenueModel> Revenues { get; set; } = new List<RM_DigitalSalesProductRevenueModel>();
        public List<RM_DigitalSalesProductMemberModel> ProductMembers { get; set; } = new List<RM_DigitalSalesProductMemberModel>();
    }

    public class RM_DigitalSalesProductCostModel : BaseModel
    {
        public int SalesProductCostID { get; set; }
        public int SalesProductID { get; set; }

        [CustomRequired]
        [CustomDisplayName("CostType_Title")]
        public int CostTypeID { get; set; }

        public string CostTypeName { get; set; }

        [CustomRequired]
        [CustomDisplayName("ProductCost_Amount_Label")]
        public decimal? Amount { get; set; }

        [CustomDisplayName("ProductCost_PaymentDate_Label")]
        public DateTime? PaymentDate { get; set; }

        [CustomDisplayName("ProductCost_Note_Label")]
        public string Note { get; set; }
    }

    public class RM_DigitalSalesProductRevenueModel : BaseModel
    {
        public int SalesProductRevenueID { get; set; }
        public int SalesProductID { get; set; }

        [CustomRequired]
        [CustomDisplayName("RevenueReceived_Amount_Label")]
        public decimal? Amount { get; set; }

        [CustomRequired]
        [CustomDisplayName("RevenueReceived_ReceivedDate_Label")]
        public DateTime? ReceivedDate { get; set; }

        [CustomDisplayName("RevenueReceived_ReceivedTime_Label")]
        public DateTime? ReceivedTime { get; set; }

        [CustomDisplayName("RevenueReceived_Note_Label")]
        public string Note { get; set; }
    }

    public class RM_DigitalSalesProductMemberModel : BaseModel
    {
        public int SalesProductMemberID { get; set; }
        public int SalesProductID { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSalesProductMember_Employee_Label")]
        public int EmployeeID { get; set; }

        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSalesProductMember_Roles_Label")]
        public int[] RoleIDs { get; set; }

        public string RoleIDsText { get; set; }
        public string RoleNames { get; set; }
    }
}
