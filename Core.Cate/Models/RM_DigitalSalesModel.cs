using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesModel : BaseModel
    {
        public int DigitalSalesID { get; set; }

        [CustomDisplayName("DigitalSales_Code_Label")]
        public string Code { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_Title_Label")]
        public string Title { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_BusinessType_Label")]
        public byte BusinessType { get; set; } // 1: Cơ hội, 2: Dự án
        public string BusinessTypeName { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_Status_Label")]
        public int StatusID { get; set; }
        public string StatusCode { get; set; }
        public string StatusName { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_Customer_Label")]
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }

        [CustomDisplayName("DigitalSales_ContactPerson_Label")]
        public int? ContactPerson_ID { get; set; }
        public string ContactPersonName { get; set; }
        public string ContactPersonPhone { get; set; }
        public string ContactPersonEmail { get; set; }

        [CustomDisplayName("DigitalSales_TotalExpectedRevenue_Label")]
        public decimal? TotalExpectedRevenue { get; set; }

        [CustomDisplayName("DigitalSales_TotalActualRevenue_Label")]
        public decimal? TotalActualRevenue { get; set; }

        [CustomDisplayName("DigitalSales_ClosingProbability_Label")]
        public decimal? ClosingProbability { get; set; }

        [CustomDisplayName("DigitalSales_ExpectedDate_Label")]
        public DateTime? ExpectedDate { get; set; }

        [CustomDisplayName("DigitalSales_StartDate_Label")]
        public DateTime? StartDate { get; set; }

        [CustomDisplayName("DigitalSales_EndDate_Label")]
        public DateTime? EndDate { get; set; }

        [CustomDisplayName("DigitalSales_Contract_Label")]
        public int? ContractID { get; set; }

        [CustomDisplayName("DigitalSales_ContractNo_Label")]
        public string ContractNo { get; set; }

        [CustomDisplayName("DigitalSales_ContractValue_Label")]
        public decimal? ContractValue { get; set; }

        [CustomDisplayName("DigitalSales_ContractSignDate_Label")]
        public DateTime? ContractSignDate { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_AssignedEmployee_Label")]
        public int? AssignedEmployeeID { get; set; }
        public string AssignedEmployeeName { get; set; }

        [CustomDisplayName("DigitalSales_Department_Label")]
        public int? DepartmentID { get; set; }
        public string DepartmentName { get; set; }

        /// <summary>
        /// Mô tả chi tiết nhu cầu / Ghi chú dịch vụ số (Không phải Lý do chuyển trạng thái)
        /// </summary>
        [AllowHtml]
        [CustomDisplayName("DigitalSales_Note_Label")]
        public string Note { get; set; }

        [CustomDisplayName("DigitalSales_FileAttach_Label")]
        public string FileAttach { get; set; }

        [CustomDisplayName("DigitalSales_IsKeyProject_Label")]
        public bool IsKeyProject { get; set; }

        [CustomDisplayName("DigitalSales_IsFollowed_Label")]
        public bool IsFollowed { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime? ActionTime { get; set; }

        // Computed & Joined fields
        public string ProductServiceNames { get; set; }
        public int ProductCount { get; set; }
        public int ProgressPercentage { get; set; }
        public int HasOverdueTasks { get; set; }
        public int? TotalCount { get; set; }

        // Danh sách sản phẩm chi tiết
        public List<RM_DigitalSalesProductModel> Products { get; set; } = new List<RM_DigitalSalesProductModel>();

        // Danh sách tiến trình thực thi
        public List<RM_DigitalSalesTrackingModel> TrackingTasks { get; set; } = new List<RM_DigitalSalesTrackingModel>();

        // Danh sách thành viên tham gia
        public List<RM_DigitalSalesMemberModel> Members { get; set; } = new List<RM_DigitalSalesMemberModel>();

        // Lịch sử chuyển đổi trạng thái (Timeline)
        public List<RM_DigitalSalesTimelineModel> Timelines { get; set; } = new List<RM_DigitalSalesTimelineModel>();

        // Dòng trao đổi & hoạt động (Activity & Discussion Stream)
        public List<RM_DigitalSalesActivityModel> Activities { get; set; } = new List<RM_DigitalSalesActivityModel>();

        // Lịch sử rà soát định kỳ của hồ sơ
        public List<RM_ReviewHistoryModel> ReviewHistory { get; set; } = new List<RM_ReviewHistoryModel>();

        // Dropdown sources for UI
        public List<SelectListItem> ListCustomer { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListProductService { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListStatus { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListContactPerson { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListEmployee { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListDepartment { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListContract { get; set; } = new List<SelectListItem>();
    }

    public class RM_DigitalSalesChangeStatusViewModel
    {
        public int DigitalSalesID { get; set; }
        public string Title { get; set; }
        public string CurrentStatusName { get; set; }
        public byte CurrentBusinessType { get; set; }
        public string CurrentBusinessTypeName { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_NewStatus_Label")]
        public int NewStatusID { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSales_ChangeStatusNote_Label")]
        public string Note { get; set; }

        [CustomDisplayName("DigitalSales_ChangeStatusAttachment_Label")]
        public string AttachmentPath { get; set; }

        public List<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
        public int? SelectedProcessID { get; set; }
        public string TrackingItemsJson { get; set; }
        public bool HasWorkflowProgressBox { get; set; }
    }

    public class ChangeStatusTrackingItemDTO
    {
        public int? ProcessID { get; set; }
        public int? ProgressID { get; set; }
        public string TaskName { get; set; }
        public int? AssignedUserID { get; set; }
        public DateTime? StartDate { get; set; }
        public int? DurationDays { get; set; }
        public DateTime? Deadline { get; set; }
        public int SortOrder { get; set; }
        public bool IsCustomTask { get; set; }
        public string Note { get; set; }
    }

    public class RM_DigitalSalesChangeProcessViewModel
    {
        public int DigitalSalesID { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public int CurrentProcessID { get; set; }
        public int SelectedProcessID { get; set; }
        public List<RM_DigitalSalesProcessModel> AvailableProcesses { get; set; } = new List<RM_DigitalSalesProcessModel>();
    }

    public class RM_DigitalSalesTrackingReportViewModel
    {
        public int TrackingID { get; set; }
        public int DigitalSalesID { get; set; }
        public string TaskName { get; set; }
        public byte Status { get; set; }
        public string ResultNote { get; set; }
        public string AttachmentFile { get; set; }
    }

    public class RM_DigitalSalesUserModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
    }

    public class RM_DigitalSalesTimelineDetailViewModel
    {
        public int DigitalSalesID { get; set; }
        public string SalesTitle { get; set; }
        public string SalesCode { get; set; }
        public string DigitalSalesCode { get; set; }
        public string DigitalSalesName { get; set; }
        public RM_DigitalSalesTimelineModel Timeline { get; set; }
        public List<RM_DigitalSalesTrackingModel> Tasks { get; set; } = new List<RM_DigitalSalesTrackingModel>();
        public List<TimelineAttachmentFileItem> TransitionFiles { get; set; } = new List<TimelineAttachmentFileItem>();
        public List<TimelineTaskFileGroup> TaskFileGroups { get; set; } = new List<TimelineTaskFileGroup>();
    }

    public class TimelineTaskFileGroup
    {
        public int TrackingID { get; set; }
        public string TrackingCode { get; set; }
        public string TaskName { get; set; }
        public List<TimelineAttachmentFileItem> Files { get; set; } = new List<TimelineAttachmentFileItem>();
    }

    public class TimelineAttachmentFileItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Extension { get; set; }
        public string SourceTaskName { get; set; }
        public string UploadedBy { get; set; }
        public DateTime? UploadedDate { get; set; }
    }
}

