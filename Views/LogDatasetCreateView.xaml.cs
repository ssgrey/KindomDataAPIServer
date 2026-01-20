using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// LogDatasetCreateView.xaml 的交互逻辑
    /// </summary>
    public partial class LogDatasetCreateView : Window
    {
        public string NewName { set; get; } = "";
        public LogDatasetCreateView()
        {
            InitializeComponent();
        }

        private void SimpleButton_Click(object sender, RoutedEventArgs e)
        {
            NewName = txt.Text;
            if (string.IsNullOrWhiteSpace(NewName))
            {
                DXMessageBox.Show(this,"The name cannot be empty!");
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
