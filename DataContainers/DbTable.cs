using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextFileExport.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TextFileExport.DataContainers
{
    public class DbTable
    {
        private readonly ILogger<DbTable> _logger;
        public string Name { get; set; }
        public bool IsInDb { get; set; }
        public bool UpdateDb { get; set; }
        public string WsName { get; set; }
        public bool IsInWs { get; set; }
        public List<AlarmRecord> AlarmRecords { get; set; }
        public DbTable(string name, string wsName, ILoggerFactory loggerFactory)
        {
            Name = name;
            AlarmRecords = new List<AlarmRecord>();
            IsInDb = false;
            UpdateDb = false;
            WsName = wsName;
            _logger = loggerFactory.CreateLogger<DbTable>();
            _logger.LogInformation($"Created table: {name}");
        }
        public string PrintExcelData()
        {
            return $"Table Name: {Name}, WS Name: {WsName}, Texts got: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.WsOk)}/{AlarmRecords.Count}";
        }
        public string PrintDbData()
        {
            return $"Table Name: {Name}, Texts inserted: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.DbInserted)}/{AlarmRecords.Count}, " +
                $"Texts updated: {AlarmRecords.Count(item => item.RecordStatus == AlarmRecord.Status.DbUpdated)}/{AlarmRecords.Count} " +
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
            {
                var warnText = $"Duplicated Id found in Sheet: {WsName}! Id:{duplicate.IdAlarm}";
                tb.AddLine(warnText);
                _logger.LogWarning(warnText);
            }
            return true;

        }
        public bool IsTableReadyToUpdateDb()
        {
            return IsInDb && UpdateDb && AlarmRecords.Count > 0;
        }
        public string GenerateMergeQuery()
        {
            if (AlarmRecords.Count == 0)
                return string.Empty;
            var query = $"--Merge into table query (Part 1) for {Name}\nMERGE INTO {Name} AS target\nUSING (VALUES";
            int linesCount = 9;
            int linesAmount = 9;
            int queryPartCount = 1;
            AlarmRecords.ForEach((record, info) =>
            {
                //SQL Insert Query maximum lines equals 1000
                if (linesCount >= 1000 || info.IsLast)
                {
                    query += $"\n\t({record.IdAlarm}, '{PrepCommentForQuery(record)}')";
                    query += ")\nAS source (IdAlarm, Comment)\nON target.IdAlarm = source.IdAlarm\nWHEN MATCHED THEN";
                    query += "\n\tUPDATE SET target.Comment = source.Comment\nWHEN NOT MATCHED THEN\n\tINSERT (IdAlarm, Comment)\n\tVALUES (source.IdAlarm, source.Comment);\n\n";
                    if (linesCount >= 1000 && !info.IsLast)
                    {
                        queryPartCount++;
                        query += $"--Merge into table query (Part {queryPartCount}) for {Name}\nMERGE INTO {Name} AS target\nUSING (VALUES";
                        linesCount = 9;
                        linesAmount += 9;
                    }
                }
                else
                {
                    query += $"\n\t({record.IdAlarm}, '{PrepCommentForQuery(record)}'),";
                    linesCount++;
                    linesAmount++;
                }
            });
            _logger.LogInformation($"Merge query generated for {Name}. Lines: {linesAmount}");
            return query;
        }
        private string PrepCommentForQuery(AlarmRecord record)
        {
            if (record.Comment == null) return string.Empty;
            if (!record.Comment.Contains("'")) return record.Comment;
            _logger.LogWarning($"Table: {Name} AlarmId: {record.IdAlarm} Comment: {record.Comment}. Detected uncorrect char [']. Changed to even amount of it to correctly generate query!");
            return FixCharsInString(record.Comment);

        }
        private string FixCharsInString(string text)
        {
            int i = 0;
            return new Regex("'").Replace(text, m => i++ % 2 == 0 ? "''": "''");
        }
    }
}
