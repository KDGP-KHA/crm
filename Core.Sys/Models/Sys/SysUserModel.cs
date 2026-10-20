using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using Core.Cate.Models;
using TSFramework.Core.Enums;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Models.Base;

namespace Core.Sys.Models.Sys
{
    public class SysUserModel : BaseModel
    {
        public int? UserId { get; set; }

        [CustomDisplayName("User_Label_OfficeName")]
        public string OfficeName { get; set; }

        [CustomRequired]
        [CustomDisplayName("User_Label_FullName")]
        public string FullName { get; set; }

        [CustomRequired]
        [CustomDisplayName("User_Label_UserName")]
        public string UserName { get; set; }

        [CustomDisplayName("User_Label_Email")]
        [CustomRequired]
        public string Email { get; set; }

        public string DetailUrl { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public bool? IsCollaborator { get; set; }
        public string RoleIDs { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string HostUrl { get; set; }

        [CustomDisplayName("User_Label_Avatar")]
        public string Avatar { get; set; }

        [CustomDisplayName("User_Label_Avatar")]
        public string AvatarPath { get; set; }

        [CustomDisplayName("User_Label_Avatar")]
        public HttpPostedFileBase AvatarFileBase { get; set; }

        [CustomDisplayName("User_Label_Phone")]
        public string Phone { get; set; }

        [RequiredIfNot("UserId", null)]
        [CustomDisplayName("Reason_Title")]
        public override string Reason { get; set; }

        public new int? TotalRow { get; set; } = 0;

        public List<SysRoleModel> ListRoles { get; set; }
        public string Permits { get; set; }
        public string ReviewDepartment { get; set; }
        public byte ReviewLevel { get; set; }
        public DateTime? RegulationViewedDate { get; set; }
    }

    public class SysUserSearchModel
    {
        public string TuKhoa { get; set; }
    }

    public class UserPermitReviewModel
    {
        public int? UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        [CustomDisplayName("User_ReviewDepartment_Label")]
        public string ReviewDepartment { get; set; }
        [CustomDisplayName("User_ReviewLevel_Label")]
        public byte ReviewLevel { get; set; }
        public List<byte> ListLevel { get; set; } = new List<byte> { 2, 3, 4 };
        public List<MN_BoPhanModel> ListBophans { get; set; } = new List<MN_BoPhanModel>();
    }
}