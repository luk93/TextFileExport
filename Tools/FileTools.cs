using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextFileExport.Tools
{
    public static class FileTools
    {
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                var stream = File.OpenRead(filePath);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}
