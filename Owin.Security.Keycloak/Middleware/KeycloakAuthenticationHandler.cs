﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Keycloak.IdentityModel;
using Keycloak.IdentityModel.Models.Responses;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace Owin.Security.Keycloak.Middleware
{
    internal class KeycloakAuthenticationHandler : AuthenticationHandler<KeycloakAuthenticationOptions>
    {
        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            // Bearer token authentication override
            if (Options.EnableBearerTokenAuth)
            {
                // Try to authenticate via bearer token auth
                if (Request.Headers.ContainsKey(Constants.BearerTokenHeader))
                {
                    var bearerAuthArr = Request.Headers[Constants.BearerTokenHeader].Trim().Split(new[] {' '}, 2);
                    if ((bearerAuthArr.Length == 2) && bearerAuthArr[0].ToLowerInvariant() == "bearer")
                    {
                        try
                        {
                            var authResponse = new TokenResponse(bearerAuthArr[1], null, null);
                            var kcIdentity = await KeycloakIdentity.ConvertFromTokenResponseAsync(Options, authResponse);
                            var identity = await kcIdentity.ToClaimsIdentityAsync();
                            SignInAsAuthentication(identity, null, Options.SignInAsAuthenticationType);
                            return new AuthenticationTicket(identity, new AuthenticationProperties());
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }

                // If bearer token auth is forced, skip standard auth
                if (Options.ForceBearerTokenAuth) return null;
            }

            return null;
        }

        public override async Task<bool> InvokeAsync()
        {
            // Check SignInAs identity for authentication update
            if (Context.Authentication.User.Identity.IsAuthenticated)
                await ValidateSignInAsIdentities();

            // Check for valid callback URI
            var callbackUri = await KeycloakIdentity.GenerateLoginCallbackUriAsync(Options, Request.Uri);
            if (!Options.ForceBearerTokenAuth && Request.Uri.GetLeftPart(UriPartial.Path) == callbackUri.ToString())
            {
                // Create authorization result from query
                var authResult = new AuthorizationResponse(Request.Uri.Query);

                // If the authorization response returned a "error" query parameter (instead of "code" + "state"), redirect to a configured URL.
                // This could occur if the login is aborted by the user.
                if (!authResult.IsSuccessfulResponse())
                {
                    HandleAuthError(authResult);
                    return true;
                }

                try
                {
                    // Validate passed state
                    var stateData = Global.StateCache.ReturnState(authResult.State);
                    if (stateData == null)
                        throw new BadRequestException("Invalid state: Please reattempt the request");

                    // Parse properties from state data
                    var properties =
                        stateData[Constants.CacheTypes.AuthenticationProperties] as AuthenticationProperties ??
                        new AuthenticationProperties();

                    // Process response
                    var kcIdentity =
                        await KeycloakIdentity.ConvertFromAuthResponseAsync(Options, authResult, Request.Uri);
                    var identity = await kcIdentity.ToClaimsIdentityAsync();
                    Context.Authentication.User.AddIdentity(identity);
                    SignInAsAuthentication(identity, properties, Options.SignInAsAuthenticationType);

                    // Redirect back to the original secured resource, if any
                    if (!string.IsNullOrWhiteSpace(properties.RedirectUri) &&
                        Uri.IsWellFormedUriString(properties.RedirectUri, UriKind.Absolute))
                    {
                        Response.Redirect(properties.RedirectUri);
                        return true;
                    }
                }
                catch (BadRequestException exception)
                {
                    await GenerateErrorResponseAsync(HttpStatusCode.BadRequest, "Bad Request", exception.Message);
                    return true;
                }
            }

            return false;
        }

        protected override async Task ApplyResponseGrantAsync()
        {
            if (Options.ForceBearerTokenAuth) return;

            var signout = Helper.LookupSignOut(Options.AuthenticationType, Options.AuthenticationMode);

            // Signout takes precedence
            if (signout != null)
            {
                await LogoutRedirectAsync();
            }
        }

        protected override async Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                // If bearer token auth is forced, keep returned 401
                if (Options.ForceBearerTokenAuth)
                {
                    await
                        GenerateUnauthorizedResponseAsync(
                            "Access Unauthorized: Requires valid bearer token authorization header");
                    return;
                }

                var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);
                if (challenge == null) return;

                await LoginRedirectAsync(challenge.Properties);
            }
        }

        #region Private Helper Functions

        private void SignInAsAuthentication(ClaimsIdentity identity, AuthenticationProperties authProperties = null,
            string signInAuthType = null)
        {
            if (!string.IsNullOrWhiteSpace(signInAuthType) && !signInAuthType.Equals(Options.AuthenticationType, StringComparison.OrdinalIgnoreCase)) return;

            var signInIdentity = signInAuthType != null
                ? new ClaimsIdentity(identity.Claims, signInAuthType, identity.NameClaimType, identity.RoleClaimType)
                : identity;

            if (string.IsNullOrWhiteSpace(signInIdentity.AuthenticationType)) return;

            if (authProperties == null)
            {
                authProperties = new AuthenticationProperties
                {
                    // TODO: Make these configurable
                    AllowRefresh = true,
                    IsPersistent = true,
                    ExpiresUtc = DateTime.Now.Add(Options.SignInAsAuthenticationExpiration)
                };
            }

            // Parse expiration date-time
            var expirations = new List<string>
            {
                identity.Claims.FirstOrDefault(c => c.Type == Constants.ClaimTypes.RefreshTokenExpiration)?.Value,
                identity.Claims.FirstOrDefault(c => c.Type == Constants.ClaimTypes.AccessTokenExpiration)?.Value
            };

            foreach (var expStr in expirations)
            {
                DateTime expDate;
                if (expStr == null ||
                    !DateTime.TryParse(expStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out expDate))
                    continue;
                authProperties.ExpiresUtc = expDate.Add(Options.TokenClockSkew);
                break;
            }

            Context.Authentication.SignIn(authProperties, signInIdentity);
        }

        private async Task ValidateSignInAsIdentities()
        {
            foreach (var origIdentity in Context.Authentication.User.Identities)
            {
                try
                {
                    if (!origIdentity.HasClaim(Constants.ClaimTypes.AuthenticationType, Options.AuthenticationType))
                        continue;
                    var kcIdentity = await KeycloakIdentity.ConvertFromClaimsIdentityAsync(Options, origIdentity);
                    if (!kcIdentity.IsTouched) continue;

                    // Replace identity if expired
                    var identity = await kcIdentity.ToClaimsIdentityAsync();
                    Context.Authentication.User = new ClaimsPrincipal(identity);
                    SignInAsAuthentication(identity, null, Options.SignInAsAuthenticationType);
                }
                catch (AuthenticationException)
                {
                    Context.Authentication.SignOut(origIdentity.AuthenticationType);
                }
                // ReSharper disable once RedundantCatchClause
                catch (Exception)
                {
                    // TODO: Some kind of exception logging, maybe log the user out
                    throw;
                }
            }
        }

        private async Task GenerateUnauthorizedResponseAsync(string errorMessage)
        {
            await GenerateErrorResponseAsync(Response, HttpStatusCode.Unauthorized, "Unauthorized", errorMessage);
        }

        private async Task GenerateErrorResponseAsync(HttpStatusCode statusCode, string reasonPhrase,
            string errorMessage)
        {
            await GenerateErrorResponseAsync(Response, statusCode, reasonPhrase, errorMessage);
        }

        private static async Task GenerateErrorResponseAsync(IOwinResponse response, HttpStatusCode statusCode,
            string reasonPhrase, string errorMessage)
        {
            // Generate error response
            var task = response.WriteAsync(errorMessage);
            response.StatusCode = (int) statusCode;
            response.ReasonPhrase = reasonPhrase;
            response.ContentType = "text/plain";
            await task;
        }

        /// <summary>
        /// Handle the Keycloak authentication redirect to the application when it has specified an error (query parameter)
        /// </summary>
        /// <param name="authResult"></param>
        private void HandleAuthError(AuthorizationResponse authResult)
        {
            //If a special Url has been configured to redirect to when an error was returned from Keycloak authentication, redirect the user to that Url with the error information from Keycloak as query parameters
            if (!string.IsNullOrWhiteSpace(Options.AuthResponseErrorRedirectUrl))
            {
                // Build authentication error redirect URL
                string authResponseErrorRedirectUrl = Options.AuthResponseErrorRedirectUrl;
                if (Uri.IsWellFormedUriString(authResponseErrorRedirectUrl, UriKind.Relative))
                {
                    // Relative address configured. Use the current application root address as parent.
                    string baseAddress = string.Format("{0}{1}", Request.Uri.GetLeftPart(UriPartial.Authority), Options.VirtualDirectory);
                    authResponseErrorRedirectUrl = baseAddress + authResponseErrorRedirectUrl;
                }

                if (!Uri.IsWellFormedUriString(authResponseErrorRedirectUrl, UriKind.RelativeOrAbsolute))
                    throw new Exception("Invalid AuthResponseErrorRedirectUrl option: Not a valid relative/absolute URL: " + authResponseErrorRedirectUrl);

                // Add query parameters
                var parameters = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(authResult.Error))
                    parameters.Add("error", authResult.Error);
                if (!string.IsNullOrEmpty(authResult.ErrorDescription))
                    parameters.Add("errordescription", authResult.ErrorDescription);
                if (!string.IsNullOrEmpty(authResult.ErrorUri))
                    parameters.Add("erroruri", authResult.ErrorUri);
                var urlEncodedParameters = new FormUrlEncodedContent(parameters);
                var authErrorQueryString = urlEncodedParameters.ReadAsStringAsync().Result;

                // Build auth error redirect address with error query parameters from Keycloak.
                var authErrorUri = new Uri(authResponseErrorRedirectUrl + (!string.IsNullOrEmpty(authErrorQueryString) ? "?" + authErrorQueryString : ""));

                Response.Redirect(authErrorUri.ToString());
            }
            else
            {
                // If configured the response from the authorization endpoint indicated error (query parameter), and AuthResponseErrorRedirectUrl setting is missing, throw an exception.
                authResult.ThrowIfError();
            }

        }

        #endregion

        #region OIDC Helper Functions

        private async Task LoginRedirectAsync(AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = Request.Uri.ToString();
            }

            // Create state
            var stateData = new Dictionary<string, object>
            {
                {Constants.CacheTypes.AuthenticationProperties, properties}
            };
            var state = Global.StateCache.CreateState(stateData);

            // Redirect response to login
            Response.Redirect((await KeycloakIdentity.GenerateLoginUriAsync(Options, Request.Uri, state)).ToString());
        }

        private async Task LogoutRedirectAsync()
        {
            // Redirect response to logout
            Response.Redirect(
                (await
                    KeycloakIdentity.GenerateLogoutUriAsync(Options, Request.Uri))
                    .ToString());
        }

        #endregion
    }
}