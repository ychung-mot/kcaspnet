using Owin;
using Owin.Security.Keycloak;
using System;

namespace kcmvc.Autentication
{
    public static class AppBuilderExtension
    {
        public static IAppBuilder UseAwpKeycloakAuthentication(this IAppBuilder app)
        {
            app.UseKeycloakAuthentication(new KeycloakAuthenticationOptions
            {
                Realm = "onestopauth-business",
                ClientId = "awp-2146",
                ClientSecret = "RoQ8AZfVzuqEDWHCqt8HJxuyRSIvo7cp",
                KeycloakUrl = "http://localhost:9030/auth",

                AuthenticationType = Constants.AwpAuthType,
                SignInAsAuthenticationType = Constants.AwpAuthType,

                EnableBearerTokenAuth = true,
                AllowUnsignedTokens = false,
                DisableIssuerSigningKeyValidation = false,
                DisableIssuerValidation = false,
                DisableAudienceValidation = true,
                TokenClockSkew = TimeSpan.FromSeconds(2),
                DisableAllRefreshTokenValidation = true, // Fix for Keycloak server v4.6-4.8,  overrides DisableRefreshTokenSignatureValidation. The content of Refresh token was changed. Refresh token should not be used by the client application other than sending it to the Keycloak server to get a new Access token (where Keycloak server will validate it) - therefore validation in client application can be skipped.
            });

            app.Use<PostAuthenticationHandler>();

            return app;
        }
    }
}