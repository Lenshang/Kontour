namespace KontourApp.Models;

public class DriveInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long AvailableSpace { get; set; }
    
    public string DisplaySize
    {
        get
        {
            if (TotalSize < 1024 * 1024 * 1024)
                return $"{TotalSize / (1024.0 * 1024):F2} MB";
            return $"{TotalSize / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    public string DisplayFreeSpace
    {
        get
        {
            if (AvailableSpace < 1024 * 1024 * 1024)
                return $"{AvailableSpace / (1024.0 * 1024):F2} MB";
            return $"{AvailableSpace / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    public double UsagePercentage
    {
        get
        {
            if (TotalSize == 0) return 0;
            return ((TotalSize - AvailableSpace) / (double)TotalSize) * 100;
        }
    }
}
