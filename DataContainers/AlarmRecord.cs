using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextFileExport.Db;

namespace TextFileExport.DataContainers
{
    public class AlarmRecord : Alarms
    {
        public enum Status
        {
            Unknown,
            WsOk,
            DbInserted,
            DbUpdated,
            DbPassed
        }
        public Status RecordStatus { get; set; }
        public AlarmRecord()
        {
            RecordStatus = Status.Unknown;
        }
    }
}
