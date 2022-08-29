using System;
using System.Collections.Generic;

namespace TextFileExport.Db
{
    public partial class Alarms
    {
        public int Id { get; set; }
        public int IdAlarm { get; set; }
        public string? Comment { get; set; }
    }
}
