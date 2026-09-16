using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_ContractsModel : BaseModel
    {
        public int ContractID { get; set; }
        public int ProductProjectID { get; set; }
        /// <summary>DigitalSales product relation; null for existing project contracts.</summary>
        public int? DigitalSalesProductID { get; set; }
        public int ProjectID { get; set; }
        [CustomRequired]
        [CustomDisplayName("ContractCode_Label_Name")]
        public string ContractCode { get; set; }
        [CustomRequired]
        [CustomDisplayName("ContractName_Label")]
        public string ContractName { get; set; }
        [CustomDisplayName("Customer_CustomerName_Label")]
        public int CustomerID { get; set; }

        [CustomDisplayName("Customer_CustomerName_Label")]
        public string CustomerName { get; set; }

        [CustomDisplayName("SignDate_Label")]
        public DateTime? SignDate { get; set; }

        [CustomDisplayName("Start_Date")]
        public DateTime? StartDate { get; set; }

        [CustomDisplayName("End_Date")]
        public DateTime? EndDate { get; set; }

        [CustomDisplayName("ContractValue_Label")]
        public double ContractValue { get; set; }

        [CustomDisplayName("VAT_label")]
        public double VAT { get; set; }

        [CustomDisplayName("TotalAmount_Label")]
        public double TotalAmount { get; set; }
        [CustomRequired]

        [CustomDisplayName("Customer_Status_Label")]
        public int StatusContract { get; set; }
        public string StatusName { get; set; }
        public string StatusClass { get; set; }
        public string UserCreated { get; set; }
        [CustomDisplayName("ProductService_Title")]
        public string NameProduct { get; set; }

        [CustomRequired]
        [CustomDisplayName("BillingCycles_Title")]
        public int BillingCycleID { get; set; }

        [CustomRequired]
        [CustomDisplayName("ReminderType_Label")]
        public string ReminderType { get; set; }

        [CustomRequired]
        [CustomDisplayName("ReminderDayOfMonth_Label")]
        public int ReminderDayOfMonth { get; set; }
        public string CycleName { get; set; }
        public List<HttpPostedFileBase> DinhKemFile { get; set; }
        public List<RM_ContractFilePathModel> ExistingFiles { get; set; }
        public List<int> DeletedFileIds { get; set; }
        public List<RM_CustomerModel> Customers { get; set; }
        public List<SelectListItem> ListCustomer { get; set; }
        public List<SelectListItem> ListStatus { get; set; }
        public List<SelectListItem> ListBillingCycles { get; set; }
        public List<RM_ContractRemindersModel> ListContractReminders { get; set; } = new List<RM_ContractRemindersModel>();
    }

    public class RM_ContractFilePathModel : BaseModel
    {
        public int FilePathID { get; set; }
        public int ContractID { get; set; }
        public string FilePath { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class RM_ContractsSearchModel : BaseModel
    {
        public string Keyword { get; set; }
        public int CustomerTypeID { get; set; }
        public int StatusID { get; set; }
        public List<RM_StatusModel> Status { get; set; }
        public List<RM_CustomerTypeModel> CustomerTypes { get; set; }
        public string UserName { get; set; }
    }
}
