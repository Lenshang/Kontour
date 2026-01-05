using KontourApp.Models;
using System.IO;

namespace KontourApp.Services;

public class FileExplorerService
{
    public List<DriveInfoModel> GetAvailableDrives()
    {
        var drives = new List<DriveInfoModel>();
        
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    drives.Add(new DriveInfoModel
                    {
                        Name = drive.Name,
                        DisplayName = string.IsNullOrEmpty(drive.VolumeLabel) 
                            ? $"{drive.Name} ({drive.DriveType})" 
                            : $"{drive.VolumeLabel} ({drive.Name})",
                        DriveType = drive.DriveType.ToString(),
                        TotalSize = drive.TotalSize,
                        AvailableSpace = drive.AvailableFreeSpace
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取驱动器失败: {ex.Message}");
        }

        return drives;
    }

    public List<FileItemModel> GetDirectoryContents(string path)
    {
        var items = new List<FileItemModel>();

        try
        {
            var dirInfo = new DirectoryInfo(path);

            // 添加文件夹（过滤掉 .previews 文件夹）
            foreach (var dir in dirInfo.GetDirectories())
            {
                try
                {
                    // 跳过 .previews 文件夹
                    if (dir.Name.Equals(".previews", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    items.Add(new FileItemModel
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        IsDirectory = true,
                        ModifiedDate = dir.LastWriteTime,
                        Icon = GetFolderIcon(dir.Name)
                    });
                }
                catch
                {
                    // 跳过无权限访问的文件夹
                }
            }

            // 添加文件
            foreach (var file in dirInfo.GetFiles())
            {
                try
                {
                    items.Add(new FileItemModel
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Extension = file.Extension,
                        Size = file.Length,
                        IsDirectory = false,
                        ModifiedDate = file.LastWriteTime,
                        Icon = GetFileIcon(file.Extension)
                    });
                }
                catch
                {
                    // 跳过无权限访问的文件
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取目录失败: {ex.Message}");
        }

        return items.OrderByDescending(x => x.IsDirectory)
                   .ThenBy(x => x.Name)
                   .ToList();
    }

    private string GetFileIcon(string extension)
    {
        return extension.ToLower() switch
        {
            ".txt" => "📝",
            ".doc" or ".docx" => "📄",
            ".pdf" => "📕",
            ".xls" or ".xlsx" => "📊",
            ".ppt" or ".pptx" => "📊",
            ".zip" or ".rar" or ".7z" => "📦",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
            ".mp3" or ".wav" or ".flac" or ".ogg" => "🎵",
            ".mp4" or ".avi" or ".mkv" => "🎬",
            ".exe" or ".msi" => "⚙️",
            ".cs" or ".java" or ".py" or ".js" => "💻",
            ".html" or ".css" => "🌐",
            ".nki" or ".nksn" or ".fxp" or ".nkm" => "🎹",
            ".mid" or ".midi" => "🎼",
            _ => "📄"
        };
    }

    private string GetFolderIcon(string folderName)
    {
        return folderName.ToLower() switch
        {
            "documents" or "文档" => "📚",
            "downloads" or "下载" => "⬇️",
            "pictures" or "图片" => "🖼️",
            "music" or "音乐" => "🎵",
            "videos" or "视频" => "🎬",
            "desktop" or "桌面" => "🖥️",
            _ => "📁"
        };
    }

    public bool CanAccessPath(string path)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            dir.GetDirectories();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
