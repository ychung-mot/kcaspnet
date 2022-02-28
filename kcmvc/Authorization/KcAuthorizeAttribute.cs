using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace kcmvc.Authorization
{
    public class KcAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var context = filterContext.HttpContext;

            if (!context.Request.IsAuthenticated)
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Account/Login.cshtml",
                    ViewBag = { Title = "Login" }
                };
            }
        }
    }
}