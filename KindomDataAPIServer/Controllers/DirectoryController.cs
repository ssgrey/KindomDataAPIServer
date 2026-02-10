using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace KindomDataAPIServer.Controllers
{
    [RoutePrefix("api/directory")]
    public class DirectoryController : ApiController
    {
        [HttpGet]
        [Route("list")]
        public IHttpActionResult GetDirectoryStructure([FromUri] string path = null)
        {
            try
            {
                
                // 如果没有指定路径，使用当前用户的主目录
                string rootPath = string.IsNullOrEmpty(path)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : path;

                // 验证路径是否存在
                if (!Directory.Exists(rootPath))
                {
                    return NotFound();
                }

                var result = new DirectoryInfo(rootPath);

                // 只读取两级目录结构
                var directoryStructure = new
                {
                    Path = result.FullName,
                    Name = result.Name,
                    LastModified = result.LastWriteTime,
                    SubDirectories = GetSubDirectories(result, 2),
                    Files = GetFiles(result)
                };

                return Ok(directoryStructure);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private List<object> GetSubDirectories(DirectoryInfo directory, int maxDepth, int currentDepth = 1)
        {
            var directories = new List<object>();

            if (currentDepth > maxDepth)
                return directories;

            try
            {
                foreach (var subDir in directory.GetDirectories())
                {
                    try
                    {
                        var dirInfo = new
                        {
                            Path = subDir.FullName,
                            Name = subDir.Name,
                            LastModified = subDir.LastWriteTime,
                            SubDirectories = currentDepth < maxDepth
                                ? GetSubDirectories(subDir, maxDepth, currentDepth + 1)
                                : new List<object>(),
                            FileCount = subDir.GetFiles().Length,
                            DirectoryCount = subDir.GetDirectories().Length
                        };
                        directories.Add(dirInfo);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 跳过没有权限的目录
                        directories.Add(new
                        {
                            Path = subDir.FullName,
                            Name = subDir.Name,
                            Error = "没有访问权限",
                            SubDirectories = new List<object>()
                        });
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 整个目录都没有权限
            }

            return directories;
        }

        private List<object> GetFiles(DirectoryInfo directory)
        {
            var files = new List<object>();

            try
            {
                foreach (var file in directory.GetFiles())
                {
                    files.Add(new
                    {
                        Name = file.Name,
                        FullName = file.FullName,
                        Extension = file.Extension,
                        Size = file.Length,
                        LastModified = file.LastWriteTime,
                        IsReadOnly = file.IsReadOnly
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 跳过没有权限的文件
            }

            return files;
        }

        [HttpGet]
        [Route("drives")]
        public IHttpActionResult GetSystemDrives()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new
                    {
                        Name = d.Name,
                        DriveType = d.DriveType.ToString(),
                        TotalSize = d.TotalSize,
                        TotalSizeGB = Math.Round(d.TotalSize / 1073741824.0, 2),
                        AvailableFreeSpace = d.AvailableFreeSpace,
                        AvailableFreeSpaceGB = Math.Round(d.AvailableFreeSpace / 1073741824.0, 2),
                        UsedSpaceGB = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1073741824.0, 2),
                        DriveFormat = d.DriveFormat,
                        VolumeLabel = d.VolumeLabel,
                        IsReady = d.IsReady
                    })
                    .ToList();

                return Ok(drives);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("specialfolders")]
        public IHttpActionResult GetSpecialFolders()
        {
            var folders = new Dictionary<string, string>
            {
                { "Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                { "Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                { "Downloads", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads" },
                { "Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                { "Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                { "Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                { "ProgramFiles", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
                { "ProgramFilesX86", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) }
            };

            return Ok(folders);
        }
    }
}