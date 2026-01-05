using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KontourApp.Models;

public class FavoriteItemModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Icon => "⭐";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
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
