using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextFileExport.Db;

namespace TextFileExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Properties.Settings.Default.ConnSetting = "Data Source = localhost\\SQLEXPRESS; Database = CPM; User ID = root; Password = root; Encrypt=False";
            Properties.Settings.Default.PLCName = "TRMS001";
            var messages = new List<Messages>();
            var stopWatch = new Stopwatch();
            using (var context = new AppDbContext())
            {
                stopWatch.Start();
                messages = context.MessagesTrms001s
                    .AsNoTracking()
                    .Where(x => x.IdAlarm > 1 && x.IdAlarm < 200)
                    .ToList();
            }
            foreach(var message in messages)
            {
                TB_Status.Text += $"\nId: {message.Id}, IdAlarm: {message.IdAlarm}, Comment: {message.Comment}";
            }
        }
    }
}
