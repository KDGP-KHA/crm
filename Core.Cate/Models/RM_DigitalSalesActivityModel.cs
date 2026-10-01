using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesActivityModel : BaseModel
    {
        public int ActivityID { get; set; }
        public int DigitalSalesID { get; set; }
        public byte ActivityType { get; set; }
        // 1: Trao đổi / Thảo luận (Discussion)
        // 2: Chuyển trạng thái (StatusChange)
        // 3: Hoàn thành đầu việc Checklist (ChecklistCompleted)
        // 4: Hoàn thành Quy trình / Tiến trình (ProcessCompleted)
        // 5: Cập nhật nội dung / Ghi chú Checklist (ChecklistUpdated)

        [AllowHtml]
        public string Content { get; set; }
        public string Attachments { get; set; }
        public string MentionedUserIDs { get; set; }
        public string MentionedNames { get; set; }
        public int? ReferenceID { get; set; }
        public int? ReplyToActivityID { get; set; }
        public string ReplyToActionByName { get; set; }
        public string ReplyToContent { get; set; }
        public DateTime? ReplyToActionDate { get; set; }
        public byte? ReplyToActivityType { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionBy { get; set; }
        public string ActionByName { get; set; }
        public string ActionByAvatar { get; set; }
        public string ActionByDepartment { get; set; }
        public bool IsDeleted { get; set; }

        public List<ActivityAttachmentItem> AttachmentList { get; set; } = new List<ActivityAttachmentItem>();
    }

    public class ActivityAttachmentItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; }
        public string Extension { get; set; }
        public bool IsImage { get; set; }
    }
}
