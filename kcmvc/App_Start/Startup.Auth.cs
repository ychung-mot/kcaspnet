using System;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin.Security.Keycloak;

namespace kcmvc
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            const string persistentAuthType = "keycloak_auth";

            app.SetDefaultSignInAsAuthenticationType(persistentAuthType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = persistentAuthType
            });

            var desc = new AuthenticationDescription();
            desc.AuthenticationType = persistentAuthType;
            desc.Caption = persistentAuthType;

            app.UseKeycloakAuthentication(new KeycloakAuthenticationOptions
            {
                Realm = "onestopauth-business",
                ClientId = "hets",
                //ClientSecret = "8eb92690-8c0c-42ba-b1ac-106dd2d06a22",

                KeycloakUrl = "http://localhost:9030/auth",
                AuthenticationType = persistentAuthType,
                SignInAsAuthenticationType = persistentAuthType,

                AllowUnsignedTokens = false,
                DisableIssuerSigningKeyValidation = false,
                DisableIssuerValidation = false,
                DisableAudienceValidation = true,
                TokenClockSkew = TimeSpan.FromSeconds(2),
                DisableAllRefreshTokenValidation = true, // Fix for Keycloak server v4.6-4.8,  overrides DisableRefreshTokenSignatureValidation. The content of Refresh token was changed. Refresh token should not be used by the client application other than sending it to the Keycloak server to get a new Access token (where Keycloak server will validate it) - therefore validation in client application can be skipped.
            });
        }
    }
}
