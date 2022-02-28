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

            Console.WriteLine(filterContext.HttpContext.Request.Path);

            if (!context.Request.IsAuthenticated)
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Account/Login.cshtml",
                    ViewBag = { Title = "Login", RedirectUrl = filterContext.HttpContext.Request.Url.ToString() }                    
                };
            }
        }
    }
}