using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
                if (ws != null)
                {
                    while (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Value?.ToString()))
                    {
                        if (!string.IsNullOrWhiteSpace(ws.Cells[row, col + 1].Value?.ToString()))
                        {
                            string status;
                            string idAlarmString;
                            idAlarmString = ws.Cells[row, col].Value.ToString()[1..];
                            _ = int.TryParse(idAlarmString, out int idAlarm);
                            status = (idAlarm <= 0) ? "WS NOK - Bad Id" : "WS OK";
                            AlarmRecord newObj = new()
                            {
                                IdAlarm = idAlarm,
                                Comment = (ws.Cells[row, col + 1].Value.ToString()),
                                Status = status
                            };
                            table.AlarmRecords.Add(newObj);
                        }
                        row++;
                    }
                }
                else
                {
                    table.IsInWs = false;
                }
            }
        }
        public async static Task UpdateInDatabase(ObservableCollection<DbTable> dbTables, TextBlock tb,
                                                         ProgressBar pb1, ProgressBar pb2, IProgress<int> progress1, IProgress<int> progress2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();

            var i = 0;
            await Task.Run(() => progress1.Report(i));
            pb1.Maximum = dbTables.Count;

            var j = 0;
            await Task.Run(() => progress2.Report(j));

            foreach (var table in dbTables)
            {
                switch (table.Name)
                {
                    case string x when x.Contains("Alarms_"):
                        if (table.UpdateDb)
                            await UpdateAlarms(table, tb,pb2,progress2);
                        break;
                    case string x when x.Contains("Warnings_"):
                        if (table.UpdateDb)
                            await UpdateWarnings(table, tb, pb2, progress2);
                        break;
                    case string x when x.Contains("Messages_"):
                        if (table.UpdateDb)
                            await UpdateMessages(table, tb, pb2, progress2);
                        break;
                    default:
                        break;
                }
                i++;
                await Task.Run(() => progress1.Report(i));
            }
        }
        public async static Task UpdateAlarms(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();

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
                    alarmRecord.Status = "DB Passed";
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.AlarmsSet.Update(dbRecord);
                    alarmRecord.Status = "DB Updated";
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.AlarmsSet.Add(dbRecord);
                    alarmRecord.Status = "DB Inserted";
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            UI_Tools.UIControlsExt.TextblockAddLine(tb, $"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms\n");
        }
        public async static Task UpdateWarnings(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();

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
                    alarmRecord.Status = "DB Passed";
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.WarningsSet.Update(dbRecord);
                    alarmRecord.Status = "DB Updated";
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.WarningsSet.Add(dbRecord);
                    alarmRecord.Status = "DB Inserted";
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            UI_Tools.UIControlsExt.TextblockAddLine(tb, $"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms\n");
        }
        public async static Task UpdateMessages(DbTable table, TextBlock tb, ProgressBar pb2, IProgress<int> progress2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();

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
                    alarmRecord.Status = "DB Passed";
                }
                else if (dbRecord != null)
                {
                    dbRecord.Comment = alarmRecord.Comment;
                    context.MessagesSet.Update(dbRecord);
                    alarmRecord.Status = "DB Updated";
                }
                else
                {
                    dbRecord = new()
                    {
                        IdAlarm = alarmRecord.IdAlarm,
                        Comment = alarmRecord.Comment
                    };
                    context.MessagesSet.Add(dbRecord);
                    alarmRecord.Status = "DB Inserted";
                }
                j++;
                await Task.Run(() => progress2.Report(j));
            }
            await context.SaveChangesAsync();
            stopwatch.Stop();
            UI_Tools.UIControlsExt.TextblockAddLine(tb, $"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms\n");
        }
        public static bool IsAnyTableReady(ObservableCollection<DbTable> dbTables)
        {
            
            foreach(var table in dbTables)
            {
                if(table.IsTableReadyToUpdateDB()) return true;
            }
            return false;
        }
    }
}

