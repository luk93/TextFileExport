using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using TextFileExport.DataContainers;
using TextFileExport.Db;

namespace TextFileExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<DbTable> dbTables;
        public MainWindow()
        {
            InitializeComponent();
        }
        #region Event Handlers
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {TB_Password.Text}; Encrypt=False";
            using var context = new AppDbContext();
            if (await context.CanConnectAsync())
                UI_ConnectionDataCorrect();
            else
                UI_ConnectionDataNotCorrect();
        }
        private void B_CheckTables_ClickAsync(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PLCName = TB_PlcName.Text;
            CreateTables(Properties.Settings.Default.PLCName);
            LV_Tables.ItemsSource = dbTables;
            try
            {
                using var context = new AppDbContext();
                foreach (var table in dbTables)
                {
                    if (context.TableExists(table.Name))
                    {
                        table.Status = true;
                        UI_PlcNameCorrect();
                        TextblockAddLine(TB_Status, $"Expected table: {table} exists!\n");
                    }
                    else
                    {
                        table.Status = false;
                        UI_PlcNameNotCorrect();
                        TextblockAddLine(TB_Status, $"Expected table: {table} NOT exists!\n");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);

            }
        }
        #endregion
        #region Users Interface
        private void UI_ConnectionDataNotCorrect()
        {
            TB_Server.Background = Brushes.IndianRed;
            TB_DBName.Background = Brushes.IndianRed;
            TB_Username.Background = Brushes.IndianRed;
            TB_Password.Background = Brushes.IndianRed;
            TB_UserInfo.Text = "Connection NOT Available!";
            TextblockAddLine(TB_Status, $"Connection String: {Properties.Settings.Default.ConnSetting} was NOT OK!\n");
        }
        private void UI_ConnectionDataCorrect()
        {
            TB_Server.Background = Brushes.LightGreen;
            TB_DBName.Background = Brushes.LightGreen;
            TB_Username.Background = Brushes.LightGreen;
            TB_Password.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "Connection Available!";
            TextblockAddLine(TB_Status, $"Connection String: {Properties.Settings.Default.ConnSetting} was OK!\n");
        }
        private void UI_PlcNameNotCorrect()
        {
            TB_PlcName.Background = Brushes.IndianRed;
        }
        private void UI_PlcNameCorrect()
        {
            TB_PlcName.Background = Brushes.LightGreen;
        }
        private static void TextblockAddLine(TextBlock tb, string text) => tb.Inlines.InsertBefore(tb.Inlines.FirstInline, new Run(text));
        #endregion
        #region Additional Functions
        private void CreateTables(string plcName)
        {
            if (dbTables == null) dbTables = new List<DbTable>();
            if (dbTables.Count > 0) dbTables.Clear();
            //To fill with Table Names
            dbTables.Add(new DbTable($"Alarms_{plcName}"));
            dbTables.Add(new DbTable($"Messages_{plcName}"));
            dbTables.Add(new DbTable($"Warnings_{plcName}"));
        }
        #endregion
    }
}
