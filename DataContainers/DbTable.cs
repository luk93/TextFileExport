using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextFileExport.DataContainers
{
    public class DbTable
    {
        public string Name { get; set; } = "";
        public bool Status { get; set; }
        public List<RecordStatus> RecordStatuses { get; set; }
        public override string ToString()
        {
            return Name + (Status ? " OK" : " NOK");
        }
        public DbTable(string name)
        {
            Name = name;
            RecordStatuses = new List<RecordStatus>();
            Status = false;
        }
    }
}
