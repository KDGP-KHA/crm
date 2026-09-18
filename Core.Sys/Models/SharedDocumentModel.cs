using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Sys.Models
{
    public class SharedDocumentModel : BaseModel
    {
        public int DocumentId { get; set; }

        [CustomRequired]
        [CustomDisplayName("SharedDoc_Category_Label")]
        public int CategoryId { get; set; }

        [CustomDisplayName("SharedDoc_Category_Label")]
        public string CategoryName { get; set; }

        [CustomRequired]
        [CustomDisplayName("SharedDoc_Name_Label")]
        public string DocumentName { get; set; }

        [AllowHtml]
        [CustomDisplayName("SharedDoc_Description_Label")]
        public string Description { get; set; }

        [CustomDisplayName("SharedDoc_File_Label")]
        public string FileName { get; set; }

        [CustomDisplayName("SharedDoc_File_Label")]
        public string OriginalFileName { get; set; }

        public string FilePath { get; set; }

        [CustomDisplayName("SharedDoc_FileSize_Label")]
        public long FileSize { get; set; }

        private string _fileSizeFormatted;
        public string FileSizeFormatted
        {
            get
            {
                if (!string.IsNullOrEmpty(_fileSizeFormatted)) return _fileSizeFormatted;
                if (FileSize <= 0) return "0 KB";
                if (FileSize < 1024 * 1024)
                    return string.Format("{0:0.#} KB", FileSize / 1024.0);
                return string.Format("{0:0.##} MB", FileSize / (1024.0 * 1024.0));
            }
            set { _fileSizeFormatted = value; }
        }

        [CustomDisplayName("SharedDoc_FileExtension_Label")]
        public string FileExtension { get; set; }

        [CustomDisplayName("SharedDoc_DownloadCount_Label")]
        public int DownloadCount { get; set; }

        [CustomDisplayName("SharedDoc_LastDownloadDate_Label")]
        public DateTime? LastDownloadDate { get; set; }

        [CustomDisplayName("SharedDoc_LastDownloadBy_Label")]
        public string LastDownloadBy { get; set; }

        public new bool CanEdit { get; set; } = true;

        public new bool CanDelete { get; set; } = true;

        [CustomDisplayName("SharedDoc_CreatedDate_Label")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [CustomDisplayName("SharedDoc_CreatedBy_Label")]
        public string CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public new string UpdatedBy { get; set; }

        public int? TotalRows { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }

        [CustomDisplayName("SharedDoc_File_Label")]
        public HttpPostedFileBase FileUpload { get; set; }
    }

    public class SharedDocumentSearchModel : BaseSearchModel
    {
        public string Keyword { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public IEnumerable<SelectListItem> Categories { get; set; }
    }
}
