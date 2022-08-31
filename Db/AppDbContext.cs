using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Linq;

namespace TextFileExport.Db
{
    public partial class AppDbContext : DbContext
    {
        public static readonly ILoggerFactory _loggerFactory = new NLogLoggerFactory();
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Alarms> Alarmss { get; set; } = null!;
        public virtual DbSet<Messages> Messagess { get; set; } = null!;
        public virtual DbSet<Warnings> Warningss { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. 
                //You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder
                    .LogTo(message => Debug.WriteLine(message))
                    .UseLoggerFactory(_loggerFactory)
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(Properties.Settings.Default.ConnSetting);

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Alarms>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable($"Alarms_{Properties.Settings.Default.PLCName}");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Messages>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable($"Messages_{Properties.Settings.Default.PLCName}");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Warnings>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable($"Warnings_{Properties.Settings.Default.PLCName}");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                await Database.OpenConnectionAsync();
                await Database.CloseConnectionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool TableExists(string tableName)
        {
            var sqlQ = $"SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            var conn = Database.GetDbConnection();
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
