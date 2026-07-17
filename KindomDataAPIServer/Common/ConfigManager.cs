using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public class UserConfig
    {
        public string ProjectName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsRememberPassword { get; set; }
    }

    public class ConfigManager
    {
        private static readonly string ConfigFileName = "userconfig.json";
        private static string _configPath;

        private static List<UserConfig> UserConfigs = new List<UserConfig>();

        static ConfigManager()
        {
            InitializeConfigPath();
        }

        private static void InitializeConfigPath()
        {
            // 优先尝试软件安装目录
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(appDirectory, ConfigFileName);

            try
            {
                // 测试是否有写入权限
                string testFile = Path.Combine(appDirectory, ".test_write");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                _configPath = configPath;
            }
            catch(Exception ex)
            {
                try
                {
                    // 如果没有权限，使用用户目录
                    string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(userDirectory, "KindomDataSync"); // 替换为你的应用名称

                    if (!Directory.Exists(appFolder))
                    {
                        Directory.CreateDirectory(appFolder);
                    }

                    _configPath = Path.Combine(appFolder, ConfigFileName);
                }
                catch (Exception ex2)
                {
                    LogManagerService.Instance.Log("Failed to access app directory for config: " + ExceptionLogHelper.Format(ex) + " | " + ExceptionLogHelper.Format(ex2));
                }
            }
        }

        public static void SaveConfig(string projPath, string username, string password, bool rememberPassword)
        {
            try
            {
                string projectName = Path.GetFileName(projPath);
                var userc = UserConfigs.FirstOrDefault(o => o.ProjectName == projectName);
                if (userc != null)
                {
                    userc.Username = username;
                    userc.Password = password;
                    userc.IsRememberPassword = rememberPassword;
                }
                else
                {
                    userc = new UserConfig
                    {
                        ProjectName = projectName,
                        Username = username,
                        Password = rememberPassword ? password : string.Empty,
                        IsRememberPassword = rememberPassword,
                    };
                }

                if (userc.IsRememberPassword && !UserConfigs.Contains(userc))
                {
                    UserConfigs.Add(userc);
                }
                else
                {
                    UserConfigs.Remove(userc);
                }
                string json = JsonHelper.ToJson(UserConfigs);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("SaveConfig failed !" + ExceptionLogHelper.Format(ex));
            }
        }

        public static UserConfig LoadConfig(string projPath)
        {
            string projectName = Path.GetFileName(projPath);
            if (!File.Exists(_configPath))
            {
                return null;
            }
            try
            {
                string json = File.ReadAllText(_configPath);

                UserConfigs = JsonHelper.ConvertFrom<List<UserConfig>>(json) ?? new List<UserConfig>();
                UserConfig userConfig = null;
                if (!string.IsNullOrEmpty(projectName))
                {
                    userConfig = UserConfigs.FirstOrDefault(u => u.ProjectName == projectName);
                }
                return userConfig;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class AdvancedSettingsConfig
    {
        public const string WellHeaderBatchSizeKey = "OnceSyncWellCount_WellHeader";
        public const string WellTrajectoryBatchSizeKey = "OnceSyncWellCount_WellTrajectory";
        public const string WellTrajectoryUploadConcurrencyKey = "well_trajectory_uploadConcurrency";
        public const string WellLogUploadConcurrencyKey = "well_log_uploadConcurrency";
        public const string WellLogBatchCurveCountKey = "well_log_batchCurveCount";
        public const string WellProductionUploadConcurrencyKey = "well_production_uploadConcurrency";
        public const string WellProductionBatchDailyDataCountKey = "well_production_batchDailyDataCount";
        public const string WellFormationBatchSizeKey = "OnceSyncWellCount_WellFormation";
        public const string ShowAdvancedSettingsMenuKey = "ShowAdvancedSettingsMenu";
        public const int DefaultWellHeaderBatchSize = 5000;
        public const int DefaultWellTrajectoryBatchSize = 3;
        public const int DefaultWellTrajectoryUploadConcurrency = 3;
        public const int DefaultWellLogUploadConcurrency = 3;
        public const int DefaultWellLogBatchCurveCount = 3;
        public const int DefaultWellProductionUploadConcurrency = 3;
        public const int DefaultWellProductionBatchDailyDataCount = 300;
        public const int DefaultWellFormationBatchSize = 5000;

        public static int GetWellHeaderBatchSize()
        {
            return GetBatchSize(WellHeaderBatchSizeKey, DefaultWellHeaderBatchSize);
        }

        public static int GetWellTrajectoryBatchSize()
        {
            return GetBatchSize(WellTrajectoryBatchSizeKey, DefaultWellTrajectoryBatchSize);
        }

        public static int GetWellTrajectoryUploadConcurrency()
        {
            return GetBatchSize(WellTrajectoryUploadConcurrencyKey, DefaultWellTrajectoryUploadConcurrency);
        }

        public static int GetWellLogUploadConcurrency()
        {
            return GetBatchSize(WellLogUploadConcurrencyKey, DefaultWellLogUploadConcurrency);
        }

        public static int GetWellLogBatchCurveCount()
        {
            return GetBatchSize(WellLogBatchCurveCountKey, DefaultWellLogBatchCurveCount);
        }

        public static int GetWellProductionUploadConcurrency()
        {
            return GetBatchSize(WellProductionUploadConcurrencyKey, DefaultWellProductionUploadConcurrency);
        }

        public static int GetWellProductionBatchDailyDataCount()
        {
            return GetBatchSize(WellProductionBatchDailyDataCountKey, DefaultWellProductionBatchDailyDataCount);
        }

        public static int GetWellFormationBatchSize()
        {
            return GetBatchSize(WellFormationBatchSizeKey, DefaultWellFormationBatchSize);
        }

        public static bool IsAdvancedSettingsMenuVisible()
        {
            string value = ConfigurationManager.AppSettings[ShowAdvancedSettingsMenuKey];
            if (bool.TryParse(value, out bool isVisible))
            {
                return isVisible;
            }

            return false;
        }

        private static int GetBatchSize(string key, int defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (!int.TryParse(value, out int batchSize) || batchSize < 1)
            {
                return defaultValue;
            }

            return batchSize;
        }

        public static void SaveWellHeaderBatchSize(int batchSize)
        {
            SaveBatchSize(WellHeaderBatchSizeKey, batchSize, DefaultWellHeaderBatchSize);
        }

        public static void SaveWellTrajectoryBatchSize(int batchSize)
        {
            SaveBatchSize(WellTrajectoryBatchSizeKey, batchSize, DefaultWellTrajectoryBatchSize);
        }

        public static void SaveWellTrajectoryUploadConcurrency(int uploadConcurrency)
        {
            SaveBatchSize(WellTrajectoryUploadConcurrencyKey, uploadConcurrency, DefaultWellTrajectoryUploadConcurrency);
        }

        public static void SaveWellLogUploadConcurrency(int uploadConcurrency)
        {
            SaveBatchSize(WellLogUploadConcurrencyKey, uploadConcurrency, DefaultWellLogUploadConcurrency);
        }

        public static void SaveWellLogBatchCurveCount(int batchCurveCount)
        {
            SaveBatchSize(WellLogBatchCurveCountKey, batchCurveCount, DefaultWellLogBatchCurveCount);
        }

        public static void SaveWellProductionUploadConcurrency(int uploadConcurrency)
        {
            SaveBatchSize(WellProductionUploadConcurrencyKey, uploadConcurrency, DefaultWellProductionUploadConcurrency);
        }

        public static void SaveWellProductionBatchDailyDataCount(int batchDailyDataCount)
        {
            SaveBatchSize(WellProductionBatchDailyDataCountKey, batchDailyDataCount, DefaultWellProductionBatchDailyDataCount);
        }

        public static void SaveWellFormationBatchSize(int batchSize)
        {
            SaveBatchSize(WellFormationBatchSizeKey, batchSize, DefaultWellFormationBatchSize);
        }

        private static void SaveBatchSize(string key, int batchSize, int defaultValue)
        {
            if (batchSize < 1)
            {
                batchSize = defaultValue;
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, batchSize.ToString());
            }
            else
            {
                config.AppSettings.Settings[key].Value = batchSize.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
