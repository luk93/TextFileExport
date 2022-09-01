using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextFileExport.Db;

namespace TextFileExport.DataContainers
{
    public class RecordStatus : Alarms
    {
        public string Status { get; set; } = "";
        public RecordStatus()
        {
            Status = "unknown";
        }
    }
}
