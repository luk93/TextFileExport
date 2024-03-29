﻿using Dapper;
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
    public static class AppDbContextExt
    {
        public static async Task<bool> CanConnectAsync(this AppDbContext context)
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
        public static bool TableExists(this AppDbContext context, string tableName)
        {
            var sqlQ = $"SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var conn = context.Database.GetDbConnection();
            {
                {
                    // Query - Dapper Lib
                    var count = conn.QueryAsync<int>(sqlQ).Result.FirstOrDefault();
                    return (count > 0);
                }
            }
            return false;
        }
        public static bool ColumnInTableExists(this AppDbContext context, string tableName, string columnName)
        {
            var sqlQ = $@"SELECT Count(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            var conn = context.Database.GetDbConnection();
            {
                {
                    // Query - Dapper Lib
                    var result = conn.QueryAsync<int>(sqlQ).Result.Single();
                    return result == 1;
                }
            }
            return false;
        }
    }
}
