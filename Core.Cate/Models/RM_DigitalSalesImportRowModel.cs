using System;
using System.Collections.Generic;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesImportRowModel
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }

        // Khách hàng
        public string CustomerInput { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerShortName { get; set; }
        public string CustomerTaxCode { get; set; }

        // AM chủ trì (nhân sự đơn vị quản lý)
        public string AMInput { get; set; }
        public int AssignedEmployeeID { get; set; }
        public string AMUserName { get; set; }
        public string AMFullName { get; set; }
        public int? DepartmentID { get; set; }

        // Thuộc tính bổ sung
        public int? ApplyYear { get; set; }
        public bool IsKey { get; set; }
        public bool IsFocus { get; set; }
        public decimal? ClosingProbability { get; set; }

        // Sản phẩm / Dịch vụ số
        public string ProductInput { get; set; }
        public int? ProductServiceID { get; set; }
        public string ProductName { get; set; }
        public decimal? ExpectedRevenue { get; set; }

        public string Note { get; set; }

        // Kết quả kiểm tra (Validation)
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => Errors == null || Errors.Count == 0;
        public string ErrorMessage => Errors != null && Errors.Count > 0 ? string.Join("; ", Errors) : string.Empty;
    }
}
