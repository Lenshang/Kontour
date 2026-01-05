using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KontourApp.Models;

public class FileItemModel : INotifyPropertyChanged
{
    private bool _isSelected;
    
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsDirectory { get; set; }
    public string Icon { get; set; } = "📄";
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                System.Diagnostics.Debug.WriteLine($"[FileItemModel] {Name} IsSelected 设置为: {value}");
                OnPropertyChanged();
            }
        }
    }

    public string DisplaySize
    {
        get
        {
            if (IsDirectory) return "文件夹";
            
            if (Size < 1024) return $"{Size} B";
            if (Size < 1024 * 1024) return $"{Size / 1024.0:F2} KB";
            if (Size < 1024 * 1024 * 1024) return $"{Size / (1024.0 * 1024):F2} MB";
            return $"{Size / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    public string DisplayDate => ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
