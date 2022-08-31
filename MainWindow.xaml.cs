using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using TextFileExport.ViewModels;

namespace TextFileExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly UserViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            //Initialize viewModel Object
            vm = ((UserViewModel)(this.DataContext));
        }
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PLCName = vm.DbConnection.PlcName;
            Properties.Settings.Default.ConnSetting = $"Data Source = {vm.DbConnection.ServerName}; Database = {vm.DbConnection.DatabaseName}; User ID = {vm.DbConnection.UserName}; Password = {vm.DbConnection.Password}; Encrypt=False";
            using var context = new AppDbContext();
            if (await context.CanConnectAsync())
            {
                TB_UserInfo.Text = "Connection Available!";
                TB_Status.Text += $"Connected using connection string:\n{Properties.Settings.Default.ConnSetting}\n";
                try
                {
                    if (context.TableExists($"Alarms_{Properties.Settings.Default.PLCName}"))
                        TB_Status.Text += $"Expected table: Alarms_{Properties.Settings.Default.PLCName} EXIST!\n";
                    else
                        TB_Status.Text += $"Expected table: Alarms_{Properties.Settings.Default.PLCName} NOT EXIST!\n";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + ex.StackTrace);
                }
            }
            else
            {
                TB_UserInfo.Text = "Connection NOT Available!";
                TB_Status.Text += $"Connection String: {Properties.Settings.Default.ConnSetting} was NOT OK!\n";
            }
            var messages = new List<Messages>();

                messages = context.Messagess
                    .AsNoTracking()
                    .Where(x => x.IdAlarm > 1 && x.IdAlarm < 200)
                    .ToList();
            foreach (var message in messages)
            {
                TB_Status.Text += $"\nId: {message.Id}, IdAlarm: {message.IdAlarm}, Comment: {message.Comment}";
            }

        }
    }
}
