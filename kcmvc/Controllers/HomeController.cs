using IdentityModel;
using IdentityModel.Client;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace kcmvc.Controllers
{
    public class HomeController : Controller
    {
        public string Realm { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string KeycloakUrl { get; set; }
        public string KeycloakTokenUrl { get; set; }
        public string KeycloakAuthUrl { get; set; }
        public string RedirectUri { get; set; }

        private static string Code { get; set; }
        private static string Token { get; set; }
        private static string TokenType { get; set; }
        private static string Message { get; set; } = "";

        private static readonly string State = CryptoRandom.CreateUniqueId();

        public HomeController()
        {
            Realm = ConfigurationManager.AppSettings["KcRealm"];
            ClientId = ConfigurationManager.AppSettings["KcClientId"];
            ClientSecret = ConfigurationManager.AppSettings["KcClientSecret"];
            KeycloakUrl = ConfigurationManager.AppSettings["KcAuthUrl"];
            KeycloakAuthUrl = $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/auth";
            KeycloakTokenUrl = $"{KeycloakUrl}/realms/{Realm}/protocol/openid-connect/token";
            RedirectUri = "https://localhost:44317/home/callback";
        }
        public ActionResult Index()
        {
            ViewBag.Message = Message;
            return View();
        }

        public RedirectResult Authorize()
        {
            var authUrl = 
                $"{KeycloakAuthUrl}?client_id={ClientId}&redirect_uri={RedirectUri}&response_type=code&response_mode=query&state={State}";

            return Redirect(authUrl);
        }

        public RedirectToRouteResult Callback(string code, string state)
        {
            if (State != state)
            {
                Message += "\n\nState not recognised. Cannot trust response.";
                return RedirectToAction("Index", "Home");
            }

            Code = code;

            Message += "\n\nApplication Authorized!";
            Message += $"\n<b>code:</b> {code}";
            Message += $"\n<b>state:</b> {State}";

            return RedirectToAction("Index", "Home");
        }

        public async Task<RedirectToRouteResult> GetToken()
        {
            if (Code == null)
            {
                Message += "\n\nNot ready! Authorize first.";
                return RedirectToAction("Index", "Home");
            }

            Message += "\n\nCalling token endpoint...";
            var tokenClient = new HttpClient();
            var tokenResponse = await tokenClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = KeycloakTokenUrl,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Code = Code,
                RedirectUri = RedirectUri,
            });

            if (tokenResponse.IsError)
            {
                ViewBag.Message += "\n\nToken request failed";
                return RedirectToAction("Index", "Home");
            }

            TokenType = tokenResponse.TokenType;
            Token = tokenResponse.AccessToken;
            Message += "\n\nToken Received!";
            Message += $"\n<b>access_token:</b> {tokenResponse.AccessToken}";
            Message += $"\n<b>refresh_token:</b> {tokenResponse.RefreshToken}";
            Message += $"\n<b>expires_in:</b> {tokenResponse.ExpiresIn}";
            Message += $"\n<b>token_type:</b> {tokenResponse.TokenType}";

            return RedirectToAction("Index", "Home");
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}