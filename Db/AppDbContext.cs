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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TextFileExport.Db
{
    public partial class AppDbContext : DbContext
    {

        private string defaultAlarmTableName;
        public string DefaultAlarmTableName => defaultAlarmTableName ?? $"Alarms_{Properties.Settings.Default.PLCName}";

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
                optionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomModelCacheKeyFactory>();

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Alarms>(entity =>
            {
                //entity.HasNoKey();

                entity.ToTable(DefaultAlarmTableName);

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
        class CustomModelCacheKeyFactory : IModelCacheKeyFactory
        {
            public object Create(DbContext context) => new CustomModelCacheKey(context);
        }

        class CustomModelCacheKey
        {
            (Type ContextType, string CustomTableName) key;
            public CustomModelCacheKey(DbContext context)
            {
                key.ContextType = context.GetType();
                key.CustomTableName = (context as AppDbContext)?.DefaultAlarmTableName;
            }
            public override int GetHashCode() => key.GetHashCode();
            public override bool Equals(object obj) => obj is CustomModelCacheKey other && key.Equals(other.key);
        }
    }
}
