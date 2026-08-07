using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

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
        private const string ConfigFileName = "userconfig.json";
        private static string _configPath;
        private static List<UserConfig> _userConfigs = new List<UserConfig>();

        static ConfigManager()
        {
            InitializeConfigPath();
        }

        private static void InitializeConfigPath()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(appDirectory, ConfigFileName);
            try
            {
                string testFile = Path.Combine(appDirectory, ".test_write");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                _configPath = configPath;
            }
            catch (Exception appDirectoryException)
            {
                try
                {
                    string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(userDirectory, "KindomDataSync");
                    Directory.CreateDirectory(appFolder);
                    _configPath = Path.Combine(appFolder, ConfigFileName);
                }
                catch (Exception userDirectoryException)
                {
                    LogManagerService.Instance.Log("Failed to access app directory for config: " +
                        ExceptionLogHelper.Format(appDirectoryException) + " | " +
                        ExceptionLogHelper.Format(userDirectoryException));
                }
            }
        }

        public static void SaveConfig(string projPath, string username, string password, bool rememberPassword)
        {
            try
            {
                string projectName = Path.GetFileName(projPath);
                UserConfig userConfig = _userConfigs.FirstOrDefault(item => item.ProjectName == projectName);
                if (userConfig == null)
                {
                    userConfig = new UserConfig { ProjectName = projectName };
                }
                userConfig.Username = username;
                userConfig.Password = rememberPassword ? password : string.Empty;
                userConfig.IsRememberPassword = rememberPassword;

                _userConfigs.RemoveAll(item => item.ProjectName == projectName);
                if (rememberPassword)
                {
                    _userConfigs.Add(userConfig);
                }
                File.WriteAllText(_configPath, JsonHelper.ToJson(_userConfigs));
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log("SaveConfig failed: " + ExceptionLogHelper.Format(ex));
            }
        }

        public static UserConfig LoadConfig(string projPath)
        {
            if (!File.Exists(_configPath))
            {
                return null;
            }
            try
            {
                _userConfigs = JsonHelper.ConvertFrom<List<UserConfig>>(File.ReadAllText(_configPath))
                    ?? new List<UserConfig>();
                string projectName = Path.GetFileName(projPath);
                return string.IsNullOrEmpty(projectName)
                    ? null
                    : _userConfigs.FirstOrDefault(item => item.ProjectName == projectName);
            }
            catch
            {
                return null;
            }
        }
    }

    public static class SyncSelectionConfig
    {
        private const string RequireFormationAndLogSelectionKey = "RequireFormationAndLogSelection";

        public static bool IsFormationAndLogSelectionRequired()
        {
            string value = ConfigurationManager.AppSettings[RequireFormationAndLogSelectionKey];
            return !bool.TryParse(value, out bool required) || required;
        }
    }
}
