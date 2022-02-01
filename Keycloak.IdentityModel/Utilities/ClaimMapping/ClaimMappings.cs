using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Keycloak.IdentityModel.Extensions;
using Newtonsoft.Json.Linq;

namespace Keycloak.IdentityModel.Utilities.ClaimMapping
{
    internal static class ClaimMappings
    {
        public static IEnumerable<ClaimLookup> AccessTokenMappings { get; } = new List<ClaimLookup>
        {
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.Audience,
                JSelectQuery = "aud"
            },
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.Issuer,
                JSelectQuery = "iss"
            },
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.IssuedAt,
                JSelectQuery = "iat",
                Transformation =
                    token => ((token.Value<double?>() ?? 1) - 1).ToDateTime().ToString(CultureInfo.InvariantCulture)
            },
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.AccessTokenExpiration,
                JSelectQuery = "exp",
                Transformation =
                    token => ((token.Value<double?>() ?? 1) - 1).ToDateTime().ToString(CultureInfo.InvariantCulture)
            },
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.SubjectId,
                JSelectQuery = "sub"
            },
            new ClaimLookup
            {
                ClaimName = ClaimTypes.Name,
                JSelectQuery = "preferred_username",
                Transformation =
                    token => ((token.Value<string>() ?? "@").Split('@')[0])
            },
            new ClaimLookup
            {
                ClaimName = ClaimTypes.GivenName,
                JSelectQuery = "given_name"
            },
            new ClaimLookup
            {
                ClaimName = ClaimTypes.Surname,
                JSelectQuery = "family_name"
            },
            new ClaimLookup
            {
                ClaimName = ClaimTypes.Email,
                JSelectQuery = "email"
            },
            new ClaimLookup
            {
                ClaimName = ClaimTypes.Role,
                JSelectQuery = "resource_access.{gid}.roles"
            },
            new ClaimLookup
            {
                ClaimName = "identity_provider",
                JSelectQuery = "identity_provider"
            },
            new ClaimLookup
            {
                ClaimName = "idir_userid",
                JSelectQuery = "idir_userid"
            },
            new ClaimLookup
            {
                ClaimName = "bceid_userid",
                JSelectQuery = "bceid_userid"
            },
            new ClaimLookup
            {
                ClaimName = "bceid_business_name",
                JSelectQuery = "bceid_business_name"
            },
            new ClaimLookup
            {
                ClaimName = "bceid_business_guid",
                JSelectQuery = "bceid_business_guid"
            },
        };

        public static IEnumerable<ClaimLookup> IdTokenMappings { get; } = new List<ClaimLookup>();

        public static IEnumerable<ClaimLookup> RefreshTokenMappings { get; } = new List<ClaimLookup>
        {
            new ClaimLookup
            {
                ClaimName = Constants.ClaimTypes.RefreshTokenExpiration,
                JSelectQuery = "exp",
                Transformation =
                    token => ((token.Value<double?>() ?? 1) - 1).ToDateTime().ToString(CultureInfo.InvariantCulture)
            }
        };
    }
}