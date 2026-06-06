using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HermesPet.Models;

namespace HermesPet.Windows
{
    /// <summary>
    /// ConnectionStatus 到颜色的转换器（M3.3 实现）
    /// 
    /// 用于在动态岛右上角显示连接状态指示器：
    /// - Connected: 绿色 (#4CAF50)
    /// - Connecting: 黄色 (#FFC107)
    /// - Disconnected: 灰色 (#9E9E9E)
    /// - Error: 红色 (#F44336)
    /// </summary>
    public class ConnectionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status)
            {
                return status switch
                {
                    ConnectionStatus.Connected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),   // 绿色
                    ConnectionStatus.Connecting => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),  // 黄色
                    ConnectionStatus.Disconnected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)), // 灰色
                    ConnectionStatus.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)),      // 红色
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)) // 默认灰色
                };
            }

            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}