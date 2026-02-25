using KindomDataAPIServer.Common;
using KindomDataAPIServer.DataService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// LogView.xaml 的交互逻辑
    /// </summary>
    public partial class LogServerView : Window
    {
        public LogServerView()
        {
            InitializeComponent();
        }

        private void Logger_Clean(object sender, RoutedEventArgs e)
        {
            logger.Document.Blocks.Clear();
            logs = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
        string[] logs;
        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wellDataService = ServiceLocator.GetService<IDataWellService>();
                double days = Convert.ToDouble(logDaysInput.Value);
                var logStr = await wellDataService.get_intelligent_logging(days);
                logs = JsonHelper.ConvertFrom<string[]>(logStr);
                RefreahLogByType();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        private void logTypeInput_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            RefreahLogByType();
        }

        private void RefreahLogByType()
        {
            try
            {
                if (logs == null || logs.Length == 0)
                {
                    return;
                }
                var logType = logTypeInput.Text;
                string[] filteredLogs;
                if (logType == "INFO")
                {
                    filteredLogs = logs.Where(log => log.Contains($"[INFO ]")).ToArray();
                }
                else if (logType == "ERROR")
                {
                    filteredLogs = logs.Where(log => log.Contains($"[ERROR]")).ToArray();
                }
                else
                {
                    filteredLogs = logs;
                }
                logger.Document.Blocks.Clear();
                foreach (var item in filteredLogs)
                {
                    logger.AppendText(item);
                    logger.AppendText(Environment.NewLine);
                }
                logger.ScrollToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (logs == null || logs.Length == 0)
                {
                    return;
                }
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "Text File (*.txt)|*.txt";
                saveFileDialog.Title = "Export Log";
                saveFileDialog.FileName = "log.txt";
                if (saveFileDialog.ShowDialog() == true)
                {
                    string allText = new TextRange(logger.Document.ContentStart, logger.Document.ContentEnd).Text;
                    try
                    {
                        System.IO.File.WriteAllText(saveFileDialog.FileName, allText, Encoding.UTF8);
                        MessageBox.Show("Export succeeded!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
}
