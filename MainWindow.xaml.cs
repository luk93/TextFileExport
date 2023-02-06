using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OfficeOpenXml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
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
using TextFileExport.Extensions;
using TextFileExport.Tools;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace TextFileExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<DbTable> DbTablesG;
        public FileInfo TextFileG;
        public Stopwatch Stopwatch;
        public Progress<int> Progress1;
        public Progress<int> Progress2;
        public ILoggerFactory LoggerFactory;
        public MainWindow()
        {
            InitializeComponent();
            //Logger Configuration
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                LoggerConfiguration loggerConfiguration = new();
                loggerConfiguration.WriteTo.File("loggs.txt", rollingInterval: RollingInterval.Day)
                   .MinimumLevel.Information()
                   .MinimumLevel.Override("Logging: ", Serilog.Events.LogEventLevel.Debug);
                builder.AddSerilog(loggerConfiguration.CreateLogger());
            });
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Properties.Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {TB_Password.Text}; Encrypt=False";
            Properties.Settings.Default.PLCName = TB_PlcName.Text;
            DbTablesG = new ObservableCollection<DbTable>();
            LV_Tables.ItemsSource = DbTablesG;
            TextFileG = null!;
            Stopwatch = new Stopwatch();
            Progress1 = new Progress<int>(val => PB_Status1.Value = val);
            Progress2 = new Progress<int>(val => PB_Status2.Value = val);
        }
        #region UI Event Handlers
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            Properties.Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {TB_Password.Text}; Encrypt=False";
            using var context = new AppDbContext(LoggerFactory);
            if (!await context.CanConnectAsync())
            {
                UI_ConnectionDataNotCorrect();
                UI_EnableButtonAndChangeCursor(sender);
                return;
            }
            UI_ConnectionDataCorrect();               
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void B_CheckTables_Click(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            Properties.Settings.Default.PLCName = TB_PlcName.Text;
            DbTablesTools.FillTableWithData(DbTablesG, Properties.Settings.Default.PLCName);
            try
            {
                using var context = new AppDbContext(LoggerFactory);
                bool tableFound = false;
                foreach (var table in DbTablesG)
                {

                    if (context.TableExists(table.Name))
                    {
                        var notCorrectTableFound = false;
                        //Get all properties info from Alarms class 
                        foreach (PropertyInfo propertyInfo in table.AlarmRecords.GetType().GetGenericArguments().Single().BaseType.GetProperties())
                        {
                            var columnName = propertyInfo.Name;
                            if (context.ColumnInTableExists(table.Name, columnName))
                            {
                                TB_Status.AddLine($"Expected column:{columnName} in table: {table.Name} exists!");
                            }
                            else
                            {
                                notCorrectTableFound = true;
                                TB_Status.AddLine($"Expected column:{columnName} in table: {table.Name} NOT exists!");
                            }
                        }
                        if (notCorrectTableFound)
                        {
                            table.IsInDb = false;
                            TB_Status.AddLine($"Expected table: {table.Name} exists but not correct table has been found!");
                        }
                        else
                        {
                            tableFound = true;
                            table.IsInDb = true;
                            TB_Status.AddLine($"Expected table: {table.Name} exists!");
                        }
                    }
                    else
                    {
                        table.IsInDb = false;
                        TB_Status.AddLine($"Expected table: {table.Name} NOT exists!");
                    }
                }
                if (tableFound)
                    UI_PlcNameCorrect();
                else
                    UI_PlcNameNotCorrect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private async void B_GetTextsFromTextfile_Click(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            if (TextFileG.Exists && !FileTools.IsFileLocked(TextFileG.FullName))
            {
                TB_Status.AddLine($"Selected: {TextFileG.FullName}");
                try
                {
                    await DbTablesTools.LoadFromExcelFile(DbTablesG, TextFileG);
                    bool duplicateFound = false;
                    bool allTablesEmpty = true;
                    foreach (var table in DbTablesG)
                    {
                        TB_Status.AddLine(table.PrintExcelData());
                        duplicateFound = table.AreDuplicates(TB_Status) || duplicateFound;
                        allTablesEmpty = table.AlarmRecords.Count <= 0 && allTablesEmpty;
                    }
                    if (allTablesEmpty)
                    {
                        UI_TextfileNotCorrect();
                        TB_UserInfo.Text = "No valid text found in chosen document!";
                        TB_Status.AddLine("No valid text found in chosen document!");
                    }
                    else if (duplicateFound)
                    {
                        UI_TextfileNotCorrect();
                        TB_UserInfo.Text = "Duplicated Ids found in chosen document!";
                    }
                    else
                    {
                        UI_TextfileCorrect();
                        TB_UserInfo.Text = "(5)Update/Insert texts to DB";
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + ex.StackTrace);
                }
            }
            else
                TB_Status.AddLine("File not exist or in use!");
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void B_BrowseTexfilePath_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            OpenFileDialog openFileDialog1 = new()
            {
                InitialDirectory = @"c:\Users\localadm\Desktop",
                Title = "Select Customer Textfile (.xlsm)",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "xlsm",
                Filter = "Excel file (*.xlsm)|*.xlsm",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true,
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                TextFileG = new FileInfo(openFileDialog1.FileName);
                TB_TextfilePath.Text = TextFileG.FullName;
                TB_Status.AddLine($"Chosen file: {TextFileG.FullName}");
                UI_TextfileSelected();
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private async void B_ExportTextsToDB_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            try
            {
                await DbTablesTools.UpdateInDatabase(DbTablesG, TB_Status, PB_Status1, PB_Status2, Progress1, Progress2, LoggerFactory);
                UI_TextsExportedToDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Msg: {ex.Message}, Inner: {ex.InnerException?.Message}, StackTrace:{ex.StackTrace}");
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void DbUpdateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UIExt_ExportToDbEnable();
        }

        #endregion
        #region UI Functions
        public void UI_DisableButtonAndChangeCursor(object sender)
        {
            Cursor = Cursors.Wait;
            Button button = (Button)sender;
            button.IsEnabled = false;
        }
        public void UI_EnableButtonAndChangeCursor(object sender)
        {
            Cursor = Cursors.Arrow;
            Button button = (Button)sender;
            button.IsEnabled = true;
        }
        private void UI_ConnectionDataNotCorrect()
        {
            TB_Server.Background = Brushes.IndianRed;
            TB_DBName.Background = Brushes.IndianRed;
            TB_Username.Background = Brushes.IndianRed;
            TB_Password.Background = Brushes.IndianRed;
            TB_UserInfo.Text = "(1)Connection NOT Available! Type Correct DB Data.";
            TB_Status.AddLine($"Connection String: {Properties.Settings.Default.ConnSetting} was NOT OK!");
            B_CheckTables.IsEnabled = false;
            B_ExportTextsToDB.IsEnabled = false;
        }
        private void UI_ConnectionDataCorrect()
        {
            TB_Server.Background = Brushes.LightGreen;
            TB_DBName.Background = Brushes.LightGreen;
            TB_Username.Background = Brushes.LightGreen;
            TB_Password.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "(2)Connection Available! Check DB Tables";
            TB_Status.AddLine($"Connection String: {Properties.Settings.Default.ConnSetting} was OK!");
            B_CheckTables.IsEnabled = true;
        }
        private void UI_PlcNameNotCorrect()
        {
            TB_PlcName.Background = Brushes.IndianRed;
            TB_UserInfo.Text = "(2)Available DB Tables have not been found! Check PLC Name or make sure that correct tables exist in DB";
            B_ExportTextsToDB.IsEnabled = false;
        }
        private void UI_PlcNameCorrect()
        {
            TB_PlcName.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "(3)Available DB Tables have been found! Browse for Textfile (.xlsm) now";
            B_BrowseTexfilePath.IsEnabled = true;
        }
        private void UI_TextfileSelected()
        {
            B_ExportTextsToDB.IsEnabled = false;
            B_GetTextsFromTextfile.IsEnabled = true;
            TB_UserInfo.Text = "(4)Get Texts from choosen document";
            TB_TextfilePath.Background = Brushes.WhiteSmoke;
        }
        private void UI_TextfileNotCorrect()
        {
            TB_TextfilePath.Background = Brushes.IndianRed;
        }
        private void UI_TextfileCorrect()
        {
            TB_TextfilePath.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "(4)Choose tables to update and trigger Apply button to insert/update Texts in DB!";
            UIExt_ExportToDbEnable();
        }
        private void UI_TextsExportedToDB()
        {
            TB_UserInfo.Text = "Operations on DB finished!";
        }
        #endregion
        #region UI Function Extensions
        private void UIExt_ExportToDbEnable()
        {
            B_ExportTextsToDB.IsEnabled = DbTablesTools.IsAnyTableReady(DbTablesG);
        }
        #endregion
    }

}