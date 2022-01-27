using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using kcmvc.Autentication;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin;
using System;

namespace kcmvc
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(Constants.AwpAuthType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = Constants.AwpAuthType
            });

            app.UseAwpAuthentication();

        }
    }
}
