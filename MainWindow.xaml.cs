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
        public ObservableCollection<DbTable> dbTables_g;
        public FileInfo textFile_g;
        public Stopwatch stopwatch;
        public Progress<int> progress1;
        public Progress<int> progress2;
        public ILoggerFactory loggerFactory;
        public MainWindow()
        {
            InitializeComponent();
            //Logger Configuration
            loggerFactory = LoggerFactory.Create(builder =>
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
            dbTables_g = new ObservableCollection<DbTable>();
            LV_Tables.ItemsSource = dbTables_g;
            textFile_g = null!;
            stopwatch = new Stopwatch();
            progress1 = new Progress<int>(val => PB_Status1.Value = val);
            progress2 = new Progress<int>(val => PB_Status2.Value = val);
        }
        #region UI Event Handlers
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            Properties.Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {TB_Password.Text}; Encrypt=False";
            using var context = new AppDbContext(loggerFactory);
            if (await AppDbContextExt.CanConnectAsync(context))
                UI_ConnectionDataCorrect();
            else
                UI_ConnectionDataNotCorrect();
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void B_CheckTables_Click(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            Properties.Settings.Default.PLCName = TB_PlcName.Text;
            DbTablesTools.FillTableWithData(dbTables_g, Properties.Settings.Default.PLCName);
            try
            {
                using var context = new AppDbContext(loggerFactory);
                bool tableFound = false;
                foreach (var table in dbTables_g)
                {

                    if (AppDbContextExt.TableExists(context, table.Name))
                    {
                        var notCorrectTableFound = false;
                        //Get all properties info from Alarms class 
                        foreach (PropertyInfo propertyInfo in table.AlarmRecords.GetType().GetGenericArguments().Single().BaseType.GetProperties())
                        {
                            var columnName = propertyInfo.Name;
                            if (AppDbContextExt.ColumnInTableExists(context, table.Name, columnName))
                            {
                                TB_Status.AddLine($"Expected column:{columnName} in table: {table.Name} exists!\n");
                            }
                            else
                            {
                                notCorrectTableFound = true;
                                TB_Status.AddLine($"Expected column:{columnName} in table: {table.Name} NOT exists!\n");
                            }
                        }
                        if (notCorrectTableFound)
                        {
                            table.IsInDb = false;
                            TB_Status.AddLine($"Expected table: {table.Name} exists but not correct table has been found!\n");
                        }
                        else
                        {
                            tableFound = true;
                            table.IsInDb = true;
                            TB_Status.AddLine($"Expected table: {table.Name} exists!\n");
                        }
                    }
                    else
                    {
                        table.IsInDb = false;
                        TB_Status.AddLine($"Expected table: {table.Name} NOT exists!\n");
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
            if (textFile_g != null)
            {
                if (textFile_g.Exists && !FilesTools.IsFileLocked(textFile_g.FullName))
                {
                    TB_Status.AddLine($"Selected: {textFile_g.FullName}\n");
                    try
                    {
                        await DbTablesTools.LoadFromExcelFile(dbTables_g, textFile_g);
                        bool duplicateFound = false;
                        bool allTablesEmpty = true;
                        foreach (var table in dbTables_g)
                        {
                            TB_Status.AddLine(table.PrintExcelData());
                            duplicateFound = table.AreDuplicates(TB_Status) || duplicateFound;
                            allTablesEmpty = table.AlarmRecords.Count <= 0 && allTablesEmpty;
                        }
                        if (allTablesEmpty)
                        {
                            UI_TextfileNotCorrect();
                            TB_UserInfo.Text = "No valid text found in choosen document!";
                            TB_Status.AddLine("No valid text found in choosen document!");
                        }
                        else if (duplicateFound)
                        {
                            UI_TextfileNotCorrect();
                            TB_UserInfo.Text = "Duplicated Ids found in choosen document!";
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
                    TB_Status.AddLine("File not exist or in use!\n");
            }
            else
                TB_UserInfo.Text = "No valid text found in choosen document!";
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
                textFile_g = new FileInfo(openFileDialog1.FileName);
                TB_TextfilePath.Text = textFile_g.FullName;
                TB_Status.AddLine($"Chosen file: {textFile_g.FullName}\n");
                UI_TextfileSelected();
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private async void B_ExportTextsToDB_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            try
            {
                await DbTablesTools.UpdateInDatabase(dbTables_g, TB_Status, PB_Status1, PB_Status2, progress1, progress2, loggerFactory);
                UI_TextsExportedToDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Msg: {ex.Message}, Inner: {ex.InnerException.Message}, StackTrace:{ex.StackTrace}");
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
            TB_Status.AddLine($"Connection String: {Properties.Settings.Default.ConnSetting} was NOT OK!\n");
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
            TB_Status.AddLine($"Connection String: {Properties.Settings.Default.ConnSetting} was OK!\n");
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
            B_ExportTextsToDB.IsEnabled = DbTablesTools.IsAnyTableReady(dbTables_g);
        }
        #endregion
    }

}