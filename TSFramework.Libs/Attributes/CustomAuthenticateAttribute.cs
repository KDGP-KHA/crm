using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using TSFramework.Libs.Enums;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Principals;
using TSFramework.Libs.Processors;

namespace TSFramework.Libs.Attributes
{
    public class CustomAuthenticateAttribute : AuthorizeAttribute
    {
        protected virtual AppPrincipal User => HttpContext.Current.User as AppPrincipal;

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            RedirectToRouteResult routeData;
            if (AllowAnonymous(filterContext)) return;
            var apiActions = filterContext.ActionDescriptor
                .GetCustomAttributes(typeof(ApiActionAttribute), false);

            //if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            //{
            //    routeData = new RedirectToRouteResult(new RouteValueDictionary(
            //        new
            //        {
            //            area = "",
            //            controller = "Account",
            //            action = "Login"
            //        }));

            //    filterContext.Result = routeData;
            //}
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                var request = filterContext.HttpContext.Request;

                // AJAX → trả 401
                if (request.IsAjaxRequest())
                {
                    var response = filterContext.HttpContext.Response;

                    response.StatusCode = 401;
                    response.SuppressFormsAuthenticationRedirect = true; 
                    response.End();

                    return;
                }
                else
                {
                    // Request thường → redirect login
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(
                            new
                            {
                                area = "",
                                controller = "Account",
                                action = "Login",
                                returnUrl = request.RawUrl
                            }));
                }

                return; 
            }
            else
            {
                var actionAllowAnyPermission =
                    filterContext.ActionDescriptor.GetCustomAttributes(typeof(AllowAnyPermissionAttribute), false);
                var controllerAllowAnyPermission =
                    filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(
                        typeof(AllowAnyPermissionAttribute), false);

                if (!actionAllowAnyPermission.Any() && !controllerAllowAnyPermission.Any())
                {
                    var areaName = filterContext.Controller.ControllerContext.RouteData.DataTokens["area"] as string ?? filterContext.Controller.ControllerContext.RouteData.Values["area"] as string;
                    var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;

                    //var apiActions = filterContext.ActionDescriptor
                    //    .GetCustomAttributes(typeof(ApiActionAttribute), false);
                    //if(apiActions.Length > 0) return;

                    var actionTypes = filterContext.ActionDescriptor
                        .GetCustomAttributes(typeof(ActionTypeAttribute), false);
                    var currentActionType = actionTypes.Length > 0 ? actionTypes[0] as ActionTypeAttribute : null;

                    // Nguyên tắc phòng thủ ATTT: Default Deny.
                    // Nếu action có [ActionType] thì kiểm tra quyền tương ứng.
                    // Nếu action KHÔNG có [ActionType], mặc định yêu cầu tối thiểu quyền View của Controller này.
                    var actionTypeName = currentActionType != null
                        ? EnumHelper.GetDescription(currentActionType.Type)
                        : EnumHelper.GetDescription(EnumActionType.View);

                    if (User == null || !AppProcessor.Author.IsAllow(User.UserName, areaName, controllerName, actionTypeName))
                    {
                        if (filterContext.HttpContext.Request.IsAjaxRequest())
                        {
                            filterContext.HttpContext.Response.StatusCode = 403;
                            filterContext.Result = new JsonResult
                            {
                                Data = new { status = false, message = "Bạn không có quyền thực hiện thao tác này!" },
                                JsonRequestBehavior = JsonRequestBehavior.AllowGet
                            };
                        }
                        else
                        {
                            routeData = new RedirectToRouteResult
                            (new RouteValueDictionary
                            (
                                new
                                {
                                    area = "",
                                    controller = "Error",
                                    action = "AccessDenied"
                                }
                            ));
                            filterContext.Result = routeData;
                        }
                    }
                }
            }

            base.OnAuthorization(filterContext);
        }

        private static bool AllowAnonymous(AuthorizationContext filterContext)
        {
            return filterContext.ActionDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any()
                   || filterContext.ActionDescriptor.ControllerDescriptor
                       .GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
        }
    }
}