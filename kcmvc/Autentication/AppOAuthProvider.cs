using kcmvc.Services;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace kcmvc.Autentication
{
    public class AppOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;
        private IApiAuthService _apiAuthService;

        public AppOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException(nameof(publicClientId));
            }

            _publicClientId = publicClientId;

            var apiAuthService = DependencyResolver.Current.GetService<IApiAuthService>();
            _apiAuthService = apiAuthService;
        }

        /// <summary>  
        /// Grant resource owner credentials overload method.  
        /// </summary>  
        /// <param name="context">Context parameter</param>  
        /// <returns>Returns when task is completed</returns>  
        public override Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            if (String.IsNullOrEmpty(context.UserName) || String.IsNullOrEmpty(context.Password))
            {
                context.SetError("invalid_grant", "The user name or password is not set.");
            }

            // Validate password
            if (!_apiAuthService.ValidateApiRequest(context.UserName, context.Password))
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
            }

            // Initialization.  
            var claims = new List<Claim>();

            // Setting  
            claims.Add(new Claim(ClaimTypes.Name, context.UserName));
            claims.Add(new Claim(ClaimTypes.Role, "DataIngest"));
            //claims.Add(new Claim(ClaimTypes.NameIdentifier, "E5468E6A96904CF59C2B6A8EA5286EBF"));
            claims.Add(new Claim("identity_provider", "awp_idprovider"));

            // Setting Claim Identities for OAUTH 2 protocol.  
            ClaimsIdentity oAuthClaimIdentity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);

            // Setting user authentication.  
            var propertyDictionary = new Dictionary<string, string> {
                { ClaimTypes.Name, context.UserName },
                { ClaimTypes.Role, "DataIngest" }
            };
            var properties = new AuthenticationProperties(propertyDictionary);

            if (!context.HasError)
            {
                AuthenticationTicket ticket = new AuthenticationTicket(oAuthClaimIdentity, properties);

                context.Validated(ticket);
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>  
        /// Token endpoint override method  
        /// </summary>  
        /// <param name="context">Context parameter</param>  
        /// <returns>Returns when task is completed</returns>  
        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                // Adding.  
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            // Return info.  
            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            if (context.ClientId == null)
                context.Validated();

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                var expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                    context.Validated();
            }
            return Task.FromResult<object>(null);
        }
    }
}