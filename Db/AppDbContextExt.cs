using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextFileExport.Db
{
    public class AppDbContextExt
    {
        public static async Task<bool> CanConnectAsync(AppDbContext context)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool TableExists(AppDbContext context, string tableName)
        {
            var sqlQ = $"SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var conn = context.Database.GetDbConnection();
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
