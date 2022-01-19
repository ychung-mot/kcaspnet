using kcmvc.Services;
using Microsoft.Owin;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
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
                var user = (ClaimsPrincipal)context.Request.User;

                var claims = new List<Claim>();

                //read DB to get permissions and add claims to the user
                claims.Add(new Claim("AWP_PERMISSION", "read"));

                claims.Add(new Claim(ClaimTypes.Name, user.Identity.Name));

                user.AddIdentity(new ClaimsIdentity(claims));
            }

            await Next.Invoke(context);
        }
    }
}