using System;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using TSFramework.Libs.Principals;
using TSFramework.Libs.Processors;

namespace TSFramework.Libs.BaseApps
{
    public class BaseHttpApplication : HttpApplication
    {
        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception != null && exception.InnerException != null)
            {
                LogInnerException(exception.InnerException);
            }

            if (exception != null)
            {
                AppProcessor.Logger.Error(exception);
            }

            var currentPath = Request?.Url?.AbsolutePath ?? "";
            if (currentPath.IndexOf("/Error/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Đang ở trang Error mà lại phát sinh lỗi, tránh chuyển hướng vô tận (ERR_TOO_MANY_REDIRECTS)
                Server.ClearError();
                return;
            }

            Response.Clear();
            var httpException = exception as HttpException;
            string sErrorUrlRedirect;

            if (httpException == null)
                sErrorUrlRedirect = "~/Error/Error";
            else //It's an Http Exception, Let's handle it.
                switch (httpException.GetHttpCode())
                {
                    case 404:
                        sErrorUrlRedirect = "~/Error/NotFound";
                        break;
                    case 500:
                        sErrorUrlRedirect = "~/Error/Error";
                        break;
                    default:
                        sErrorUrlRedirect = "~/Error/AccessDenied";
                        break;
                }

            // Clear the error on server.
            Server.ClearError();
            Response.Redirect(sErrorUrlRedirect, true);
            // Avoid IIS7 getting in the middle
            Response.TrySkipIisCustomErrors = true;
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            try
            {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie == null || string.IsNullOrWhiteSpace(authCookie.Value)) return;

                FormsAuthenticationTicket authTicket = null;
                try
                {
                    authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                }
                catch
                {
                    // Khi AppPool recycle hoặc đổi machineKey, cookie cũ không giải mã được.
                    // Xóa cookie cũ để tránh quăng lỗi unhandled dẫn đến vòng lặp chuyển hướng.
                    ExpireAuthCookie();
                    return;
                }

                if (authTicket == null || string.IsNullOrEmpty(authTicket.UserData))
                {
                    ExpireAuthCookie();
                    return;
                }

                AppPrincipalSerializeModel serializeModel = null;
                try
                {
                    serializeModel = JsonConvert.DeserializeObject<AppPrincipalSerializeModel>(authTicket.UserData);
                }
                catch
                {
                    ExpireAuthCookie();
                    return;
                }

                if (serializeModel == null)
                {
                    ExpireAuthCookie();
                    return;
                }

                var userPrincipal = new AppPrincipal(authTicket.Name)
                {
                    UserId = serializeModel.UserId,
                    FullName = serializeModel.FullName,
                    UserName = serializeModel.UserName,
                    Email = serializeModel.Email,
                    Avatar = serializeModel.Avatar,
                    Token = serializeModel.Token,
                    CreatedDate = serializeModel.CreatedDate
                };
                BaseAppContext.Current.User = userPrincipal;
                HttpContext.Current.User = userPrincipal;
            }
            catch (Exception ex)
            {
                AppProcessor.Logger.Error(ex);
                ExpireAuthCookie();
            }
        }

        private void ExpireAuthCookie()
        {
            try
            {
                FormsAuthentication.SignOut();
                var cookieName = FormsAuthentication.FormsCookieName;
                if (Response.Cookies[cookieName] != null)
                {
                    Response.Cookies[cookieName].Value = "";
                    Response.Cookies[cookieName].Expires = DateTime.Now.AddDays(-1);
                }
                else
                {
                    var expiredCookie = new HttpCookie(cookieName, "")
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    };
                    Response.Cookies.Add(expiredCookie);
                }
            }
            catch { }
        }

        private void LogInnerException(Exception ex)
        {
            if (ex != null)
            {
                AppProcessor.Logger.Error(ex);
                if (ex.InnerException != null)
                {
                    LogInnerException(ex.InnerException);
                }
            }
        }
    }
}