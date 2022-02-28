using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Owin.Security.Keycloak;
using System;
using System.Configuration;

namespace kcmvc.Autentication
{
    public static class AppBuilderExtension
    {
        public static IAppBuilder UseAwpAuthentication(this IAppBuilder app)
        {
            app.UseOAuthBearerTokens(new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/api/transfer/token"),
                Provider = new AppOAuthProvider("self"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(7),
            });

            app.UseKeycloakAuthentication(new KeycloakAuthenticationOptions
            {
                Realm = ConfigurationManager.AppSettings["KcRealm"],
                ClientId = ConfigurationManager.AppSettings["KcClientId"],
                ClientSecret = ConfigurationManager.AppSettings["KcClientSecret"],
                KeycloakUrl = ConfigurationManager.AppSettings["KcAuthUrl"],

                PostLogoutRedirectUrl = "/account/SignOutCallback",
                AuthenticationType = Constants.AwpAuthType,
                SignInAsAuthenticationType = Constants.AwpAuthType,

                EnableBearerTokenAuth = true,
                AllowUnsignedTokens = false,
                DisableIssuerSigningKeyValidation = false,
                DisableIssuerValidation = false,
                DisableAudienceValidation = false,
                TokenClockSkew = TimeSpan.FromSeconds(2),
                DisableAllRefreshTokenValidation = true, //Fix for Keycloak server v4.6-4.8
            });

            app.Use<PostAuthenticationHandler>();

            return app;
        }
    }
}