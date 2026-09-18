using System;
using System.Collections.Generic;

namespace Core.Cate.Models
{
    public class DataMigrationRequestModel
    {
        /// <summary>
        /// 1: Cơ hội kinh doanh (RM_BusinessOpportunity), 2: Dự án (RM_Project)
        /// </summary>
        public int SourceType { get; set; }

        /// <summary>
        /// ID của Cơ hội hoặc Dự án
        /// </summary>
        public int SourceID { get; set; }

        /// <summary>
        /// Chuỗi mã trạng thái phân cách bằng dấu chấm phẩy, ví dụ: UNCAPTURED; APPROACHING; FORMATION; IMPLEMENTING
        /// </summary>
        public string StatusTransitionCodes { get; set; }

        /// <summary>
        /// Có tự động di chuyển tệp đính kèm sang thư mục Uploads chuẩn và xóa file cũ không
        /// </summary>
        public bool MoveAttachments { get; set; } = true;
    }

    public class DataMigrationPreviewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public int SourceType { get; set; }
        public string SourceTypeName { get; set; }
        public int SourceID { get; set; }
        public string SourceCode { get; set; }
        public string Title { get; set; }
        public int? CustomerID { get; set; }
        public string CustomerName { get; set; }
        public int? ContactPersonID { get; set; }
        public string ContactPersonName { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public string AMName { get; set; }
        public string Description { get; set; }

        public string CurrentStatusName { get; set; } = "Đang xử lý";
        public bool AlreadyConverted { get; set; }
        public int? ExistingDigitalSalesID { get; set; }
        public string ExistingDigitalSalesCode { get; set; }

        public int MemberCount { get; set; }
        public List<string> MembersSummary { get; set; } = new List<string>();

        public int ProductCount { get; set; }
        public List<string> ProductsSummary { get; set; } = new List<string>();

        public int ActivityCount { get; set; }
        public int AttachmentCount { get; set; }
        public List<string> AttachmentsSummary { get; set; } = new List<string>();

        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string SuggestedChecklist { get; set; }
    }

    public class DataMigrationResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewDigitalSalesID { get; set; }
        public string NewCode { get; set; }
        public string DetailUrl { get; set; }
        public int MigratedMembers { get; set; }
        public int MigratedProducts { get; set; }
        public int MigratedDiscussions { get; set; }
        public int MigratedAttachments { get; set; }
        public int TimelineMilestones { get; set; }
        public int TrackingProcesses { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> StepLogs { get; set; } = new List<string>();
    }

    public class RecentMigrationItemModel
    {
        public int DigitalSalesID { get; set; }
        public string Code { get; set; }
        public string DigitalSalesCode
        {
            get => Code;
            set => Code = value;
        }
        public int SourceType { get; set; }
        public int SourceId { get; set; }
        public string Title { get; set; }
        public string DigitalSalesName
        {
            get => Title;
            set => Title = value;
        }
        public string CustomerName { get; set; }
        public byte BusinessType { get; set; }
        public string BusinessTypeName { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public string AMName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateStr => CreatedDate.ToString("dd/MM/yyyy HH:mm");
        public string CreatedBy { get; set; }
    }
}
