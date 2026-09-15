using System.Collections.Generic;
using System.Web.Mvc;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesSearchModel : BaseModel
    {
        [CustomDisplayName("DigitalSalesSearch_Keyword_Label")]
        public string Keyword { get; set; }

        [CustomDisplayName("DigitalSalesSearch_BusinessType_Label")]
        public byte BusinessType { get; set; } // 0: Tất cả, 1: Cơ hội, 2: Dự án

        [CustomDisplayName("DigitalSalesSearch_Status_Label")]
        public int StatusID { get; set; }
        public string StatusIDs { get; set; }
        public List<int> ExcludedStatusIDs { get; set; } = new List<int>();
        public List<RM_DigitalSalesStatusModel> StatusItems { get; set; } = new List<RM_DigitalSalesStatusModel>();
        public int CustomerID { get; set; }
        [CustomDisplayName("DigitalSalesSearch_ProductService_Label")]
        public int ProductServiceID { get; set; }

        [CustomDisplayName("DigitalSalesSearch_Department_Label")]
        public int DepartmentID { get; set; }

        [CustomDisplayName("DigitalSalesSearch_Employee_Label")]
        public int EmployeeID { get; set; }

        [CustomDisplayName("DigitalSalesSearch_FromDate_Label")]
        public string FromDate { get; set; }

        [CustomDisplayName("DigitalSalesSearch_ToDate_Label")]
        public string ToDate { get; set; }

        [CustomDisplayName("DigitalSalesSearch_FilterSpecial_Label")]
        public int FilterSpecial { get; set; } // 0: Tất cả, 1: Dự án trọng điểm, 2: Quan tâm
        public bool? IsKeyProject { get; set; }
        public bool? IsFollowed { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string UserName { get; set; }

        public List<SelectListItem> FilterSpecialList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ProductServices { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> ListCustomer { get => Customers; set => Customers = value; }
        public List<SelectListItem> ListStatus { get => StatusList; set => StatusList = value; }
        public List<SelectListItem> ListProductService { get => ProductServices; set => ProductServices = value; }
        public List<SelectListItem> ListDepartment { get => Departments; set => Departments = value; }
        public List<SelectListItem> ListEmployee { get => Employees; set => Employees = value; }
    }
}
