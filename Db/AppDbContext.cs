﻿using System;
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
using System.Windows.Navigation;

namespace TextFileExport.Db
{
    public partial class AppDbContext : DbContext
    {

        private readonly string? _defaultAlarmTableName;
        public string? DefaultAlarmTableName => _defaultAlarmTableName ?? $"Alarms_{Properties.Settings.Default.PLCName}";
        private readonly ILoggerFactory _loggerFactory;

        public AppDbContext(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _defaultAlarmTableName = null;
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Alarms> Alarms { get; set; } = null!;
        public virtual DbSet<Messages> Messages { get; set; } = null!;
        public virtual DbSet<Warnings> Warnings { get; set; } = null!;

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
                entity.ToTable(DefaultAlarmTableName);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Messages>(entity =>
            {
                entity.ToTable($"Messages_{Properties.Settings.Default.PLCName}");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Warnings>(entity =>
            {
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
            (Type ContextType, string? CustomTableName) key;
            public CustomModelCacheKey(DbContext context)
            {
                key.ContextType = context.GetType();
                key.CustomTableName = (context as AppDbContext)?.DefaultAlarmTableName;
            }
            public override int GetHashCode() => key.GetHashCode();
            public override bool Equals(object? obj) => obj is CustomModelCacheKey other && key.Equals(other.key);
        }
    }
}
