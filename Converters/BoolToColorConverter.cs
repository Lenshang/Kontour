using System.Globalization;

namespace KontourApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            // 更柔和的高亮色
            return Application.Current?.RequestedTheme == AppTheme.Dark 
                ? Color.FromArgb("#1E40AF")  // 深色模式：深蓝色
                : Color.FromArgb("#BFDBFE");  // 浅色模式：浅蓝色
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
