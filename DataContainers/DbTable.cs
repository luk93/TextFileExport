using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

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
            return $"Table Name: {Name}, WS Name: {WsName}, Texts got: {AlarmRecords.Count(item => item.Status == "WS OK")}/{AlarmRecords.Count}\n";
        }
        public string PrintDbData()
        {
            return $"Table Name: {Name}, Texts inserted: {AlarmRecords.Count(item => item.Status == "DB Inserted")}/{AlarmRecords.Count}, " +
                $"Texts updated: {AlarmRecords.Count(item => item.Status == "DB Updated")}/{AlarmRecords.Count} "+
                $"Texts passed: {AlarmRecords.Count(item => item.Status == "DB Passed")}/{AlarmRecords.Count}";
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
            if (repItemlist.Count > 0)
            {
                foreach (var duplicate in repItemlist)
                {
                    UI_Tools.UIControlsExt.TextblockAddLine(tb, $"Duplicated Id found! Id:{duplicate.IdAlarm}\n");
                }
                return true;
            }
            return false;

        }
        public bool IsTableReadyToUpdateDB()
        {
            return IsInDb && UpdateDb && AlarmRecords.Count > 0;
        }
    }
}
