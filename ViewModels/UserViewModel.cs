using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace TextFileExport.ViewModels
{
    public class UserViewModel
    {
        public Models.DbConnection DbConnection { get; set; }
        public UserViewModel()
        {
            DbConnection = new Models.DbConnection(@"localhost\SQLEXPRESS", "CPM","root","root","TRMS001");
        }
    }
}
