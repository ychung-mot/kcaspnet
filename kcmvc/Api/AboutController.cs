using System.Net;
using System.Net.Http;
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
            var response = new { Name = "KC Test Application", Version = "1.0" };
            return Request.CreateResponse(HttpStatusCode.OK, response, "application/json");
        }
    }
}