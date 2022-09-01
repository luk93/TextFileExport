using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
    }
}

