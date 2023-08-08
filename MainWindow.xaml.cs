using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using OfficeOpenXml;
using Serilog;
using Serilog.Events;
using TextFileExport.DataContainers;
using TextFileExport.Db;
using TextFileExport.Extensions;
using TextFileExport.Properties;
using TextFileExport.Tools;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TextFileExport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<DbTable> _dbTables;
        private FileInfo _textFile;
        private readonly Progress<int> _progress1;
        private readonly Progress<int> _progress2;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        public MainWindow()
        {
            InitializeComponent();

            //Logger Configuration
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                LoggerConfiguration loggerConfiguration = new();
                loggerConfiguration.WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
                   .MinimumLevel.Information()
                   .MinimumLevel.Override("Logging: ", LogEventLevel.Debug);
                builder.AddSerilog(loggerConfiguration.CreateLogger());
            });
            _logger = _loggerFactory.CreateLogger("logger");
            _logger.LogInformation("Logging Started");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {PB_Password.Password}; Encrypt=False";
            Settings.Default.PLCName = TB_PlcName.Text;
            _dbTables = new ObservableCollection<DbTable>();
            LV_Tables.ItemsSource = _dbTables;
            _textFile = null!;
            _progress1 = new Progress<int>(val => PB_Status1.Value = val);
            _progress2 = new Progress<int>(val => PB_Status2.Value = val);
        }
        #region UI Event Handlers
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {PB_Password.Password}; Encrypt=False";
            using var context = new AppDbContext(_loggerFactory);
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
            if (string.IsNullOrEmpty(TB_PlcName.Text))
            {
                TB_Status.AddLine($"Fill PLC Name please");
                return;
            }
            UI_DisableButtonAndChangeCursor(sender);
            Settings.Default.PLCName = TB_PlcName.Text;
            DbTablesTools.FillTableWithData(_dbTables, Settings.Default.PLCName, _loggerFactory);
            try
            {
                using var context = new AppDbContext(_loggerFactory);
                bool tableFound = false;
                foreach (var table in _dbTables)
                {
                    if (context.TableExists(table.Name))
                    {
                        var notCorrectTableFound = false;
                        //Get all properties info from Alarms class 
                        var memberInfo = table.AlarmRecords.GetType().GetGenericArguments()
                            .Single().BaseType;
                        if (memberInfo != null)
                            foreach (PropertyInfo propertyInfo in memberInfo.GetProperties())
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
                _logger.LogError(ex.Message + ex.StackTrace);
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private async void B_GetTextsFromTextfile_Click(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            if (_textFile.Exists && !FileTools.IsFileLocked(_textFile.FullName))
            {
                TB_Status.AddLine($"Selected: {_textFile.FullName}");
                try
                {
                    await DbTablesTools.LoadFromExcelFile(_dbTables, _textFile);
                    bool duplicateFound = false;
                    bool allTablesEmpty = true;
                    foreach (var table in _dbTables)
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
                        TB_UserInfo.Text = "(5)Update/Insert texts to DB OR (13) Generate MERGE Query";
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message + ex.StackTrace);
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
                _textFile = new FileInfo(openFileDialog1.FileName);
                TB_TextfilePath.Text = _textFile.FullName;
                TB_Status.AddLine($"Chosen file: {_textFile.FullName}");
                UI_TextfileSelected();
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private async void B_ExportTextsToDB_ClickAsync(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            try
            {
                await DbTablesTools.UpdateInDatabase(_dbTables, TB_Status, PB_Status1, PB_Status2, _progress1, _progress2, _loggerFactory);
                UI_TextsExportedToDB();
            }
            catch (Exception ex)
            {
                var info = $"Msg: {ex.Message}, Inner: {ex.InnerException?.Message}, StackTrace:{ex.StackTrace}";
                _logger.LogError(info);
                MessageBox.Show(info);
            }
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void DbUpdateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UIExt_ExportToDbEnable();
        }
        private void B_GenTables_Click(object sender, RoutedEventArgs e)
        {
            string plcName = TB_PlcName.Text;
            if (string.IsNullOrEmpty(plcName))
            {
                TB_Status.AddLine($"Fill PLC Name please");
                return;
            }
            UI_DisableButtonAndChangeCursor(sender);
            DbTablesTools.FillTableWithData(_dbTables, plcName, _loggerFactory);
            UI_TablesGenerated();
            UI_EnableButtonAndChangeCursor(sender);
        }
        private void B_GenMergeQuery_Click(object sender, RoutedEventArgs e)
        {
            UI_DisableButtonAndChangeCursor(sender);
            string output = string.Empty;
            _dbTables.ForEach((table,info) =>
            {
                output += table.GenerateMergeQuery();
            });
            if (output == string.Empty)
            {
                TB_UserInfo.Text = "MERGE Query has not been generated. Amount of generated alarm records is 0";
                UI_EnableButtonAndChangeCursor(sender);
                return;
            }
            Clipboard.SetText(output);
            TB_UserInfo.Text = "MERGE Query has been copied to Clipboard -> can be pasted to Notepad or Ignition DB Browser";
            UI_EnableButtonAndChangeCursor(sender);
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
            PB_Password.Background = Brushes.IndianRed;
            TB_UserInfo.Text = "(1)Connection NOT Available! Type Correct DB Data.";
            TB_Status.AddLine($"Connection String was NOT OK!");
            B_CheckTables.IsEnabled = false;
            B_ExportTextsToDB.IsEnabled = false;
        }
        private void UI_ConnectionDataCorrect()
        {
            TB_Server.Background = Brushes.LightGreen;
            TB_DBName.Background = Brushes.LightGreen;
            TB_Username.Background = Brushes.LightGreen;
            PB_Password.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "(2)Connection Available! Check DB Tables";
            TB_Status.AddLine($"Connection String was OK!");
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
        private void UI_TablesGenerated()
        {
            TB_UserInfo.Text = "(11) Browse for Textfile (.xlsm) now";
            B_BrowseTexfilePath.IsEnabled = true;
        }
        private void UI_TextfileSelected()
        {
            B_ExportTextsToDB.IsEnabled = false;
            B_GetTextsFromTextfile.IsEnabled = true;
            TB_UserInfo.Text = "(4/12)Get Texts from choosen document";
            TB_TextfilePath.Background = Brushes.WhiteSmoke;
        }
        private void UI_TextfileNotCorrect()
        {
            TB_TextfilePath.Background = Brushes.IndianRed;
            B_GenMergeQuery.IsEnabled = false;
        }
        private void UI_TextfileCorrect()
        {
            TB_TextfilePath.Background = Brushes.LightGreen;
            TB_UserInfo.Text = "(4)Choose tables to update and trigger Apply button to insert/update Texts in DB! OR (12)Generate Insert Query";
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
            B_ExportTextsToDB.IsEnabled = DbTablesTools.IsAnyTableReady(_dbTables);
            B_GenMergeQuery.IsEnabled = true;
        }
        #endregion
    }

}