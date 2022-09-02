using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TextFileExport.Db
{
    public partial class Alarms
    {
        [Key]
        public int Id { get; set; }
        public int IdAlarm { get; set; }
        public string? Comment { get; set; }
    }
}
