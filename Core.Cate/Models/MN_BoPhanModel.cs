using System;
using System.Collections.Generic;
using System.Linq;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class MN_BoPhanModel : BaseSearchModel
    {
        public int RowIndex { get; set; }
        public int BoPhan_ID { get; set; }

        [CustomRequired]
        [CustomDisplayName("Department_Code_Label")]
        public string MaBoPhan { get; set; }

        public string TenBoPhanView { get; set; }

        [CustomDisplayName("Department_ParentName_Label")]
        public string TenBoPhanCha { get; set; }

        public int MaBoPhanCha { get; set; }

        [CustomDisplayName("Department_ParentName_Label")]
        public int? BoPhanCha_ID { get; set; }

        [CustomRequired]
        [CustomDisplayName("Department_Name_Label")]
        public string TenBoPhan { get; set; }

        public bool Da_Xoa { get; set; }
        public int Level { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? Ngay_CN { get; set; }
        public string Nguoi_Tao { get; set; }
        public string Nguoi_CN { get; set; }
        public string Ma_DV_BCN { get; set; }
        public int CapQuanLy { get; set; }

        public List<System.Web.Mvc.SelectListItem> ListBoPhanCha { get; set; }

        public string TenBoPhanDisplay
        {
            get
            {
                return new string('-', Level * 2) + " " + TenBoPhanView;
            }
        }
    }
    public class MN_BoPhanSearchModel : BaseSearchModel
    {
        public string Keyword { get; set; }
    }
    public class MN_BoPhanExportModel
    {
        public string MaBoPhan { get; set; }
        public string TenBoPhan { get; set; }
        public string TenBoPhanCha { get; set; }
        public int CapQuanLy { get; set; }
        public string TrangThai { get; set; }
    }
}
