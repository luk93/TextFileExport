using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TextFileExport.Db
{
    public class AppDbContextExt
    {
        public static async Task<bool> CanConnectAsync(AppDbContext _context)
        {
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool TableExists(AppDbContext _context, string tableName)
        {
            var sqlQ = $"SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var conn = _context.Database.GetDbConnection();
            {
                if (conn != null)
                {
                    // Query - Dapper Lib
                    var count = conn.QueryAsync<int>(sqlQ).Result.FirstOrDefault();
                    return (count > 0);
                }
            }
            return false;
        }
    }
}
