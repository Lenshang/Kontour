using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace KontourApp.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private CultureInfo _currentCulture;

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;

    private LocalizationService()
    {
        _currentCulture = CultureInfo.CurrentCulture;
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;
                
                OnPropertyChanged();
                // 通知索引器属性变化
                OnPropertyChanged("Item[]");
                LanguageChanged?.Invoke();
                
                System.Diagnostics.Debug.WriteLine($"语言已切换到: {value.Name}");
            }
        }
    }

    public string this[string key]
    {
        get
        {
            try
            {
                var value = Resources.Localization.AppResources.ResourceManager.GetString(key, _currentCulture);
                return value ?? key;
            }
            catch
            {
                return key;
            }
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
