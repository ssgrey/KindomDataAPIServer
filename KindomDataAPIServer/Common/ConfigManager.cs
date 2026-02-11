using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    LogManagerService.Instance.Log("Failed to access app directory for config: " + ex.Message + " | " + ex2.Message);
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

                if (userc.IsRememberPassword)
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
                LogManagerService.Instance.Log("SaveConfig failed !" + ex.StackTrace + ex.Message);
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
}