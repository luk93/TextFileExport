using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

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
    }
}
