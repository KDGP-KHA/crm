using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Core.Cate.Caches;
using Core.Sys.BaseApp;
using Core.Sys.Caches.Sys;
using Core.Sys.Models.Sys;
using Modules.Sys.Areas.Sys.Data;
using TSFramework.Libs.Attributes;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Models.Base;
using TSFramework.Libs.Models.Mail;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Providers;
using TSFramework.Libs.Utils;

namespace Modules.Sys.Areas.Sys.Controllers
{
    public class UserController : AppController
    {
        private readonly string _avatarFolderPath = ConfigurationManager.AppSettings["AppAvatarFolder_Path"];
        private readonly SysConfigCache _configsCache = new SysConfigCache();
        private readonly SysModuleCache _moduleCache = new SysModuleCache();
        private readonly SysRoleCache _roleCache = new SysRoleCache();
        private readonly MN_BoPhanCache _boPhanCache = new MN_BoPhanCache();
        private readonly SysUserBoPhanCache _userBoPhanCache = new SysUserBoPhanCache();
        private readonly string _roleTitle = AppProcessor.Messagor.GetMessage("Role_Title");
        private readonly SysUserCache _userCache = new SysUserCache();
        private readonly string _userTitle = AppProcessor.Messagor.GetMessage("User_Title");

        // GET: User
        [ActionType(Type = EnumActionType.View)]
        [HttpGet]
        public ActionResult Index()
        {
            var model = new SysUserSearchModel();
            //model.ListBoPhans = _boPhanCache.GetAll()
            //    .Select(x => new SelectListItem
            //    {
            //        Text = x.TitleView,
            //        Value = x.BoPhan_ID.ToString()
            //    }).ToList();
            //model.ListChucVus = _chucVuCache.GetAll()
            //    .Select(x => new SelectListItem
            //    {
            //        Text = x.TenChucVu,
            //        Value = x.ChucVu_ID.ToString()
            //    }).ToList();
            return View(new SysUserSearchModel());
        }

        #region Extend Functions
        private void SaveAvatar(HttpPostedFileBase avatarFileBase, string virtualAvatarFolderPath, string fileName, int? employeeId)
        {
            if (avatarFileBase == null || employeeId <= 0 || string.IsNullOrEmpty(virtualAvatarFolderPath) || string.IsNullOrEmpty(fileName)) return;
            var absoluteAvatarFolderPath = HostingEnvironment.MapPath(virtualAvatarFolderPath);
            if (string.IsNullOrEmpty(absoluteAvatarFolderPath)) return;

            absoluteAvatarFolderPath = Path.Combine(absoluteAvatarFolderPath, employeeId.ToString());

            if (!Directory.Exists(absoluteAvatarFolderPath)) Directory.CreateDirectory(absoluteAvatarFolderPath);
            avatarFileBase.SaveAs(Path.Combine(absoluteAvatarFolderPath, fileName));
        }

        /// <summary>
        /// Kiểm tra số điện thoại hợp lệ
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        bool IsPhoneNumberValid(string phoneNumber)
        {
            string pattern = @"^\d{10}$";  // Regular expression pattern for a 10-digit phone number
            return Regex.IsMatch(phoneNumber, pattern);
        }

        /// <summary>
        /// Kiểm tra mail hợp lệ
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        bool IsMailValid(string mail)
        {
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";  // Biểu thức chính quy cho địa chỉ email
            return Regex.IsMatch(mail, pattern);
        }

        /// <summary>
        /// Kiểm tra ten tai khoan hợp lệ
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        bool IsValidUsername(string username)
        {
            string pattern = "^[a-zA-Z0-9_]+$";  // Biểu thức chính quy cho các ký tự chữ cái, chữ số và dấu gạch dưới
            return Regex.IsMatch(username, pattern);
        }
        #endregion

        #region Main Action

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult Get(SysUserSearchModel model)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);

            var searchCondititions = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };
            var lstUsers = _userCache.Get(out int total, model, searchCondititions);
            lstUsers.ForEach(u =>
            {
                u.AvatarPath = string.IsNullOrEmpty(u.Avatar)
                    ? "/Contents/Base/imgs/avatar-default.png"
                    : System.IO.File.Exists(Server.MapPath($"{_avatarFolderPath}/{u.UserId.ToString()}/{u.Avatar}")) ? $"{_avatarFolderPath}/{u.UserId.ToString()}/{u.Avatar}" : "/Contents/Base/imgs/avatar-default.png";
            });

            var result = Json(
                new { draw = Convert.ToInt32(draw), recordsTotal = total, recordsFiltered = total, data = lstUsers },
                JsonRequestBehavior.AllowGet);
            return result;
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult Add()
        {
            var model = new SysUserModel { ListRoles = _roleCache.GetAll() };
            return PartialView("_Add", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult Add(SysUserModel model)
        {
            // kiểm tra số điện thoại
            if (!string.IsNullOrEmpty(model.Phone))
            {
                // Xóa chuỗi "(+84)" và khoảng trắng và dấu _
                string cleanedPhoneNumber = model.Phone.Replace("(+84)", "0").Replace(" ", "").Replace("_", "");
                if (IsPhoneNumberValid(cleanedPhoneNumber) == false)
                {
                    ModelState.AddModelError("Phone", string.Format(AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"), AppProcessor.Messagor.GetMessage("SoDienThoai")));
                }
            }
            // kiểm tra mail
            if (!string.IsNullOrEmpty(model.Email))
            {
                // Xóa khoảng trắng và dấu _
                string cleanedEmail = model.Email.Replace("_", "");
                if (IsMailValid(cleanedEmail) == false)
                {
                    ModelState.AddModelError("Email", string.Format(AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"), "Email"));
                }
            }
            // kiểm tra tên tài khoản
            if (!string.IsNullOrEmpty(model.UserName))
            {
                if (IsValidUsername(model.UserName) == false)
                {
                    ModelState.AddModelError("UserName", string.Format(AppProcessor.Messagor.GetMessage("UserName_DataNotCorrect_Message"), AppProcessor.Messagor.GetMessage("Authorize_UserName")));
                }
            }


            if (!ModelState.IsValid)
            {
                model.ListRoles = _roleCache.GetAll();
                return PartialView("_User", model);
            }

            model.RoleIDs = string.IsNullOrEmpty(model.RoleIDs) ? null : model.RoleIDs.Trim(',');
            model.Password = _configsCache.GetViaKey("PassDefault")?.ConfigValue;

            //var mailNewUser = new SysMailUserModel
            //{
            //    FullName = model.FullName,
            //    UserName = model.UserName,
            //    Email = model.Email,
            //    Password = model.Password,
            //    HostUrl = Request.Url?.Host,
            //    SupportEmail = _configsCache.GetViaKey("Email_Support")?.ConfigValue
            //};

            var salt = UtilEncrypt.GenerateSalt();
            var passwordHash = UtilEncrypt.GenerateCryptoPassword(model.Password, salt);

            string fileName = null;
            if (model.AvatarFileBase != null)
            {
                // Create a new Guid
                Guid myGuid = Guid.NewGuid();
                fileName = myGuid.ToString() + Path.GetExtension(model.AvatarFileBase.FileName);
            }

            var userId = _userCache.Save(new SysUserModel
            {
                UserId = 0,
                FullName = model.FullName,
                UserName = model.UserName,
                Email = model.Email,
                Password = passwordHash,
                Salt = salt,
                Avatar = fileName,
                Phone = string.IsNullOrEmpty(model.Phone) ? null : model.Phone.Replace("(+84)", "0").Replace(" ", "").Replace("_", ""),
                RoleIDs = model.RoleIDs,
                IsActive = true,
                Reason = "Thêm mới",
                //HostlUrl = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "")
            }, User.UserName);
            if (userId == -9)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle} <b>[{model.UserName}]</b>", EnumProcessType.DataExisted,
                        EnumMsgIcon.Error)
                });
            if (userId == -10)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle} <b>[{model.Email}]</b>", EnumProcessType.DataExisted,
                        EnumMsgIcon.Error)
                });
            if (userId > 0)
            {
                SaveAvatar(model.AvatarFileBase, _avatarFolderPath, fileName, userId);

                //var dataHtml = RenderTemplateHtmlProvider.RenderPartialToHtml(
                //    HostingEnvironment.MapPath(ConfigurationManager.AppSettings["EmailTemplates_TemplateNewUser"]),
                //    mailNewUser);

                //AppProcessor.Mailer.PushEmail(new List<MailModel>
                //{
                //    new MailModel
                //    {
                //        From = null,
                //        DisplayNameFrom = null,
                //        Subject =
                //            $"[{AppProcessor.Messagor.GetMessage("App_Title")}] Thông tin tài khoản {model.FullName}",
                //        To = new List<string> { model.Email },
                //        IsBodyHtml = true,
                //        Body = dataHtml,
                //        DicImgs = new Dictionary<string, byte[]>
                //        {
                //            {
                //                "LogoVNPT",
                //                System.IO.File.ReadAllBytes(
                //                    $"{Server.MapPath(ConfigurationManager.AppSettings["Logo_VNPT_Path"])}")
                //            },
                //            {
                //                "LogoApp",
                //                System.IO.File.ReadAllBytes(
                //                    $"{Server.MapPath(ConfigurationManager.AppSettings["Logo_App_Path"])}")
                //            }
                //        }
                //    }
                //});
            }

            var response = CreateMessage($"{_userTitle} [{model.FullName}]", EnumProcessType.Add,
                userId > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(int id = 0)
        {
            var model = _userCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            model.RoleIDs = string.Join(",", _userCache.GetRoles(model.UserId).Select(g => g.RoleId));
            model.ListRoles = _roleCache.GetAll();
            model.AvatarPath = string.IsNullOrEmpty(model.Avatar)
                ? null
                : $"{_avatarFolderPath}/{model.UserId.ToString()}/{model.Avatar}";
            model.Phone = !string.IsNullOrEmpty(model.Phone) ? "(+84) " + model.Phone.Substring(1, model.Phone.Length - 1) : string.Empty;
            return PartialView("_Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Edit(SysUserModel model)
        {
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            // kiểm tra số điện thoại
            if (!string.IsNullOrEmpty(model.Phone))
            {
                // Xóa chuỗi "(+84)" và khoảng trắng và dấu _
                string cleanedPhoneNumber = model.Phone.Replace("(+84)", "0").Replace(" ", "").Replace("_", "");
                if (IsPhoneNumberValid(cleanedPhoneNumber) == false)
                {
                    ModelState.AddModelError("Phone", string.Format(AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"), AppProcessor.Messagor.GetMessage("SoDienThoai")));
                }
            }
            // kiểm tra mail
            if (!string.IsNullOrEmpty(model.Email))
            {
                // Xóa khoảng trắng và dấu _
                string cleanedEmail = model.Email.Replace("_", "");
                if (IsMailValid(cleanedEmail) == false)
                {
                    ModelState.AddModelError("Email", string.Format(AppProcessor.Messagor.GetMessage("DataNotCorrect_Message"), "Email"));
                }
            }

            if (ModelState.IsValid)
            {
                int? userId;
                model.RoleIDs = string.IsNullOrEmpty(model.RoleIDs) ? null : model.RoleIDs.Trim(',');

                string fileName = null;
                if (model.AvatarFileBase != null)
                {
                    // Create a new Guid
                    Guid myGuid = Guid.NewGuid();
                    fileName = myGuid.ToString() + Path.GetExtension(model.AvatarFileBase.FileName);
                }
                else
                {
                    fileName = model.Avatar;
                }

                model.Phone = model.Phone != null ? model.Phone.Replace("(+84)", "0").Replace(" ", "").Replace("_", "") : null;
                if (!string.IsNullOrEmpty(model.Password))
                    userId = _userCache.Save(new SysUserModel
                    {
                        UserId = model.UserId,
                        FullName = model.FullName,
                        UserName = model.UserName,
                        Email = model.Email,
                        Phone = model.Phone,
                        RoleIDs = model.RoleIDs,
                        IsActive = true,
                        Reason = model.Reason,
                    }, User.UserName);
                else
                    userId = _userCache.Save(new SysUserModel
                    {
                        UserId = model.UserId,
                        FullName = model.FullName,
                        UserName = model.UserName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Password = null,
                        Salt = null,
                        Avatar = fileName,
                        RoleIDs = model.RoleIDs,
                        IsActive = true,
                        Reason = model.Reason,
                    }, User.UserName);
                if (userId == -9)
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage($"{_userTitle} <b>[{model.UserName}]</b>", EnumProcessType.DataExisted,
                            EnumMsgIcon.Error)
                    });
                if (userId == -10)
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage($"{_userTitle} <b>[{model.Email}]</b>", EnumProcessType.DataExisted,
                            EnumMsgIcon.Error)
                    });
                SaveAvatar(model.AvatarFileBase, _avatarFolderPath, fileName, userId);

                var response = CreateMessage($"{_userTitle} [{model.FullName}]", EnumProcessType.Edit,
                    userId > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }
            model.ListRoles = _roleCache.GetAll();
            model.AvatarPath = string.IsNullOrEmpty(model.Avatar) ? null : $"{_avatarFolderPath}/{model.UserId.ToString()}/{model.Avatar}";
            return PartialView("_User", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(int id = 0)
        {
            var model = _userCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                $"<b>{_userTitle} [{model.FullName}]</b>");
            model.Reason = string.Empty;
            return PartialView("_Delete", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult Delete(SysUserModel model)
        {
            if (ModelState.IsValidField("Reason"))
            {
                var isSuccess = _userCache.Delete(model, User.UserName);

                var response = CreateMessage($"Xóa <b>{_userTitle} [{model.FullName}]</b> thành công",
                    EnumProcessType.NonFormat, isSuccess ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            ViewBag.ConfirmMessage = $"Bạn muốn xóa <b>{_userTitle} [{model.FullName}]</b>";
            return PartialView("_DeleteBody", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.View)]
        public ActionResult IsExistUser(string userName, int userId)
        {
            var model = _userCache.GetByUserName(userName);
            if (model == null)
                return Json(new
                {
                    status = false,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            if (userId < 1)
                return Json(new
                {
                    status = false
                });
            if (model.UserId != userId)
                return Json(new
                {
                    status = false
                });
            return Json(new
            {
                status = true
            });
        }

        [HttpGet]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangePassword(int id = 0)
        {
            var currentUser = _userCache.GetById(id);
            if (currentUser == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage("Tài khoản",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                }, JsonRequestBehavior.AllowGet);
            return PartialView("_ChangePassword", new ChangePasswordModel
            {
                FullName = currentUser.FullName,
                UserName = currentUser.UserName,
                Email = currentUser.Email
            });
        }

        [HttpPost]
        [AjaxOnly]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            ModelState.Remove("CurrentPassword");
            if (!ModelState.IsValid) return PartialView("_Password", model);
            if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\W)"))
            {
                ModelState.AddModelError("NewPassword",
                    "Mật khẩu phải ít nhất có 1 chữ hoa, 1 chữ thường và một kí tự đặt biệt");
                return PartialView("_Password", model);
            }

            var salt = UtilEncrypt.GenerateSalt();
            var passwordHash = UtilEncrypt.GenerateCryptoPassword(model.NewPassword, salt);

            var userId = _userCache.ResetPassword(
                model.UserName,
                passwordHash,
                salt,
                model.Reason,
                User.UserName
            );
            switch (userId)
            {
                case -1:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage("Tài khoản không tồn tại hoặc đã khoá.",
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                default:
                    //AppProcessor.Notifider.ForceLogout(model.UserName);
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage("Mật khẩu", EnumProcessType.Edit, EnumMsgIcon.Success)
                    }, JsonRequestBehavior.AllowGet);
            }
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ResetPassword(int id = 0)
        {
            var model = _userCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            if (!model.IsActive)
                return Json(new
                {
                    status = true,
                    message = CreateMessage(
                        $"{_userTitle} <b>[{model.FullName} - {model.UserName}]</b> đã ngưng hoạt động.",
                        EnumProcessType.NonFormat, EnumMsgIcon.Error)
                });
            model.Reason = "Reset password";
            ViewBag.ConfirmMessage =
                $"Bạn muốn đặt lại mật khẩu cho tài khoản <b>[{model.FullName} - {model.UserName}]</b>?";
            return PartialView("_ResetPassword", model);
        }

        [AjaxOnly]
        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult ResetPassword(SysUserModel model)
        {
            var userModel = _userCache.GetById(model.UserId.GetValueOrDefault(0));
            if (userModel == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            if (!userModel.IsActive)
                return Json(new
                {
                    status = true,
                    message = CreateMessage(
                        $"{_userTitle} <b>[{model.FullName} - {model.UserName}]</b> đã ngưng hoạt động.",
                        EnumProcessType.NonFormat, EnumMsgIcon.Error)
                });

            model.Password = _configsCache.GetViaKey("PassDefault")?.ConfigValue;
            var salt = UtilEncrypt.GenerateSalt();
            var passwordHash = UtilEncrypt.GenerateCryptoPassword(model.Password, salt);

            var userId = _userCache.ResetPassword(
                model.UserName,
                passwordHash,
                salt,
                "Đặt lại mật khẩu",
                User.UserName
            );
            switch (userId)
            {
                case -1:
                    return Json(new
                    {
                        status = false,
                        message = CreateMessage("Tài khoản không tồn tại hoặc đã khoá.",
                            EnumProcessType.NonFormat, EnumMsgIcon.Error)
                    }, JsonRequestBehavior.AllowGet);
                default:
                    return Json(new
                    {
                        status = true,
                        message = CreateMessage(AppProcessor.Messagor.GetMessage("Modal_Title_SendEmail") + $" {_userTitle} <b>[{model.FullName} - {model.UserName}]</b> thành công",
                            EnumProcessType.NonFormat, EnumMsgIcon.Success)
                    }, JsonRequestBehavior.AllowGet);
            }
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeActive(int id = 0)
        {
            var model = _userCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            model.Reason = string.Empty;
            ViewBag.ConfirmMessage = $"Bạn muốn ngưng hoạt động <b>{_userTitle} [{model.FullName}]</b> ?";
            return PartialView("_DeActive", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult DeActive(SysUserModel model)
        {
            if (ModelState.IsValidField("Reason"))
            {
                var isSuccess = _userCache.DeActive(model, User.UserName);

                var response = CreateMessage($"Ngưng hoạt động <b>{_userTitle} [{model.FullName}]</b> thành công",
                    EnumProcessType.NonFormat, isSuccess ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            ViewBag.ConfirmMessage = $"Bạn muốn ngưng hoạt động <b>{_userTitle} [{model.FullName}]</b>";
            return PartialView("_DeActiveBody", model);
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Active(int id = 0)
        {
            var model = _userCache.GetById(id);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            ViewBag.ConfirmMessage = $"Bạn muốn kích hoạt lại <b>{_userTitle} [{model.FullName}]</b>";
            return PartialView("_Active", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Active(SysUserModel model)
        {
            if (ModelState.IsValidField("Reason"))
            {
                var isSuccess = _userCache.Active(model, User.UserName);

                var response = CreateMessage($"Kích hoạt <b>{_userTitle} [{model.FullName}]</b> thành công",
                    EnumProcessType.NonFormat, isSuccess ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            ViewBag.ConfirmMessage = $"Bạn muốn kích hoạt lại <b>{_userTitle} [{model.FullName}]</b>";
            return PartialView("_ActiveBody", model);
        }

        #endregion

        #region User Via Role

        [AjaxOnly]
        [ActionType(Type = EnumActionType.View)]
        [HttpPost]
        public ActionResult GetUsersViaRole(int? roleId)
        {
            var search = Request.Form.GetValues("search[value]")?[0];
            var draw = Request.Form.GetValues("draw")?[0];
            var order = Request.Form.GetValues("order[0][column]")?[0];
            var orderDir = Request.Form.GetValues("order[0][dir]")?[0];
            var startRec = Convert.ToInt32(Request.Form.GetValues("start")?[0]);
            var pageSize = Convert.ToInt32(Request.Form.GetValues("length")?[0]);

            var searchCondititions = new BaseSearchModel
            {
                Search = string.IsNullOrEmpty(search) ? null : search,
                Order = order,
                OrderDir = orderDir,
                StartIndex = startRec,
                PageSize = pageSize
            };

            var dataUsers = _userCache.GetViaRole(roleId, out int total, searchCondititions);

            var result =
                Json(
                    new
                    {
                        draw = Convert.ToInt32(draw),
                        recordsTotal = total,
                        recordsFiltered = total,
                        data = dataUsers
                    }, JsonRequestBehavior.AllowGet);
            return result;
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult UsersViaRole(int roleId)
        {
            var model = _roleCache.GetById(roleId);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_roleTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            return PartialView("_UsersViaRole", model);
        }

        [AjaxOnly]
        [ActionType(Type = EnumActionType.Create)]
        [HttpGet]
        public ActionResult AddUser(int roleId)
        {
            var model = _roleCache.GetById(roleId);
            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_roleTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            var lstSelectedUsers = _userCache.GetViaRole(roleId, out _);
            model.ListUsers = _userCache.GetAll()
                //.Where(u => !lstSelectedUsers.Exists(su => su.UserId == u.UserId))
                .ToList();
            model.Users = string.Join(",", lstSelectedUsers.Select(u => u.UserId));

            return PartialView("_AddUser", model);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Create)]
        public ActionResult AddUser(SysRoleModel model)
        {
            if (!ModelState.IsValid)
            {
                var lstSelectedUsers = _userCache.GetViaRole(model.RoleId, out _);
                model.ListUsers = _userCache.GetAll().Where(u => !lstSelectedUsers.Exists(su => su.UserId == u.UserId))
                    .ToList();
                return PartialView("_UserRole", model);
            }

            var roleId = _roleCache.AddUsers(new SysRoleModel
            {
                RoleId = model.RoleId,
                Users = model.Users
                //Users = string.Join(",", model.SelectedUsers)
            });

            var response = CreateMessage(
                string.Format(AppProcessor.Messagor.GetMessage("Add_User_To_Role"), $"[{model.Name}]"),
                EnumProcessType.Add,
                roleId > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
            return Json(new { status = true, message = response });
        }

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult RemoveUser(int roleId, int userId)
        {
            var userModel = _userCache.GetById(userId);
            if (userModel == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            var roleModel = _roleCache.GetById(roleId);
            if (roleModel == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_roleTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            roleModel.UserId = userId;

            ViewBag.ConfirmMessage = string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Delete"),
                string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Remove_From_Role"),
                    $"<b>[{userModel.FullName}]</b>", $"<b>[{roleModel.Name}]</b>"));
            return PartialView("_RemoveUser", roleModel);
        }

        [HttpPost]
        [ActionType(Type = EnumActionType.Delete)]
        public ActionResult RemoveUser(SysRoleModel model)
        {
            var isSuccess = _roleCache.RemoveUser(model.RoleId, model.UserId);
            var userModel = _userCache.GetById(model.UserId);
            var roleModel = _roleCache.GetById(model.RoleId);

            var response = CreateMessage(
                string.Format(AppProcessor.Messagor.GetMessage("Message_Confirm_Remove_From_Role"),
                    $"<b>[{userModel.FullName}]</b>", $"<b>[{roleModel.Name}]</b>"), EnumProcessType.Delete,
                isSuccess ? EnumMsgIcon.Success : EnumMsgIcon.Error);

            return Json(new { status = true, message = response });
        }

        #endregion

        #region Permit

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Permit(int id = 0)
        {
            var userModel = _userCache.GetById(id);
            if (userModel == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            var userPermitModel = new UserPermitModel
            {
                Email = userModel.Email,
                FullName = userModel.FullName,
                UserName = userModel.UserName,
                UserId = userModel.UserId,
                OfficeName = userModel.OfficeName,

                RoleIDs = string.Join(",", _userCache.GetRoles(userModel.UserId).Select(g => g.RoleId)),
                ListRoles = _roleCache.GetAll(),

                ModuleIDs = string.Join(",", _moduleCache.GetByUserName(userModel.UserName).Select(g => g.ModuleId)),
                ListModules = _moduleCache.GetAll(),

                //TypeServiceIDs = string.Join(",", new Cate_TypeServiceCache().GetByUserName(userModel.UserName).Select(g => g.TypeServiceId)),
                //ListTypeService = new Cate_TypeServiceCache().GetAll()
            };

            return PartialView("_Permit", userPermitModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult Permit(UserPermitModel model)
        {
            if (ModelState.IsValid)
            {
                var retInt = _userCache.Permit(model.UserId, model.RoleIDs, model.ModuleIDs, model.TypeServiceIDs);
                var response =
                    CreateMessage(
                        $"{AppProcessor.Messagor.GetMessage("Modal_Title_Accessibility")} - {_userTitle} [{model.FullName}]",
                        EnumProcessType.Edit, retInt > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);
                return Json(new { status = true, message = response });
            }

            model.ListRoles = _roleCache.GetAll();
            model.ListModules = _moduleCache.GetAll();
          //  model.ListTypeService = new Cate_TypeServiceCache().GetAll();

            return PartialView("_PermitBody", model);
        }

        #endregion
        #region Phân đơn vị

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult BoPhan(int id = 0)
        {
            var userModel = _userCache.GetById(id);
            if (userModel == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}", EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });

            var assignedList = _userBoPhanCache.GetByEmail(userModel.Email);
            var assignedMaCodes = string.Join(",", assignedList.ConvertAll(b => b.MaBoPhan));
            var allBophans = _boPhanCache.GetAll()
                .Where(b => b.Da_Xoa == false)
                .ToList();

            var model = new UserBoPhanModel
            {
                UserId = userModel.UserId,
                FullName = userModel.FullName,
                UserName = userModel.UserName,
                Email = userModel.Email,
                MaBophans = assignedMaCodes,
                ListBophans = allBophans
            };

            return PartialView("_BoPhan", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult BoPhan(UserBoPhanModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListBophans = _boPhanCache.GetAll()
                    .Where(b => b.Da_Xoa == false)
                    .ToList();
                return PartialView("_BoPhanBody", model);
            }

            var maBophans = string.IsNullOrWhiteSpace(model.MaBophans)
                ? null
                : string.Join(",", model.MaBophans
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            var result = _userBoPhanCache.Save(
                model.Email,
                maBophans,
                User.UserName);

            var response = CreateMessage(
                $"Phân đơn vị - {_userTitle} [{model.FullName}]",
                EnumProcessType.Edit,
                result > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);

            return Json(new { status = result > 0, message = response });
        }

        #endregion

        #region Phân quyền rà soát

        [AjaxOnly]
        [HttpGet]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult PermitReview(int id = 0)
        {
            var userModel = _userCache.GetById(id);
            var allBophans = _boPhanCache.GetAll();

            var model = new UserPermitReviewModel
            {
                UserId = userModel.UserId,
                FullName = userModel.FullName,
                UserName = userModel.UserName,
                Email = userModel.Email,
                ReviewDepartment = userModel.ReviewDepartment,
                ReviewLevel = userModel.ReviewLevel,
                ListBophans = allBophans
            };

            if (model == null)
                return Json(new
                {
                    status = true,
                    message = CreateMessage($"{_userTitle}",
                        EnumProcessType.DataNotExist, EnumMsgIcon.Error)
                });
            return PartialView("_PermitReview", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionType(Type = EnumActionType.Edit)]
        public ActionResult PermitReview(UserPermitReviewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ListBophans = _boPhanCache.GetAll();
                return PartialView("_PermitReviewBody", model);
            }

            var result = _userBoPhanCache.PermitReview(model, User.UserName);

            var response = CreateMessage(
                $"Phân quyền rà soát - {_userTitle} [{model.FullName}]",
                EnumProcessType.Edit,
                result > 0 ? EnumMsgIcon.Success : EnumMsgIcon.Error);

            return Json(new { status = true, message = response });
        }

        #endregion
    }
}