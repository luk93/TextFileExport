﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
using TextFileExport.UI_Tools;
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
        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            dbTables_g = new ObservableCollection<DbTable>();
            LV_Tables.ItemsSource = dbTables_g;
            textFile_g = null!;
        }
        #region UI Event Handlers
        private async void B_CheckDbConn_ClickAsync(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ConnSetting = $"Data Source = {TB_Server.Text}; Database = {TB_DBName.Text}; User ID = {TB_Username.Text}; Password = {TB_Password.Text}; Encrypt=False";
            using var context = new AppDbContext();
            if (await context.CanConnectAsync())
                UI_ConnectionDataCorrect();
            else
                UI_ConnectionDataNotCorrect();
        }
        private void B_CheckTables_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PLCName = TB_PlcName.Text;
            DbTablesTools.FillTableWithData(dbTables_g, Properties.Settings.Default.PLCName);
            try
            {
                using var context = new AppDbContext();
                foreach (var table in dbTables_g)
                {
                    if (context.TableExists(table.Name))
                    {
                        table.IsInDb = true;
                        UI_PlcNameCorrect();
                        TextblockAddLine(TB_Status, $"Expected table: {table.Name} exists!\n");
                    }
                    else
                    {
                        table.IsInDb = false;
                        UI_PlcNameNotCorrect();
                        TextblockAddLine(TB_Status, $"Expected table: {table.Name} NOT exists!\n");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);

            }
        }
        private async void B_BrowseTexfilePath_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (textFile_g.Exists && !UiTools.IsFileLocked(textFile_g.FullName))
            {
                TextblockAddLine(TB_Status, $"Selected: {textFile_g.FullName}\n");
                try
                {
                    await DbTablesTools.LoadFromExcelFile(dbTables_g, textFile_g);
                    foreach (var table in dbTables_g)
                    {
                        TextblockAddLine(TB_Status, table.PrintExcelData());
                    }
                }
                catch (Exception ex)
                {
                    TB_Status.Text = ex.Message;
                    TB_Status.Text += ex.StackTrace;
                }
            }
            else
            {
                TextblockAddLine(TB_Status, "File not exist or in use!\n");
            }

        }
        private void B_GetTextsFromTextfile_Click(object sender, RoutedEventArgs e)
        {
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
                TextblockAddLine(TB_Status, $"Chosen file: {textFile_g.FullName}\n");
            }
        }
        private void B_ExportTextsToDB_ClickAsync(object sender, RoutedEventArgs e)
        {
            var messages = new List<Messages>();
            using (var context = new AppDbContext())
            {
                messages = context.Messagess
                    .AsNoTracking()
                    .Where(x => x.IdAlarm > 1 && x.IdAlarm < 200)
                    .ToList();
            }
            //using var context = new AppDbContext();
            //foreach (var table in dbTables_g)
            //{
            //    if(table.UpdateDb && table.Name.Contains("Alarms"))
            //    {
            //        foreach (var alarmRecord in table.AlarmRecords)
            //        {
            //            var dbRecord = context.Alarmss
            //                .Where(x => x.Id == alarmRecord.Id)
            //                .SingleOrDefault();
            //            if(dbRecord != null)
            //            {
            //                context.Alarmss.Update(alarmRecord);
            //                alarmRecord.Status = "DB Updated";
            //            }
            //            else
            //            {
            //                context.Alarmss.Add(alarmRecord);
            //                alarmRecord.Status = "DB Inserted";
            //            }
            //        }
            //        context.SaveChanges();
            //    }
            //}
        }
        #endregion
        #region UI Functions
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

        #endregion
    }
}
