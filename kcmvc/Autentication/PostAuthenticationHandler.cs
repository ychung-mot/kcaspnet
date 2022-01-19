using kcmvc.Services;
using Microsoft.Owin;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace kcmvc.Autentication
{
    public class PostAuthenticationHandler : OwinMiddleware
    {
        private IUserService _userService;

        public PostAuthenticationHandler(OwinMiddleware next) : base(next)
        {
            _userService = DependencyResolver.Current.GetService<IUserService>();
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.User.Identity.IsAuthenticated)
            {
                Debug.WriteLine(context.Request.User.Identity.Name);
                Debug.WriteLine(_userService.GetUserRole(context.Request.User.Identity.Name));
            }

            await Next.Invoke(context);
        }
    }
}