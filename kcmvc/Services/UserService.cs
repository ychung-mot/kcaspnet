namespace kcmvc.Services
{
    public interface IUserService
    {
        string GetUserRole(string userId);
    }
    public class UserService : IUserService
    {
        public string GetUserRole(string userId)
        {
            return "admin";
        }
    }
}