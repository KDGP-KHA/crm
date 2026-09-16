using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;
using CenIT.Solution.TOC.WebApp.Models;
using CenIT.Solution.TOC.WebApp.Providers.VNPTEmailMembership; 
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.BaseApps;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Mail;
using TSFramework.Libs.Principals;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Providers;
using TSFramework.Libs.Utils;
using CenIT.SSOServices.ServiceSSO;

namespace CenIT.Solution.TOC.WebApp.Controllers
{
    /// <summary>
    /// Xử lý các luồng đăng nhập, đổi mật khẩu, quên mật khẩu và cập nhật thông tin tài khoản.
    /// </summary>
    public class AccountController : BaseController
    {
        private const string SESSION_VARIABLE_NAME = "SessionNumber";

        private readonly SysUserCache _userCache = new SysUserCache();
        private readonly SysUserIPLockCache _userIPLockCache = new SysUserIPLockCache();
        private readonly SysModuleCache _moduleCache = new SysModuleCache();

        private readonly string _defaultAvatar = ConfigurationManager.AppSettings["AppAvatarDefault_URL"];
        private readonly string _avatarFolderPath = ConfigurationManager.AppSettings["AppAvatarFolder_Path"];
        private readonly VNPTEmailMembershipProvider _vnptProvider = new VNPTEmailMembershipProvider();
        private readonly string _vnptEmailExtension =
            ConfigurationManager.AppSettings["VNPT_Email_Extension"] ?? "@vnpt.vn";
        private const string VNPT_DEFAULT_ROLE_ID = "6";
        private const string VNPT_DEFAULT_MODULE_ID = "2";
        private readonly string _vnptAccountNotConfiguredMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_VNPTAccountNotConfigured");
        private readonly string _loginSuccessMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_LoginSuccess");
        private readonly string _currentPasswordIncorrectMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_CurrentPasswordIncorrect");
        private readonly string _passwordPolicyInvalidMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_PasswordPolicyInvalid");
        private readonly string _userNotFoundOrLockedMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_UserNotFoundOrLocked");
        private readonly string _changePasswordSuccessAndForceLogoutMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_ChangePasswordSuccessAndForceLogout");
        private readonly string _emailRequiredMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_EmailRequired");
        private readonly string _emailInvalidMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_EmailInvalid");
        private readonly string _userInfoNotFoundMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_UserInfoNotFound");
        private readonly string _resetPasswordEmailSentMessage =
            AppProcessor.Messagor.GetMessage("Account_Message_ResetPasswordEmailSent");
        private readonly string appCode = ConfigurationManager.AppSettings["appCode"] ?? "";
        private readonly string _appHostUrl = ConfigurationManager.AppSettings["App_HostUrl"] ?? "";

        // SSO tập đoàn theo cơ chế AppCode (cổng cenit + web service CheckUser)
        private const string SSO_TICKET_SESSION_KEY = "SSO_Ticket";
        private readonly string _ssoPortalUrl =
            ConfigurationManager.AppSettings["ssoPortalBaseUrl"]
            ?? ConfigurationManager.AppSettings["ssoPortaUrl"]
            ?? "http://ssovnpt.cenit.vn/Login.aspx";
        private readonly string _ssoServiceUrl = ConfigurationManager.AppSettings["ssoServiceUrl"] ?? "";

        /// <summary>
        /// Khởi tạo web service SSO; cho phép cấu hình lại URL qua AppSettings "ssoServiceUrl".
        /// </summary>
        private CService CreateSsoService()
        {
            var service = new CService();
            if (!string.IsNullOrWhiteSpace(_ssoServiceUrl))
            {
                service.Url = _ssoServiceUrl;
            }
            return service;
        }

        /// <summary>
        /// Xác định request hiện tại có thuộc host chính thức được cấu hình để dùng SSO hay không.
        /// Chỉ so sánh hostname, không phụ thuộc giao thức hoặc port.
        /// </summary>
        private bool IsSsoRequest()
        {
            return IsSameHost(Request?.Url?.Host, _appHostUrl);
        }

        /// <summary>
        /// So sánh hostname của request với hostname trong App_HostUrl.
        /// Cấu hình rỗng/sai hoặc request không có host được xem là môi trường không dùng SSO.
        /// </summary>
        private static bool IsSameHost(string requestHost, string configuredHostUrl)
        {
            if (string.IsNullOrWhiteSpace(requestHost) || string.IsNullOrWhiteSpace(configuredHostUrl))
                return false;

            if (!Uri.TryCreate(configuredHostUrl.Trim(), UriKind.Absolute, out var configuredUri))
                return false;

            return string.Equals(
                requestHost.Trim().TrimEnd('.'),
                configuredUri.Host.TrimEnd('.'),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Hiển thị màn hình đăng nhập và làm sạch session cũ của người dùng.
        /// </summary>
        /// <param name="returnUrl">Đường dẫn điều hướng sau khi đăng nhập thành công.</param>
        /// <returns>Màn hình đăng nhập.</returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl = "", string loginMode = "")
        {
            // Chống Open Redirect
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/Dashboard/Dashboard";
            }

            // =====================================================
            // LẤY THÔNG TIN SSO CALLBACK
            // =====================================================
            string ssoSession = Request.QueryString["Session"];
            string ssoUserKey = Request.QueryString["UserKey"];
            string ssoTicket = Request.QueryString["Ticket"];
            // URL đăng nhập SSO
            string ssoPortalLogin = string.Format("{0}?appcode={1}", _ssoPortalUrl.TrimEnd('/'), Server.UrlEncode(appCode));
            var hasSsoCallback = !string.IsNullOrEmpty(ssoSession) && !string.IsNullOrEmpty(ssoTicket);

            // Không phân biệt host: người dùng luôn được chọn SSO hoặc tài khoản CRM.
            // SSO chỉ được khởi tạo khi bấm nút tương ứng; callback vẫn được xử lý ở mọi host.
            if (!hasSsoCallback)
            {
                if (string.Equals(loginMode, "sso", StringComparison.OrdinalIgnoreCase))
                    return Redirect(ssoPortalLogin);

                if (Session[SESSION_VARIABLE_NAME] == null) Session[SESSION_VARIABLE_NAME] = 0;
                List<string> sessionKeys = Session.Keys.Cast<string>().ToList();
                foreach (string key in sessionKeys)
                {
                    if (!string.Equals(key, "FrontEndUser"))
                        Session.Remove(key);
                }
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.UseSso = true;
                ViewBag.SsoLoginUrl = Url.Action("Login", "Account", new { returnUrl, loginMode = "sso" });
                FormsAuthentication.SignOut();
                if (Request.IsAjaxRequest()) Response.StatusCode = 401;
                return View(new LoginModel());
            }

            try
            {
                // =====================================================
                // CHƯA CÓ THÔNG TIN SSO
                // → CHUYỂN SANG CỔNG SSO
                // =====================================================
                // =====================================================
                // XÁC THỰC THÔNG TIN SSO
                // =====================================================
                var ssoService = CreateSsoService();
                CheckUserInfo ssoUser = ssoService.CheckUser(ssoSession, ssoUserKey, ssoTicket, appCode, "");
                if (ssoUser == null || string.IsNullOrWhiteSpace(ssoUser.globalusername))
                {
                    return Content(
                        "Xác thực SSO không thành công. " +
                        "Vui lòng thử lại hoặc liên hệ quản trị hệ thống."
                    );
                }
                // =====================================================
                // MAP TÀI KHOẢN SSO → USER CRM
                // =====================================================
                var user = _userCache.GetByUserName(ssoUser.globalusername) ?? _userCache.GetByEmail(NormalizeVNPTUsername(ssoUser.globalusername));
                if (user == null || !user.IsActive)
                {
                    return Content(
                        "Tài khoản SSO chưa được khai báo hoặc đã bị khóa trên hệ thống CRM. " +
                        "Vui lòng liên hệ quản trị hệ thống."
                    );
                }
                // =====================================================
                // CẤP QUYỀN MẶC ĐỊNH
                // =====================================================
                EnsureVNPTPermissions(user);
                // =====================================================
                // ĐĂNG NHẬP VÀO CRM
                // =====================================================
                var loginUser = BuildLoginUser(user.UserName, true, user);
                SignInUser(loginUser);
                // Lưu Ticket để dùng khi Logout
                Session[SSO_TICKET_SESSION_KEY] = ssoTicket;
                // =====================================================
                // LƯU LỊCH SỬ LOGIN
                // =====================================================
                string senderIP = Request.UserHostAddress;
                string senderHeader = string.Join(",", Request.Headers.AllKeys.Select(key => key + ": " + Request.Headers[key]));
                _userCache.SaveLogin(user.UserName, true, senderIP, senderHeader);
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                return Content(
                    "Không thể kết nối tới hệ thống SSO. " +
                    "Vui lòng thử lại sau hoặc liên hệ quản trị hệ thống."
                );
            }
        }

        /// <summary>
        /// Xử lý đăng nhập thông thường bằng tài khoản CRM, không phụ thuộc host.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(LoginModel model, string returnUrl = "", string loginMode = "")
        {
            model = model ?? new LoginModel();

            if (!string.IsNullOrEmpty(model.URLLink))
            {
                ModelState.Remove("Password");
                ModelState.Remove("UserName");
            }

            UpdateLoginFailCount(model);

            if (!ModelState.IsValidField("UserName") || !ModelState.IsValidField("Password"))
            {
                ViewBag.ReturnUrl = returnUrl;
                return PartialView("_LoginBody", model);
            }

            ApplyLoginCredentials(model);
            model.SenderIP = Request.UserHostAddress;
            model.SenderHeader = string.Join(",", Request.Headers);

            bool isVNPTAccount;
            SysUserModel vnptUser;
            string loginFailMessage;
            if (!TryAuthenticateUser(model, out isVNPTAccount, out vnptUser, out loginFailMessage))
            {
                SendResponseNotify("MsgLoginFail", loginFailMessage, EnumProcessType.NonFormat, EnumMsgIcon.Error);
                return PartialView("_LoginBody", model);
            }

            AppPrincipalSerializeModel loginUser = BuildLoginUser(model.UserName, isVNPTAccount, vnptUser);
            List<SysRoleModel> roles = _userCache.GetRoles(loginUser.UserId);
            if (roles == null || roles.Count <= 0)
            {
                SendResponseNotify("MsgLoginFail", AppProcessor.Messagor.GetMessage("API_No_Right"),
                    EnumProcessType.NonFormat, EnumMsgIcon.Error);
                Session[SESSION_VARIABLE_NAME] = 0;
                model.LoginFailCount = 0;
                model.NeedCaptcha = false;
                return PartialView("_LoginBody", model);
            }

            SignInUser(loginUser);
            AppProcessor.Author.SaveLogin(loginUser.UserName, true, model.SenderIP, model.SenderHeader);

            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Action("Index", "Home");

            return Json(new
            {
                status = true,
                returnUrl,
                message = CreateMessage(_loginSuccessMessage, EnumProcessType.NonFormat,
                    EnumMsgIcon.Success, EnumMsgPlacement.TopCenter)
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Hiển thị popup đổi mật khẩu cho tài khoản được chọn.
        /// </summary>
        /// <param name="userName">Tên đăng nhập cần đổi mật khẩu.</param>
        /// <returns>Popup đổi mật khẩu hoặc thông báo khi tài khoản không tồn tại.</returns>
        [HttpGet]
        [AllowAnyPermission]
        public ActionResult ChangePassword(string userName)
        {
            var currentUser = _userCache.GetByUserName(userName);
            if (currentUser == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage("Tài khoản", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            return PartialView("_ChangePassword", new ChangePasswordModel { UserName = currentUser.UserName });
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho tài khoản hiện tại.
        /// </summary>
        /// <param name="model">Thông tin đổi mật khẩu từ màn hình.</param>
        /// <returns>Kết quả xử lý đổi mật khẩu.</returns>
        [HttpPost]
        [AllowAnyPermission]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid) return PartialView("_PasswordModel", model);

            var isAuthen = AppProcessor.Author.IsValidUser(model.UserName, model.CurrentPassword);
            if (!isAuthen)
            {
                ModelState.AddModelError("CurrentPassword", _currentPasswordIncorrectMessage);
                return PartialView("_PasswordModel", model);
            }

            if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\W)"))
            {
                ModelState.AddModelError("NewPassword", _passwordPolicyInvalidMessage);
                return PartialView("_PasswordModel", model);
            }

            var salt = UtilEncrypt.GenerateSalt();
            var passwordHash = UtilEncrypt.GenerateCryptoPassword(model.NewPassword, salt);
            var userId = _userCache.ResetPassword(model.UserName, passwordHash, salt, "Đổi mật khẩu", User.UserName);

            switch (userId)
            {
                case -1:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage(_userNotFoundOrLockedMessage,
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                default:
                    AppProcessor.Notifider.ForceLogout(model.UserName);
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage(
                            _changePasswordSuccessAndForceLogoutMessage,
                            EnumProcessType.NonFormat,
                            EnumMsgIcon.Success)
                    }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Hiển thị màn hình đặt lại mật khẩu theo token gửi qua email.
        /// </summary>
        /// <param name="userName">Tên đăng nhập cần đặt lại mật khẩu.</param>
        /// <param name="token">Token xác thực yêu cầu đặt lại mật khẩu.</param>
        /// <returns>Màn hình đặt lại mật khẩu hoặc thông báo khi token không hợp lệ.</returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPassword(string userName, string token)
        {
            var currentUser = _userCache.GetByUserName(userName);
            if (currentUser == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"Tài khoản {userName}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            if (!currentUser.IsActive)
                return Json(new
                {
                    status = true,
                    message = CreateMessage(
                        $"Tài khoản <b>[{currentUser.FullName} - {currentUser.UserName}]</b> đã ngưng hoạt động.",
                        EnumProcessType.NonFormat, EnumMsgIcon.Error)
                });

            var resetPasswordModel = new ResetPasswordModel { UserName = currentUser.UserName };
            var decryptToken = UtilEncrypt.Decrypt(token, currentUser.Password, out var isCorrect);

            if (isCorrect && !string.IsNullOrEmpty(decryptToken))
            {
                var arrTokens = decryptToken.Split('-');
                var dataTicks = long.Parse(arrTokens.Length > 0 ? arrTokens[1] : "0");
                var timeExpier = new DateTime(dataTicks);
                if (timeExpier >= DateTime.Now) return PartialView("ResetPassword", resetPasswordModel);
                resetPasswordModel.ErrMessage = AppProcessor.Messagor.GetMessage("ResetPassword_Token_Expired_Message");
                resetPasswordModel.IsPermit = false;
                return PartialView("ResetPassword", resetPasswordModel);
            }

            resetPasswordModel.ErrMessage = AppProcessor.Messagor.GetMessage("ResetPassword_Token_Incorrect_Message");
            resetPasswordModel.IsPermit = false;
            return PartialView("ResetPassword", resetPasswordModel);
        }

        /// <summary>
        /// Xử lý đặt lại mật khẩu từ liên kết quên mật khẩu.
        /// </summary>
        /// <param name="model">Thông tin mật khẩu mới từ màn hình.</param>
        /// <returns>Kết quả xử lý đặt lại mật khẩu.</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return PartialView("_ResetPassword", model);

            if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\W)"))
            {
                ModelState.AddModelError("NewPassword", _passwordPolicyInvalidMessage);
                return PartialView("_ResetPassword", model);
            }

            var salt = UtilEncrypt.GenerateSalt();
            var passwordHash = UtilEncrypt.GenerateCryptoPassword(model.NewPassword, salt);
            var userId = _userCache.ResetPassword(model.UserName, passwordHash, salt, "Đặt lại mật khẩu", model.UserName);

            switch (userId)
            {
                case -1:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage(_userNotFoundOrLockedMessage,
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                default:
                    AppProcessor.Notifider.ForceLogout(model.UserName);
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage("Mật khẩu", EnumProcessType.Edit, EnumMsgIcon.Success)
                    }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Hiển thị popup cập nhật thông tin tài khoản hiện tại.
        /// </summary>
        /// <param name="userName">Tên đăng nhập cần cập nhật thông tin.</param>
        /// <returns>Popup cập nhật thông tin tài khoản.</returns>
        [HttpGet]
        [AllowAnyPermission]
        public ActionResult EditInfo(string userName)
        {
            if (userName != User.UserName)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"Tài khoản [{userName}]", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            var userInfo = _userCache.GetByUserName(userName);
            if (userInfo == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"Tài khoản [{userName}]", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            userInfo.AvatarPath = string.IsNullOrEmpty(userInfo.Avatar)
                ? null
                : $"{_avatarFolderPath}/{userInfo.UserId}/{userInfo.Avatar}";
            userInfo.Phone = !string.IsNullOrEmpty(userInfo.Phone)
                ? "(+84) " + userInfo.Phone.Substring(1, userInfo.Phone.Length - 1)
                : string.Empty;
            return PartialView("_EditInfo", userInfo);
        }

        /// <summary>
        /// Lưu thông tin tài khoản sau khi người dùng cập nhật.
        /// </summary>
        /// <param name="model">Dữ liệu tài khoản cần cập nhật.</param>
        /// <returns>Kết quả xử lý cập nhật thông tin tài khoản.</returns>
        [HttpPost]
        [AllowAnyPermission]
        public ActionResult EditInfo(SysUserModel model)
        {
            if (!string.IsNullOrEmpty(model.Phone))
            {
                string cleanedPhoneNumber = model.Phone.Replace("(+84)", "0").Replace(" ", "").Replace("_", "");
                if (!IsPhoneNumberValid(cleanedPhoneNumber))
                    ModelState.AddModelError("Phone", string.Format(
                        AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"),
                        AppProcessor.Messagor.GetMessage("SoDienThoai")));
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                string cleanedEmail = model.Email.Replace("_", "");
                if (!IsMailValid(cleanedEmail))
                    ModelState.AddModelError("Email", string.Format(
                        AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"), "Email"));
            }

            model.AvatarPath = string.IsNullOrEmpty(model.Avatar)
                ? null
                : $"{_avatarFolderPath}/{model.UserId}/{model.Avatar}";

            if (!ModelState.IsValid) return PartialView("_UserInfo", model);

            string fileName = null;
            if (model.AvatarFileBase != null)
            {
                Guid myGuid = Guid.NewGuid();
                fileName = myGuid.ToString() + Path.GetExtension(model.AvatarFileBase.FileName);
                model.Avatar = fileName;
            }

            model.Phone = string.IsNullOrEmpty(model.Phone)
                ? string.Empty
                : model.Phone.Replace("(+84)", "0").Replace(" ", "");

            var userId = _userCache.UpdateInfo(model, User.UserName);
            switch (userId)
            {
                case -4:
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage($"<b>[{model.Email}]</b>", EnumProcessType.DataExisted, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                case -9:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage($"Tài khoản [{model.UserName}] không tồn tại hoặc đã khoá.",
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                case -1:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage($"Cập nhật thông tin tài khoản [{model.UserName}]",
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                default:
                    SaveAvatar(model.AvatarFileBase, _avatarFolderPath, fileName, userId);
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage(
                            $"Cập nhật thông tin tài khoản [{model.UserName}]. Vui lòng đăng nhập lại để xem thay đổi thông tin.",
                            EnumProcessType.NonFormat, EnumMsgIcon.Success)
                    }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Đăng xuất tài khoản hiện tại khỏi hệ thống.
        /// </summary>
        /// <returns>Điều hướng về màn hình đăng nhập.</returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Logout()
        {
            // Lấy SSO Ticket trước khi xóa Session
            string ssoTicket = Session[SSO_TICKET_SESSION_KEY] as string;
            bool useSso = !string.IsNullOrWhiteSpace(ssoTicket);
            try
            {
                // Chỉ hủy ticket khi phiên hiện tại thực sự đăng nhập qua SSO.
                if (useSso && !string.IsNullOrWhiteSpace(ssoTicket))
                {
                    CreateSsoService().LogOutByTicket(ssoTicket);
                }
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
            }
            // Đăng xuất khỏi CRM
            FormsAuthentication.SignOut();
            // Xóa toàn bộ Session CRM
            Session.Clear();
            Session.Abandon();

            // Phiên đăng nhập CRM quay về màn hình chọn phương thức đăng nhập.
            if (!useSso)
                return RedirectToAction("Login", "Account");

            // Logout khỏi SSO
            string ssoLogoutUrl = string.Format("{0}?appcode={1}&do=logout", _ssoPortalUrl.TrimEnd('/'), Server.UrlEncode(appCode));
            return Redirect(ssoLogoutUrl);
        }

        /// <summary>
        /// Hiển thị popup quên mật khẩu.
        /// </summary>
        /// <returns>Popup quên mật khẩu.</returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return PartialView("_ForgotPassword", new LoginModel());
        }

        /// <summary>
        /// Xử lý gửi email yêu cầu đặt lại mật khẩu cho tài khoản theo email đã nhập.
        /// </summary>
        /// <param name="model">Thông tin email cần gửi yêu cầu đặt lại mật khẩu.</param>
        /// <returns>Kết quả xử lý gửi email đặt lại mật khẩu.</returns>
        [HttpPost]
        [AjaxOnly]
        [AllowAnonymous]
        public ActionResult ForgotPassword(LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_emailRequiredMessage, EnumProcessType.NonFormat, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            if (!UtilString.IsValidEmail(model.Email))
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_emailInvalidMessage, EnumProcessType.NonFormat, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            var userModel = _userCache.GetByEmail(model.Email);
            if (userModel == null)
                return Json(new
                {
                    status = false,
                    message = CreateMessage(_userInfoNotFoundMessage, EnumProcessType.NonFormat, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);

            var passPharse = userModel.Password;
            var newPassword = UtilString.GenerateStrongPassword(8);
            userModel.HostUrl = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "");
            var baseToken = $"{newPassword}-{DateTime.Now.AddHours(24).Ticks}";
            var tokenResetPassword = UtilEncrypt.Encrypt(baseToken, passPharse);
            userModel.DetailUrl = Url.Action("ResetPassword", "Account",
                new { area = "", userName = userModel.UserName, token = tokenResetPassword });

            var mailBodyHtml = RenderTemplateHtmlProvider.RenderPartialToHtml(
                HostingEnvironment.MapPath("~/Contents/Modules/Sys/EmailTemplates/_TemplateResetPassword.cshtml"),
                userModel);

            AppProcessor.Mailer.PushEmail(new List<MailModel>
            {
                new MailModel
                {
                    Subject         = $"[{AppProcessor.Messagor.GetMessage("App_Title")}] {AppProcessor.Messagor.GetMessage("MailSubject_ResetPassword_Message")}",
                    To              = new List<string> { model.Email },
                    Body            = mailBodyHtml,
                    IsBodyHtml      = true,
                    DisplayNameFrom = AppProcessor.Messagor.GetMessage("App_Owner_DisplayName"),
                    DicImgs         = new Dictionary<string, byte[]>
                    {
                        {
                            "LogoApp",
                            System.IO.File.ReadAllBytes(
                                $"{Server.MapPath(ConfigurationManager.AppSettings["Logo_App_Path"])}")
                        }
                    }
                }
            });

            return Json(new
            {
                status = true,
                message = CreateMessage(
                    string.Format(_resetPasswordEmailSentMessage, model.Email),
                    EnumProcessType.NonFormat, EnumMsgIcon.Success)
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Render thông tin tài khoản hiện tại để hiển thị trên menu.
        /// </summary>
        /// <returns>Chuỗi HTML thông tin tài khoản.</returns>
        [HttpGet]
        public string RenderAccountInfo()
        {
            var currentUserLogin = _userCache.GetByUserName(User.UserName);
            return RenderViewToString(ControllerContext, "_AccountInfoInMenu", currentUserLogin);
        }

        #region Extend Functions

        private void UpdateLoginFailCount(LoginModel model)
        {
            int requestCount;
            if (Session[SESSION_VARIABLE_NAME] == null)
            {
                requestCount = 1;
                Session[SESSION_VARIABLE_NAME] = 1;
            }
            else
            {
                int currentCount = (int)Session[SESSION_VARIABLE_NAME];
                currentCount++;
                Session[SESSION_VARIABLE_NAME] = currentCount;
                requestCount = currentCount;
            }

            model.LoginFailCount = requestCount;
        }

        private void ApplyLoginCredentials(LoginModel model)
        {
            if (!string.IsNullOrEmpty(model.URLLink))
            {
                string value = model.URLLink;
                string key = value.Substring(0, 16);
                value = value.Replace(key, ",");
                string[] data = value.TrimStart(',').Split(',');

                string userName = AESEncrytDecryProvider.DecryptStringAES(data[0], data[2], data[3]);
                string password = AESEncrytDecryProvider.DecryptStringAES(data[1], data[2], data[3]);

                model.UserName = userName.Trim();
                model.Password = password.Trim();
            }

            model.UserName = NormalizeVNPTUsername(model.UserName);
        }

        private bool TryAuthenticateUser(
            LoginModel model,
            out bool isVNPTAccount,
            out SysUserModel vnptUser,
            out string loginFailMessage)
        {
            vnptUser = null;
            loginFailMessage = AppProcessor.Messagor.GetMessage("Authorize_LoginIncorrect");

            string emailFull = model.UserName.Trim().ToLower();
            isVNPTAccount = emailFull.EndsWith(_vnptEmailExtension);

            if (!isVNPTAccount)
            {
                return AppProcessor.Author.IsValidUser(model.UserName, model.Password);
            }

            bool isAuthen = _vnptProvider.ValidateUser(emailFull, model.Password);
            if (!isAuthen)
            {
                return false;
            }

            vnptUser = _userCache.GetByEmail(emailFull);
            if (vnptUser == null)
            {
                loginFailMessage = _vnptAccountNotConfiguredMessage;
                return false;
            }

            EnsureVNPTPermissions(vnptUser);
            model.UserName = vnptUser.UserName;
            return true;
        }

        private void EnsureVNPTPermissions(SysUserModel vnptUser)
        {
            List<SysModuleModel> userModules = _moduleCache.GetByUserName(vnptUser.UserName);
            List<SysRoleModel> userRoles = _userCache.GetRoles(vnptUser.UserId);

            if (userModules == null || userModules.Count == 0 || userRoles == null || userRoles.Count == 0)
            {
                string roleIDs = userRoles != null && userRoles.Count > 0
                    ? string.Join(",", userRoles.Select(item => item.RoleId))
                    : VNPT_DEFAULT_ROLE_ID;
                string moduleIDs = userModules != null && userModules.Count > 0
                    ? string.Join(",", userModules.Select(item => item.ModuleId))
                    : VNPT_DEFAULT_MODULE_ID;

                _userCache.Permit(vnptUser.UserId, roleIDs, moduleIDs, null);
            }
        }

        private AppPrincipalSerializeModel BuildLoginUser(
            string userName,
            bool isVNPTAccount,
            SysUserModel vnptUser)
        {
            string avatarFolderPath = ConfigurationManager.AppSettings["AppAvatarFolder_Path"];

            if (isVNPTAccount && vnptUser != null)
            {
                AppProcessor.Author.GetUserInfo(vnptUser.UserName);

                return new AppPrincipalSerializeModel
                {
                    UserId = vnptUser.UserId ?? 0,
                    FullName = vnptUser.FullName,
                    UserName = vnptUser.UserName,
                    Email = vnptUser.Email,
                    Avatar = string.IsNullOrEmpty(vnptUser.Avatar)
                        ? _defaultAvatar
                        : string.Concat(avatarFolderPath, "/", vnptUser.UserId, "/", vnptUser.Avatar)
                };
            }

            // Fix: Get SysUserModel from _userCache instead of AppProcessor.Author.GetUserInfo
            SysUserModel userOnline = _userCache.GetByUserName(userName);

            return new AppPrincipalSerializeModel
            {
                UserId = userOnline.UserId ?? 0,
                FullName = userOnline.FullName,
                UserName = userOnline.UserName,
                Email = userOnline.Email,
                Avatar = string.IsNullOrEmpty(userOnline.Avatar)
                    ? _defaultAvatar
                    : string.Concat(avatarFolderPath, "/", userOnline.UserId, "/", userOnline.Avatar)
            };
        }

        private void SignInUser(AppPrincipalSerializeModel loginUser)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string userData = serializer.Serialize(loginUser);

            FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                1,
                loginUser.UserName,
                DateTime.Now,
                DateTime.Now.AddMinutes(30),
                true,
                userData);

            string encTicket = FormsAuthentication.Encrypt(authTicket);
            HttpCookie faCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
            Request.RequestContext.HttpContext.Response.Cookies.Add(faCookie);
        }

        private void SaveAvatar(HttpPostedFileBase avatarFileBase, string virtualAvatarFolderPath,
            string fileName, int? employeeId)
        {
            if (avatarFileBase == null || employeeId <= 0 ||
                string.IsNullOrEmpty(virtualAvatarFolderPath) || string.IsNullOrEmpty(fileName)) return;

            var absoluteAvatarFolderPath = HostingEnvironment.MapPath(virtualAvatarFolderPath);
            if (string.IsNullOrEmpty(absoluteAvatarFolderPath)) return;

            absoluteAvatarFolderPath = Path.Combine(absoluteAvatarFolderPath, employeeId.ToString());
            if (!Directory.Exists(absoluteAvatarFolderPath))
                Directory.CreateDirectory(absoluteAvatarFolderPath);

            avatarFileBase.SaveAs(Path.Combine(absoluteAvatarFolderPath, fileName));
        }

        private bool IsPhoneNumberValid(string phoneNumber)
        {
            return Regex.IsMatch(phoneNumber, @"^\d{10}$");
        }

        private bool IsMailValid(string mail)
        {
            return Regex.IsMatch(mail, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$");
        }

        // Chuẩn hóa tên đăng nhập VNPT thành định dạng email đầy đủ nếu người dùng chỉ nhập phần tên mà không có phần đuôi "@vnpt.vn"
        private string NormalizeVNPTUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return username;

            username = username.Trim().ToLower();

            if (username.Contains("@"))
                return username;

            return username + _vnptEmailExtension;
        }
        #endregion
    }
}
