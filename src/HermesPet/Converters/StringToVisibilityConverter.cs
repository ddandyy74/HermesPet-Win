using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HermesPet.Converters
{
    /// <summary>
    /// 将字符串转换为 Visibility（非空字符串 → Visible，空字符串或 null → Collapsed）
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
