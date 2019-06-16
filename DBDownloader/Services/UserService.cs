using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DBDownloader.Services
{
    public class UserService
    {

        private static readonly Lazy<UserService> _instance =
            new Lazy<UserService>(() => new UserService());

        public static UserService Instance { get { return _instance.Value; } }

        private string username = string.Empty;
        private string password = string.Empty;

        private UserService()
        {
        }

        public void SetUserPassword(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public ICredentials GetNetworkCredential()
        {
            return new NetworkCredential(username, password);            
        }
    }
}
