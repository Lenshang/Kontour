using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KontourApp.Models;

public class FileExtensionFilterModel : INotifyPropertyChanged
{
    private bool _isEnabled;
    
    public string Extension { get; set; } = string.Empty;
    public string DisplayName => Extension.ToUpper();
    
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
