using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace kcmvc.Api
{
    public class AboutController : ApiController
    {
        [Authorize]
        [Route("api/about")]
        [HttpGet]
        public HttpResponseMessage GetServerVariables()
        {
            var user = (ClaimsPrincipal)User;

            var response = new { Name = user.Identity.Name, HasReadPermission = user.HasClaim("AWP_PERMISSION", "read") };
            return Request.CreateResponse(HttpStatusCode.OK, response, "application/json");
        }
    }
}