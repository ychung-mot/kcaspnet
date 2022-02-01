using kcmvc.Services;
using Microsoft.Owin;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace kcmvc.Autentication
{
    public class PostAuthenticationHandler : OwinMiddleware
    {
        private IUserManagerService _userManagerService;

        public PostAuthenticationHandler(OwinMiddleware next) : base(next)
        {
            _userManagerService = DependencyResolver.Current.GetService<IUserManagerService>();
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.User.Identity.IsAuthenticated)
            {
                var user = (ClaimsPrincipal)context.Request.User;

                var username = (user.FindFirst(ClaimTypes.Name)?.Value ?? "").ToLowerInvariant().Trim();

                var identityProvider = (user.FindFirst("identity_provider")?.Value ?? "").ToLowerInvariant().Trim();
                var userid = "";

                switch (identityProvider)
                {
                    case "idir":
                        userid = user.FindFirst("idir_userid")?.Value ?? "";
                        break;
                    case "bceid-business":
                        userid = user.FindFirst("bceid_userid")?.Value ?? "";
                        break;
                    default:
                        userid = _userManagerService.GetUserIdentifierByBusinessId(username)?.Guid.ToString("N");
                        break;
                }

                var userGuid = userid == null || userid == "" ? Guid.Empty : new Guid(userid);
                var identifier = _userManagerService.GetSecurityIdentifierByGUID(userGuid);
                var roles = _userManagerService.GetRolesBySecurityIdentifierId(identifier.SecurityIdentifierId);

                foreach (var role in roles)
                {
                    user.Identities.FirstOrDefault().AddClaim(new Claim(ClaimTypes.Role, role));
                }

                user.Identities.FirstOrDefault().AddClaim(new Claim(ClaimTypes.NameIdentifier, userid));
            }

            await Next.Invoke(context);
        }
    }
}