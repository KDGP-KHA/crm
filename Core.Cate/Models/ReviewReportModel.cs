using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class ReviewReportModel : BaseModel
    {
        public int ObjectType { get; set; }
        public int ObjectID { get; set; }
        public string ObjectCode { get; set; }
        public string ObjectName { get; set; }
        public string ObjectTypeName { get; set; }
        public string Level4Reviewer { get; set; }
        public DateTime? Level4Date { get; set; }
        public string Level4Comment { get; set; }
        public bool? Level4Status { get; set; }
        public string Level3Reviewer { get; set; }
        public DateTime? Level3Date { get; set; }
        public string Level3Comment { get; set; }
        public bool? Level3Status { get; set; }
        public string Level2Reviewer { get; set; }
        public DateTime? Level2Date { get; set; }
        public string Level2Comment { get; set; }
        public bool? Level2Status { get; set; }
        public bool IsReviewed { get; set; }
        public byte? FinalReviewConclusion { get; set; }
    }

    public class ReviewReportSearchModel : BaseSearchModel
    {
        public int ReviewBatchID { get; set; }
        public List<RM_ReviewBatchModel> ListReviewPatch { get; set; }
    }
}
