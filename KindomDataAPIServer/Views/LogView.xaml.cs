using KindomDataAPIServer.Common;
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
using System.Windows.Threading;

namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// LogView.xaml 的交互逻辑
    /// </summary>
    public partial class LogView : Window
    {
        private const int DefaultMaxUiLogLines = 50;
        private const int DefaultRefreshIntervalMs = 3000;
        private const int MinMaxUiLogLines = 20;
        private const int MaxMaxUiLogLines = 20000;
        private const int MinRefreshIntervalMs = 100;
        private const int MaxRefreshIntervalMs = 30000;

        private readonly List<string> _visibleLogs = new List<string>();
        private readonly DispatcherTimer _refreshTimer;
        private int _maxUiLogLines = DefaultMaxUiLogLines;
        private ScrollViewer _logScrollViewer;

        public string MaxUiLogLinesRangeToolTip
        {
            get { return string.Format("Range: {0} - {1}", MinMaxUiLogLines, MaxMaxUiLogLines); }
        }

        public string RefreshIntervalMsRangeToolTip
        {
            get { return string.Format("Range: {0} - {1} ms", MinRefreshIntervalMs, MaxRefreshIntervalMs); }
        }

        public LogView()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background);
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(DefaultRefreshIntervalMs);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _logScrollViewer = FindVisualChild<ScrollViewer>(logger);
                FlushPendingLogs();
            }), DispatcherPriority.Loaded);
        }

        private void Logger_Clean(object sender, RoutedEventArgs e)
        {
            _visibleLogs.Clear();
            logger.Document.Blocks.Clear();
            LogManagerService.Instance.ClearPendingUiLogs();
        }

        private void ApplyLogSettings_Click(object sender, RoutedEventArgs e)
        {
            ApplyLogSettings();
        }

        private void LogSettingsInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyLogSettings();
                e.Handled = true;
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            FlushPendingLogs();
        }

        private void ApplyLogSettings()
        {
            _maxUiLogLines = ReadBoundedInt(maxLineCountInput.Text, DefaultMaxUiLogLines, MinMaxUiLogLines, MaxMaxUiLogLines);
            var refreshIntervalMs = ReadBoundedInt(refreshIntervalInput.Text, DefaultRefreshIntervalMs, MinRefreshIntervalMs, MaxRefreshIntervalMs);

            maxLineCountInput.Text = _maxUiLogLines.ToString();
            refreshIntervalInput.Text = refreshIntervalMs.ToString();
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(refreshIntervalMs);

            TrimVisibleLogs();
            ScrollToEnd();
        }

        private void FlushPendingLogs()
        {
            List<string> pendingLogs = LogManagerService.Instance.DequeueUiLogs();
            if (pendingLogs.Count == 0)
            {
                return;
            }

            bool shouldScrollToEnd = IsScrolledToEnd();
            bool shouldRebuildDocument = false;

            if (pendingLogs.Count >= _maxUiLogLines)
            {
                _visibleLogs.Clear();
                pendingLogs = pendingLogs.Skip(pendingLogs.Count - _maxUiLogLines).ToList();
                shouldRebuildDocument = true;
            }
            else
            {
                int overflowCount = _visibleLogs.Count + pendingLogs.Count - _maxUiLogLines;
                if (overflowCount > 0)
                {
                    _visibleLogs.RemoveRange(0, overflowCount);
                    shouldRebuildDocument = true;
                }
            }

            _visibleLogs.AddRange(pendingLogs);

            if (shouldRebuildDocument)
            {
                RebuildLogDocument();
            }
            else
            {
                AppendLogsToDocument(pendingLogs);
            }

            if (shouldScrollToEnd)
            {
                ScrollToEnd();
            }
        }

        private void TrimVisibleLogs()
        {
            if (_visibleLogs.Count <= _maxUiLogLines)
            {
                return;
            }

            var retainedLogs = _visibleLogs.Skip(_visibleLogs.Count - _maxUiLogLines).ToList();
            _visibleLogs.Clear();
            _visibleLogs.AddRange(retainedLogs);
            RebuildLogDocument();
        }

        private bool IsScrolledToEnd()
        {
            if (_logScrollViewer == null)
            {
                return true;
            }

            return _logScrollViewer.VerticalOffset >= _logScrollViewer.ScrollableHeight - 1;
        }

        private void ScrollToEnd()
        {
            if (_visibleLogs.Count > 0)
            {
                logger.ScrollToEnd();
            }
        }

        private void AppendLogsToDocument(IEnumerable<string> logs)
        {
            foreach (var log in logs)
            {
                logger.AppendText(log);
                logger.AppendText(Environment.NewLine);
            }
        }

        private void RebuildLogDocument()
        {
            logger.Document.Blocks.Clear();

            var paragraph = new Paragraph
            {
                Margin = new Thickness(0),
                LineHeight = 1
            };
            paragraph.Inlines.Add(new Run(string.Join(Environment.NewLine, _visibleLogs)));
            logger.Document.Blocks.Add(paragraph);

            if (_visibleLogs.Count > 0)
            {
                logger.AppendText(Environment.NewLine);
            }
        }

        private static int ReadBoundedInt(string value, int defaultValue, int minValue, int maxValue)
        {
            int result;
            if (!int.TryParse(value, out result))
            {
                return defaultValue;
            }

            if (result < minValue)
            {
                return minValue;
            }

            if (result > maxValue)
            {
                return maxValue;
            }

            return result;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var typedChild = child as T;
                if (typedChild != null)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
