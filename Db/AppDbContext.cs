using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

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

        public virtual DbSet<AlarmsTrms001> AlarmsTrms001s { get; set; } = null!;
        public virtual DbSet<MessagesTrms001> MessagesTrms001s { get; set; } = null!;
        public virtual DbSet<WarningsTrms001> WarningsTrms001s { get; set; } = null!;

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
                    .UseSqlServer("Data Source = localhost\\SQLEXPRESS; Database = CPM; User ID = root; Password = root; Encrypt=False");

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlarmsTrms001>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Alarms_TRMS001");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<MessagesTrms001>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Messages_TRMS001");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<WarningsTrms001>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Warnings_TRMS001");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
