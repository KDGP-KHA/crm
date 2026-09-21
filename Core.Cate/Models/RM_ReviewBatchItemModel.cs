using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Processors;

namespace Core.Cate.Models
{
    public class RM_ReviewBatchItemModel : BaseModel
    {
        public List<RM_ReviewProjectModel> ListProject { get; set; }
        public List<RM_ReviewBusinessOpportunityModel> ListBusinessOpportunity { get; set; }
    }

    public class RM_ReviewBatchItemSearchModel : BaseModel
    {
        public RM_ReviewDigitalSalesSearchModel DigitalSalesSearch { get; set; }
        public RM_ReviewProjectSearchModel ProjectSearch { get; set; }
        public RM_ReviewBusinessOpportunitySearchModel BusinessOpportunitySearch { get; set; }
    }

    public class RM_ReviewDigitalSalesSearchModel : BaseModel
    {
        [CustomDisplayName("Label_TuKhoa")]
        public string Keyword { get; set; }

        [CustomDisplayName("ReviewBatch_Title")]
        public int ReviewBatchID { get; set; }

        [CustomDisplayName("ReviewDigitalSales_BusinessType_Label")]
        public byte BusinessType { get; set; }

        [CustomDisplayName("ReviewDigitalSales_Status_Label")]
        public int StatusID { get; set; }

        [CustomDisplayName("Department_Search_Label")]
        public int DepartmentID { get; set; }

        [CustomDisplayName("Employee_Search_Label")]
        public int EmployeeID { get; set; }

        [CustomDisplayName("ReviewBatch_IsReviewed_Label")]
        public bool IsReviewed { get; set; }

        [CustomDisplayName("DigitalSalesTracking_Process_Label")]
        public int ProcessID { get; set; }

        [CustomDisplayName("DigitalSalesTracking_Progress_Label")]
        public int ProgressID { get; set; }

        public string UserName { get; set; }
        public List<RM_ReviewBatchModel> ReviewBatches { get; set; } = new List<RM_ReviewBatchModel>();
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ProcessOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ProgressOptions { get; set; } = new List<SelectListItem>();
        public List<MN_BoPhanModel> Departments { get; set; } = new List<MN_BoPhanModel>();
    }

    public class RM_ReviewDigitalSalesModel : BaseModel
    {
        public int DigitalSalesID { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public byte BusinessType { get; set; }
        public string BusinessTypeName { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public bool IsKeyProject { get; set; }
        public bool IsFollowed { get; set; }
        public string ProductServiceNames { get; set; }
        public string CustomerName { get; set; }
        public string AssignedEmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public decimal? TotalExpectedRevenue { get; set; }
        public int ReviewBatchItemID { get; set; }
        public int HighestReviewedLevel { get; set; }
        public DateTime? LastReviewedDate { get; set; }
        public bool IsReviewed { get; set; }
        public DateTime? LastReviewDate { get; set; }
        public string LastReviewerName { get; set; }
        public string LastReviewComment { get; set; }
        public byte? FinalReviewConclusion { get; set; }
        public int ReviewCount { get; set; }
    }

    public class RM_ReviewProjectSearchModel
    {
        public string Keyword { get; set; }
        public int ReviewBatchID { get; set; }
        public int UserLevel { get; set; }
        public int? Status { get; set; }
        public int BoPhanID { get; set; }
        public int EmployeeID { get; set; }
        public bool IsReviewed { get; set; } = false;
        public string UserName { get; set; }
        public List<RM_ReviewBatchModel> ListReviewPatch { get; set; }
        public List<RM_StatusModel> ListStatus { get; set; }
        public List<MN_BoPhanModel> Departments { get; set; }
    }

    public class RM_ReviewBusinessOpportunitySearchModel
    {
        public string Keyword { get; set; }
        public int ReviewBatchID { get; set; }
        public int UserLevel { get; set; }
        public int? StatusID { get; set; }
        public int BoPhanID { get; set; }
        public int EmployeeID { get; set; }
        public bool IsReviewed { get; set; } = false;
        public string UserName { get; set; }
        public List<RM_ReviewBatchModel> ListReviewPatch { get; set; }
        public List<RM_StatusModel> ListStatus { get; set; }
        public List<MN_BoPhanModel> Departments { get; set; }
    }

    public class RM_ReviewProjectFilterSessionModel : BaseSearchModel
    {
        public string Keyword { get; set; }
        public int? ReviewBatchID { get; set; }
        public int? Status { get; set; }
        public bool? IsReviewed { get; set; }
    }

    public class RM_ReviewBusinessOpportunityFilterSessionModel : BaseSearchModel
    {
        public string Keyword { get; set; }
        public int? ReviewBatchID { get; set; }
        public int? StatusID { get; set; }
        public bool? IsReviewed { get; set; }
    }

    public class RM_ReviewProjectModel : BaseModel
    {
        public int ReviewBatchID { get; set; }
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string StatusName { get; set; }
        public string StatusClass { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public int ReviewBatchItemID { get; set; }
        public int HighestReviewedLevel { get; set; }
        public DateTime? LastReviewedDate { get; set; }
        public bool IsReviewed { get; set; }
        public string AMName { get; set; }
        public DateTime? LastReviewDate { get; set; }
        public string LastReviewerName { get; set; }
        public string LastReviewComment { get; set; }
        public int ReviewCount { get; set; }
    }

    public class RM_ReviewBusinessOpportunityModel : BaseModel
    {
        public int ReviewPatchID { get; set; }
        public int BusinessOpportunityID { get; set; }
        public string CodeOpportunity { get; set; }
        public string OpportunityName { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string StatusName { get; set; }
        public string StatusClass { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal ClosingProbability { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int ReviewBatchItemID { get; set; }
        public int HighestReviewedLevel { get; set; }
        public DateTime? LastReviewedDate { get; set; }
        public bool IsReviewed { get; set; }
        public string AMName { get; set; }
        public DateTime? LastReviewDate { get; set; }
        public string LastReviewerName { get; set; }
        public string LastReviewComment { get; set; }
        public int ReviewCount { get; set; }
    }

    public class RM_ReviewFormModel
    {
        public int ReviewHistoryID { get; set; }
        public int ReviewBatchID { get; set; }
        public int ReviewBatchItemID { get; set; }
        public int DigitalSalesID { get; set; }
        public int UserLevel { get; set; }
        [AllowHtml]
        [CustomDisplayName("ReviewBatch_ReviewComment_Label")]
        public string ReviewComment { get; set; }
        [CustomDisplayName("ReviewBatch_Confirm_Label")]
        public bool IsConfirmed { get; set; }
        [CustomDisplayName("ReviewConclusion_Label")]
        public byte? ReviewConclusion { get; set; }
        public List<SelectListItem> ReviewConclusionOptions { get; set; } = new List<SelectListItem>();
        public List<HttpPostedFileBase> DinhKemFile { get; set; }
        public List<RM_ReviewBatchFilePathModel> ExistingFiles { get; set; }
        public List<int> DeletedFileIds { get; set; }
        public string ContinueReviewFilter { get; set; }
    }

    public class RM_ReviewHistoryModel : BaseModel
    {
        public int ReviewHistoryID { get; set; }
        public int ReviewBatchID { get; set; }
        public int ReviewBatchItemID { get; set; }
        public string Reviewer { get; set; }
        public string BatchCode { get; set; }
        public string BatchName { get; set; }
        public int ReviewLevel { get; set; }
        public byte ReviewAction { get; set; }
        public string ReviewComment { get; set; }
        public bool IsConfirmed { get; set; }
        public byte? ReviewConclusion { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<RM_ReviewBatchFilePathModel> ExistingFiles { get; set; }
    }

    public static class RM_ReviewConclusion
    {
        public const byte Accepted = 1;
        public const byte Interested = 2;
        public const byte Rejected = 3;

        public static bool IsValid(byte? value)
        {
            return value == Accepted || value == Interested || value == Rejected;
        }
    }

    public class RM_ReviewBatchFilePathModel : BaseModel
    {
        public int FilePathID { get; set; }
        public int ReviewHistoryID { get; set; }
        public string FilePath { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
