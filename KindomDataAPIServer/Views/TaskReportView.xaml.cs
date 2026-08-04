using KindomDataAPIServer.Common;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace KindomDataAPIServer.Views
{
    public partial class TaskReportView : Window
    {
        public TaskReportView()
        {
            InitializeComponent();
            LoadLatestReport();
        }

        public void LoadLatestReport()
        {
            LoadReport(SyncTaskReportService.Instance.GetLatestReportPath());
        }

        private void Latest_Click(object sender, RoutedEventArgs e)
        {
            LoadLatestReport();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Task Report (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            string reportDirectory = SyncTaskReportService.Instance.ReportDirectory;
            if (!string.IsNullOrWhiteSpace(reportDirectory) && Directory.Exists(reportDirectory))
            {
                dialog.InitialDirectory = reportDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                LoadReport(dialog.FileName);
            }
        }

        private void LoadReport(string reportPath)
        {
            reportPathTextBox.Text = reportPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
            {
                reportTextBox.Text = "No task report found.";
                return;
            }

            try
            {
                reportTextBox.Text = File.ReadAllText(reportPath, Encoding.UTF8);
                reportTextBox.ScrollToEnd();
            }
            catch (Exception ex)
            {
                reportTextBox.Text = "Failed to load task report: " + ex.Message;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}
