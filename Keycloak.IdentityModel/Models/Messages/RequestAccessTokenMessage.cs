﻿using System;
using System.Threading.Tasks;
using Keycloak.IdentityModel.Models.Configuration;
using Keycloak.IdentityModel.Models.Responses;
using Keycloak.IdentityModel.Utilities;

namespace Keycloak.IdentityModel.Models.Messages
{
    public class RequestAccessTokenMessage : GenericMessage<TokenResponse>
    {
        public RequestAccessTokenMessage(Uri baseUri, IKeycloakParameters options,
            AuthorizationResponse authResponse)
            : base(options)
        {
            if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
            if (authResponse == null) throw new ArgumentNullException(nameof(authResponse));

            BaseUri = baseUri;
            AuthResponse = authResponse;
        }

        protected Uri BaseUri { get; }
        private AuthorizationResponse AuthResponse { get; }

        public override async Task<TokenResponse> ExecuteAsync()
        {
            return new TokenResponse(await ExecuteHttpRequestAsync());
        }

        private async Task<string> ExecuteHttpRequestAsync()
        {
            var uriManager = await OidcDataManager.GetCachedContextAsync(Options);
            var response = await SendHttpPostRequest(uriManager.GetTokenEndpoint(),
                uriManager.BuildAccessTokenEndpointContent(BaseUri, AuthResponse.Code));
            return await response.Content.ReadAsStringAsync();
        }
    }
}