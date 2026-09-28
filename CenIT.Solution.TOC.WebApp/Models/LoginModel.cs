using System.ComponentModel.DataAnnotations;
using TSFramework.Libs.Attributes;

namespace CenIT.Solution.TOC.WebApp.Models
{
    public class LoginModel
    {
        [CustomDisplayName("Authorize_UserName")]
        [CustomRequired]
        public string UserName { get; set; }

        [CustomDisplayName("Authorize_Email")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [CustomRequired]
        [CustomDisplayName("Authorize_Password")]
        public string Password { get; set; }

        public bool RememberMe { get; set; } = false;

        public string SenderIP { get; set; }

        public string SenderHeader { get; set; }

        public bool NeedCaptcha { get; set; } = false;

        public int LoginFailCount { get; set; } = 0;

        public string MessageLoginFailCount => $"Bạn đã đăng nhập sai [{LoginFailCount}] lần";

        public bool IsShowWarning => LoginFailCount > 0;

        public string Warning => "Lưu ý: Tài khoản và IP của bạn sẽ bị khoá nếu bạn đăng nhập sai quá [5] lần.";
        public bool ShowPassword { get; set; } = false;
        public string URLLink { get; set; }
    }
}