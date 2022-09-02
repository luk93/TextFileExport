using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            dbTables.Add(new DbTable($"Warnings_{plcName}", "W_Warnings"));
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
                            table.UpdateDb = true;
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
        public static async Task UpdateInDatabase(ObservableCollection<DbTable> dbTables, TextBlock tb, ProgressBar pb1, ProgressBar pb2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();
            pb1.Maximum = dbTables.Count() - 1;
            pb1.Value = 0;
            foreach (var table in dbTables)
            {
                if (table.UpdateDb && table.Name.Contains("Alarms"))
                {
                    pb2.Maximum = table.AlarmRecords.Count();
                    pb2.Value = 0;
                    stopwatch.Reset();
                    stopwatch.Start();
                    foreach (var alarmRecord in table.AlarmRecords)
                    {
                        var dbRecord = context.Alarmss
                            .Where(x => x.Id == alarmRecord.Id)
                            .SingleOrDefault();
                        if (dbRecord != null)
                        {
                            context.Alarmss.Update(alarmRecord);
                            alarmRecord.Status = "DB Updated";
                        }
                        else
                        {
                            await context.Alarmss.AddAsync(alarmRecord);
                            alarmRecord.Status = "DB Inserted";
                        }
                        pb2.Value++;
                    }
                    await context.SaveChangesAsync();
                    stopwatch.Stop();
                    MainWindow.TextblockAddLine(tb, $"Task finished in: {stopwatch.ElapsedMilliseconds} ms\n");
                    MainWindow.TextblockAddLine(tb, $"{table.PrintDbData()}");
                }
                pb1.Value++;
            }
        }
        public async static Task UpdateInDatabaseAnother(ObservableCollection<DbTable> dbTables, TextBlock tb,
                                                         ProgressBar pb1, ProgressBar pb2, IProgress<int> progress1, IProgress<int> progress2)
        {
            Stopwatch stopwatch = new();
            using var context = new AppDbContext();
            var i = 0;
            await Task.Run(() => progress1.Report(i));
            var j = 0;
            await Task.Run(() => progress2.Report(j));
            pb1.Maximum = dbTables.Count()-1;
            foreach (var table in dbTables)
            {
                pb2.Maximum = table.AlarmRecords.Count();
                if (table.UpdateDb && table.Name.Contains("Alarms"))
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    var ids = table.AlarmRecords.Select(c => c.IdAlarm);
                    var dbRecords = context.Alarmss
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
                            context.Alarmss.Update(dbRecord);
                            alarmRecord.Status = "DB Updated";
                        }
                        else
                        {
                            context.Alarmss.Add(alarmRecord);
                            alarmRecord.Status = "DB Inserted";
                        }
                        i++;
                        await Task.Run(() => progress2.Report(i));
                    }
                    await context.SaveChangesAsync();
                    stopwatch.Stop();
                    MainWindow.TextblockAddLine(tb, $"{table.PrintDbData()}, Time: {stopwatch.ElapsedMilliseconds}ms\n");
                }
                j++;
                await Task.Run(() => progress2.Report(j));

            }
        }

    }
}

