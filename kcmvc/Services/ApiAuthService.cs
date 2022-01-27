namespace kcmvc.Services
{
    public interface IApiAuthService
    {
        bool ValidateApiRequest(string username, string password);
        string GetApiIngestUserName();
    }

    public class ApiAuthService : IApiAuthService
    {
        public ApiAuthService()
        {
        }

        public bool ValidateApiRequest(string username, string password)
        {
            return username == "ychung-svc" && password == "password";
        }

        public string GetApiIngestUserName()
        {
            return "Young-Jin Chung";
        }
    }
}