using DevExpress.Xpf.Core;
using KindomDataAPIServer.Common;
using System;
using System.Windows;

namespace KindomDataAPIServer.Views
{
    /// <summary>
    /// Interaction logic for AdvancedSettingView.xaml
    /// </summary>
    public partial class AdvancedSettingView : ThemedWindow
    {
        public AdvancedSettingView()
        {
            InitializeComponent();
            WellHeaderBatchSize = AdvancedSettingsConfig.GetWellHeaderBatchSize();
            WellTrajectoryBatchSize = AdvancedSettingsConfig.GetWellTrajectoryBatchSize();
            WellTrajectoryUploadConcurrency = AdvancedSettingsConfig.GetWellTrajectoryUploadConcurrency();
            WellLogUploadConcurrency = AdvancedSettingsConfig.GetWellLogUploadConcurrency();
            WellLogBatchCurveCount = AdvancedSettingsConfig.GetWellLogBatchCurveCount();
            WellProductionUploadConcurrency = AdvancedSettingsConfig.GetWellProductionUploadConcurrency();
            WellProductionBatchDailyDataCount = AdvancedSettingsConfig.GetWellProductionBatchDailyDataCount();
            WellFormationBatchSize = AdvancedSettingsConfig.GetWellFormationBatchSize();
            DataContext = this;
        }

        public int WellHeaderBatchSize { get; set; }
        public int WellTrajectoryBatchSize { get; set; }
        public int WellTrajectoryUploadConcurrency { get; set; }
        public int WellLogUploadConcurrency { get; set; }
        public int WellLogBatchCurveCount { get; set; }
        public int WellProductionUploadConcurrency { get; set; }
        public int WellProductionBatchDailyDataCount { get; set; }
        public int WellFormationBatchSize { get; set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdvancedSettingsConfig.SaveWellHeaderBatchSize(WellHeaderBatchSize);
                AdvancedSettingsConfig.SaveWellTrajectoryBatchSize(WellTrajectoryBatchSize);
                AdvancedSettingsConfig.SaveWellTrajectoryUploadConcurrency(WellTrajectoryUploadConcurrency);
                AdvancedSettingsConfig.SaveWellLogUploadConcurrency(WellLogUploadConcurrency);
                AdvancedSettingsConfig.SaveWellLogBatchCurveCount(WellLogBatchCurveCount);
                AdvancedSettingsConfig.SaveWellProductionUploadConcurrency(WellProductionUploadConcurrency);
                AdvancedSettingsConfig.SaveWellProductionBatchDailyDataCount(WellProductionBatchDailyDataCount);
                AdvancedSettingsConfig.SaveWellFormationBatchSize(WellFormationBatchSize);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Save advanced settings failed: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
