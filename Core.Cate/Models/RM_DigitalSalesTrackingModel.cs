using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesTrackingModel : BaseModel
    {
        public int TrackingID { get; set; }
        public string TrackingCode { get; set; }
        public int DigitalSalesID { get; set; }
        public int? ParentID { get; set; }
        public int? ProcessID { get; set; }
        public string ProcessName { get; set; }
        public int? StatusID { get; set; }
        public string SalesStatusName { get; set; }
        public int ProcessCountOfStatus { get; set; }
        public int? TimelineID { get; set; }
        public DateTime? TimelineDate { get; set; }
        public int? ProgressID { get; set; }
        [CustomRequired]
        [CustomDisplayName("DigitalSalesTracking_TaskName_Label")]
        public string TaskName { get; set; }
        public string ProgressName { get; set; }
        [CustomDisplayName("DigitalSalesTracking_DurationDays_Label")]
        public int? DurationDays { get; set; }
        public int DefaultDurationDays { get; set; }
        [CustomDisplayName("DigitalSalesTracking_AssignedUser_Label")]
        public int? AssignedUserID { get; set; }
        public string AssignedUserName { get; set; }
        [CustomDisplayName("DigitalSalesTracking_StartDate_Label")]
        public DateTime StartDate { get; set; }

        [CustomDisplayName("DigitalSalesTracking_Deadline_Label")]
        public DateTime? Deadline { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string CompletedBy { get; set; }
        public string CompletedByName { get; set; }
        [CustomDisplayName("DigitalSalesTracking_Status_Label")]
        public byte Status { get; set; } // 1: Chưa làm, 2: Đang làm, 3: Hoàn thành, 4: Quá hạn
        public string TaskStatusName { get; set; }
        public int IsOverdue { get; set; } // 1: Quá hạn, 0: Bình thường
        [AllowHtml]
        [CustomDisplayName("DigitalSalesTracking_ResultNote_Label")]
        public string ResultNote { get; set; }

        [CustomDisplayName("DigitalSalesTracking_AttachmentFile_Label")]
        public string AttachmentFile { get; set; }
        public bool IsCustomTask { get; set; }
        [CustomDisplayName("DigitalSalesWorkflow_ProgressSortOrder_Label")]
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedByName { get; set; }

        // Helper computed properties
        public int EffectiveDurationDays => DurationDays.HasValue && DurationDays.Value > 0 ? DurationDays.Value : (DefaultDurationDays > 0 ? DefaultDurationDays : 3);
        public DateTime MaxDeadline => StartDate.AddDays(EffectiveDurationDays);
        public List<RM_DigitalSalesTrackingModel> TodoList { get; set; } = new List<RM_DigitalSalesTrackingModel>();
    }

    public class RM_DigitalSalesProgressImportRowDTO
    {
        public int RowIndex { get; set; }
        public string TaskName { get; set; }
        public string AssignedUserName { get; set; }
        public int? AssignedUserID { get; set; }
        public string StartDateStr { get; set; }
        public DateTime? StartDate { get; set; }
        public int? DurationDays { get; set; }
        public string DeadlineStr { get; set; }
        public DateTime? Deadline { get; set; }
        public string Note { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
    }

    public class RM_DigitalSalesTodoImportRowDTO
    {
        public int RowIndex { get; set; }
        public string TrackingCode { get; set; }
        public int? ParentTrackingID { get; set; }
        public string ParentTaskName { get; set; }
        public DateTime? ParentMaxDeadline { get; set; }
        public string TaskName { get; set; }
        public string AssignedUserName { get; set; }
        public int? AssignedUserID { get; set; }
        public byte? Status { get; set; }
        public string StatusStr { get; set; }
        public string StartDateStr { get; set; }
        public DateTime? StartDate { get; set; }
        public int? DurationDays { get; set; }
        public string DeadlineStr { get; set; }
        public DateTime? Deadline { get; set; }
        public string Note { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
    }
}

