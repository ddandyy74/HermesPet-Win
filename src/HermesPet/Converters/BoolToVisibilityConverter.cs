using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HermesPet.Converters;

/// <summary>
/// 将布尔值转换为 Visibility。
/// true → Visible, false → Collapsed。
/// 
/// 支持 ConvertBack（用于双向绑定）。
/// 可通过 ConverterParameter 指定 false 时的行为：
/// - "Hidden" → false 时返回 Hidden
/// - 默认 → false 时返回 Collapsed
/// 
/// 参考 WPF 内置 BooleanToVisibilityConverter，但支持 Hidden 状态
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (boolValue)
            {
                return Visibility.Visible;
            }
            else
            {
                // 根据 ConverterParameter 决定 false 时的行为
                if (parameter is string paramString && paramString.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Hidden;
                }
                return Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
