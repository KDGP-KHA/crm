using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class MN_ChucVuModel : BaseSearchModel
    {
        public int RowIndex { get; set; }
        public int ChucVu_ID { get; set; }
        [CustomRequired]
        [CustomDisplayName("Position_PositionName_Label")]
        public string TenChucVu { get; set; }

        [CustomRequired]
        [CustomDisplayName("Position_PositionCode_Label")]
        public string MaChucVu { get; set; }

        public bool DaXoa { get; set; }
    }

    public class MN_ChucVuSearchModel : BaseSearchModel
    {
        public int PageIndex { get; set; }
        public string Keyword { get; set; }
    }
    public class MN_ChucVuExportModel
    {
        public string MaChucVu { get; set; }
        public string TenChucVu { get; set; }
        public string TrangThai { get; set; }
    }
}
