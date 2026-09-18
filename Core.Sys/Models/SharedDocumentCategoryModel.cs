using System;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Sys.Models
{
    public class SharedDocumentCategoryModel : BaseModel
    {
        public int CategoryId { get; set; }

        [CustomRequired]
        [CustomDisplayName("SharedDoc_Category_Label")]
        public string CategoryName { get; set; }

        [CustomDisplayName("SharedDoc_Description_Label")]
        public string Description { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
