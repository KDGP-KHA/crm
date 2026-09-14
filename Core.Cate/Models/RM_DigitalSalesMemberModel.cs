using System;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Cate.Models
{
    public class RM_DigitalSalesMemberModel : BaseModel
    {
        public int MemberID { get; set; }
        public int DigitalSalesID { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSalesMember_User_Label")]
        public int UserID { get; set; }

        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        [CustomDisplayName("DigitalSalesMember_RoleTitle_Label")]
        public string RoleTitle { get; set; }

        [CustomDisplayName("DigitalSalesMember_IsAM_Label")]
        public bool IsAM { get; set; }

        [CustomDisplayName("DigitalSalesMember_Note_Label")]
        public string Note { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Model nhập liệu dùng riêng cho thao tác thêm nhiều thành viên vào hồ sơ.
    /// Không dùng RM_DigitalSalesMemberModel vì mỗi lần lưu có thể tạo nhiều bản ghi.
    /// </summary>
    public class RM_DigitalSalesMemberFormModel : BaseModel
    {
        public int DigitalSalesID { get; set; }

        [CustomRequired]
        [CustomDisplayName("DigitalSalesMember_User_Label")]
        public int[] EmployeeIDs { get; set; }

        [CustomDisplayName("DigitalSalesMember_RoleTitle_Label")]
        public int[] RoleIDs { get; set; }

        [CustomDisplayName("DigitalSalesMember_CustomRole_Label")]
        public string CustomRole { get; set; }

        [CustomDisplayName("DigitalSalesMember_IsAM_Label")]
        public bool IsAM { get; set; }

        [CustomDisplayName("DigitalSalesMember_Note_Label")]
        public string Note { get; set; }
    }
}
