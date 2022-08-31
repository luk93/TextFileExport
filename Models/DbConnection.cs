using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextFileExport.Models
{
    public class DbConnection
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PlcName { get; set; }
        public DbConnection(string serverName, string dbNAme, string userName, string password, string plcName)
        {
            ServerName = serverName;
            DatabaseName = dbNAme;
            UserName = userName;
            Password = password;
            PlcName = plcName;
        }
    }
}
