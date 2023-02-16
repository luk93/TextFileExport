using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TextFileExport.Db;
using TextFileExport.Extensions;
using TextFileExport.UI_Tools;

namespace TextFileExport.DataContainers
{
    public static class DbTablesTools
    {
        public static void FillTableWithData(ObservableCollection<DbTable> dbTables, string plcName)
        {
            if (dbTables.Count > 0) dbTables.Clear();
            //Hardcoded Names to Change according to Table names and WorksheetName
            dbTables.Add(new DbTable($"Alarms_{plcName}", "F_Faults"));
            dbTables.Add(new DbTable($"Messages_{plcName}", "S_Status"));
            //Hardcode Exclude of Warnings Table - project specification changed:
            //dbTables.Add(new DbTable($"Warnings_{plcName}", "W_Warnings"));
        }
        public static async Task LoadFromExcelFile(ObservableCollection<DbTable> dbTables, FileInfo file)
        {
            var package = new ExcelPackage(file);
            await package.LoadAsync(file);


            foreach (var table in dbTables)
            {

                if (table.AlarmRecords.Count >= 0) table.AlarmRecords.Clear();
                table.IsInWs = true;
                var ws = package.Workbook.Worksheets[table.WsName];
                int row = 6;
                int col = 2;
                if (ws == null)
                {
                    table.IsInWs = false;
                    return;
                }
                while (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
                {
                    if (!string.IsNullOrWhiteSpace(ws.Cells[row, col + 1].Value?.ToString()))
                    {
                        var idAlarmString = ws.Cells[row, col].Value.ToString()?[1..];
                        _ = int.TryParse(idAlarmString, out int idAlarm);
                        var status = (idAlarm <= 0) ? AlarmRecord.Status.WsNok : AlarmRecord.Status.WsOk;
                        AlarmRecord newObj = new()
                        {
                            IdAlarm = idAlarm,
                            Comment = (ws.Cells[row, col + 1].Value.ToString()),
                            RecordStatus = status
                        };
                        table.AlarmRecords.Add(newObj);
                    }
                    row++;
                }
            }
        }
        public static async Task UpdateInDatabase(ObservableCollection<DbTable> dbTables, TextBlock tb,
                                                         ProgressBar pb1, ProgressBar pb2, IProgress<int> progress1,
                                                         IProgress<int> progress2, ILoggerFactory loggerFactory)
        {
            Stopwatch stopwatch = new();
            await using var context = new AppDbContext(loggerFactory);

            var i = 0;
            await Task.Run(() => progress1.Report(i));
            pb1.Maximum = dbTables.Count;

            var j = 0;
            await Task.Run(() => progress2.Report(j));

            foreach (var table in dbTables)
            {
                switch (table.Name)
                {
                    case { } x when x.Contains("Alarms_"):
                        if (table.UpdateDb)
                            await UpdateAlarms(table, tb, pb2, progress2, loggerFactory);
                        break;
                    case { } x when x.Contains("Warnings_"):
                        if (table.UpdateDb)
                            await UpdateWarnings(table, tb, pb2, progress2, loggerFactory);
                        break;
                    case { } x when x.Contains("Messages_"):
                        if (table.UpdateDb)
                            await UpdateMessages(table, tb, pb2, progress2, loggerFactory);
                        break;
                    default:
                        break;
                }
                i++;
                await Task.Run(() => progress1.Report(i));
            }
        }
        public static async Task UpdateAlarms(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2, ILoggerFactory loggerFactory)
        {
            Stopwatch stopwatch = new();
            await using var context = new AppDbContext(loggerFactory);

            var j = 0;
            await Task.Run(() => progress2.Report(j));
            pb2.Maximum = table.AlarmRecords.Count;
            stopwatch.Reset();
            stopwatch.Start();

            var ids = table.AlarmRecords.Select(c => c.IdAlarm);
            var dbRecords = context.AlarmsSet
                 .Where(c => ids.Contains(c.IdAlarm))
                 .ToList();
            foreach (var alarmRecord in table.AlarmRecords)
            {
                var dbRecord = dbRecords
                    .SingleOrDefault(c => c.IdAlarm == alarmRecord.IdAlarm);
                if (dbRecord != null && dbRecord.Comment == alarmRecord.Comment)
                {
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbPassed;
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.AlarmsSet.Update(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbUpdated;
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.AlarmsSet.Add(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbInserted;
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            tb.AddLine($"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms");
        }
        public static async Task UpdateWarnings(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2, ILoggerFactory loggerFactory)
        {
            Stopwatch stopwatch = new();
            await using var context = new AppDbContext(loggerFactory);

            var j = 0;
            await Task.Run(() => progress2.Report(j));
            pb2.Maximum = table.AlarmRecords.Count;
            stopwatch.Reset();
            stopwatch.Start();

            var ids = table.AlarmRecords.Select(c => c.IdAlarm);
            var dbRecords = context.WarningsSet
                 .Where(c => ids.Contains(c.IdAlarm))
                 .ToList();
            foreach (var alarmRecord in table.AlarmRecords)
            {
                var dbRecord = dbRecords
                    .SingleOrDefault(c => c.IdAlarm == alarmRecord.IdAlarm);
                if (dbRecord != null && dbRecord.Comment == alarmRecord.Comment)
                {
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbPassed;
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.WarningsSet.Update(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbUpdated;
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.WarningsSet.Add(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbInserted;
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            tb.AddLine($"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms");
        }
        public static async Task UpdateMessages(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2, ILoggerFactory loggerFactory)
        {
            Stopwatch stopwatch = new();
            await using var context = new AppDbContext(loggerFactory);

            var j = 0;
            await Task.Run(() => progress2.Report(j));
            pb2.Maximum = table.AlarmRecords.Count;
            stopwatch.Reset();
            stopwatch.Start();

            var ids = table.AlarmRecords.Select(c => c.IdAlarm);
            var dbRecords = context.MessagesSet
                 .Where(c => ids.Contains(c.IdAlarm))
                 .ToList();
            foreach (var alarmRecord in table.AlarmRecords)
            {
                var dbRecord = dbRecords
                    .SingleOrDefault(c => c.IdAlarm == alarmRecord.IdAlarm);
                if (dbRecord != null && dbRecord.Comment == alarmRecord.Comment)
                {
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbPassed;
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.MessagesSet.Update(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbUpdated;
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.MessagesSet.Add(dbRecord);
                    alarmRecord.RecordStatus = AlarmRecord.Status.DbInserted;
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            tb.AddLine($"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms");
        }
        public static bool IsAnyTableReady(ObservableCollection<DbTable> dbTables)
        {

            foreach (var table in dbTables)
            {
                if (table.IsTableReadyToUpdateDb()) return true;
            }
            return false;
        }
    }
}

