using System;
using System.Collections.Generic;

namespace kcmvc.Services
{
    public interface IUserManagerService
    {
        string GetUserRole(string userId);
        SecurityIdentifier GetUserIdentifierByBusinessId(string username);
        SecurityIdentifier GetSecurityIdentifierByGUID(Guid userGuid);
        List<string> GetRolesBySecurityIdentifierId(int securityIdentifierId);
    }
    public class UserManagerService : IUserManagerService
    {
        public List<string> GetRolesBySecurityIdentifierId(int securityIdentifierId)
        {
            return new List<string> { "Read", "Write" };
        }

        public SecurityIdentifier GetSecurityIdentifierByGUID(Guid userGuid)
        {
            return new SecurityIdentifier { SecurityIdentifierId = 1, Guid = new Guid("E5468E6A96904CF59C2B6A8EA5286EBF") };
        }

        public SecurityIdentifier GetUserIdentifierByBusinessId(string username)
        {
            return new SecurityIdentifier { Guid = new Guid("E5468E6A96904CF59C2B6A8EA5286EBF") };
        }

        public string GetUserRole(string userId)
        {
            return "admin";
        }
    }

    public class SecurityIdentifier
    {
        public int SecurityIdentifierId { get; set; }
        public Guid Guid { get; set; }
    }
}