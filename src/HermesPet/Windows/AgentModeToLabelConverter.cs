using System;
using System.Globalization;
using System.Windows.Data;
using HermesPet.Models;

namespace HermesPet.Windows
{
    /// <summary>
    /// AgentMode 到显示标签的转换器（M3.3 实现）
    /// 
    /// 将 AgentMode 枚举转换为用户友好的显示名称
    /// </summary>
    public class AgentModeToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AgentMode mode)
            {
                return mode.GetLabel();
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}