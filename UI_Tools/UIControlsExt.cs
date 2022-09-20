using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TextFileExport.UI_Tools
{
    public static class UIControlsExt
    {
        public static void TextblockAddLine(TextBlock tb, string text) => tb.Inlines.InsertBefore(tb.Inlines.FirstInline, new Run(text));
    }
}
