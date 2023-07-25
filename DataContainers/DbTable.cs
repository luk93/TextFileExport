﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextFileExport.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TextFileExport.DataContainers
{
    public class DbTable
    {
        public string Name { get; set; }
        public bool IsInDb { get; set; }
        public bool UpdateDb { get; set; }
        public string WsName { get; set; }
        public bool IsInWs { get; set; }
        public List<AlarmRecord> AlarmRecords { get; set; }
        public DbTable(string name, string wsName)
        {
            Name = name;
            AlarmRecords = new List<AlarmRecord>();
            IsInDb = false;
            UpdateDb = false;
            WsName = wsName;
        }
        public string PrintExcelData()
        {
            return $"Table Name: {Name}, WS Name: {WsName}, Texts got: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.WsOk)}/{AlarmRecords.Count}";
        }
        public string PrintDbData()
        {
            return $"Table Name: {Name}, Texts inserted: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.DbInserted)}/{AlarmRecords.Count}, " +
                $"Texts updated: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.DbUpdated)}/{AlarmRecords.Count} "+
                $"Texts passed: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.DbPassed)}/{AlarmRecords.Count}";
        }
        public bool AreDuplicates(TextBlock tb)
        {
            var distinctedList = AlarmRecords.Distinct().ToList();
            var repItemlist = new List<AlarmRecord>();
            foreach (var item in distinctedList)
            {
                if (AlarmRecords.Count(e => e.IdAlarm == item.IdAlarm) > 1)
                    repItemlist.Add(item);
            }

            if (repItemlist.Count <= 0) return false;
            foreach (var duplicate in repItemlist)
                tb.AddLine($"Duplicated Id found in Sheet: {WsName}! Id:{duplicate.IdAlarm}");
            return true;

        }
        public bool IsTableReadyToUpdateDb()
        {
            return IsInDb && UpdateDb && AlarmRecords.Count > 0;
        }
        public string GenerateMergeQuery()
        {
            var query = $"--Merge into table query (Part 1) for {Name}\nMERGE INTO {Name} AS target\nUSING (VALUES";
            int linesCount = 9;
            int queryPartCount = 1;
            AlarmRecords.ForEach((record,info) =>
            {
                //SQL Insert Query maximum lines equals 1000
                if (linesCount >= 1000 || info.IsLast)
                {
                    query += $"\n\t({record.IdAlarm}, '{record.Comment}')";
                    query += ")\nAS source (IdAlarm, Comment)\nON target.IdAlarm = source.IdAlarm\nWHEN MATCHED THEN";
                    query += "\n\tUPDATE SET target.Comment = source.Comment\nWHEN NOT MATCHED THEN\n\tINSERT (IdAlarm, Comment)\n\tVALUES (source.IdAlarm, source.Comment);\n\n";
                    if (linesCount >= 1000 && !info.IsLast)
                    {
                        queryPartCount++;
                        query += $"--Merge into table query (Part {queryPartCount}) for {Name}\nMERGE INTO {Name} AS target\nUSING (VALUES";
                        linesCount = 9;
                    }
                }
                else
                {
                    query += $"\n\t({record.IdAlarm}, '{record.Comment}'),";
                    linesCount++;
                }
            });
            return query;
        }
    }
}
