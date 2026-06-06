using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HermesPet.Models;

namespace HermesPet.Windows;

/// <summary>
/// 动态岛状态 → 可见性转换器
/// 
/// 用法：
/// Visibility="{Binding State, Converter={StaticResource StateToVisibilityConverter}, ConverterParameter=Idle,Hovering}"
/// 
/// ConverterParameter: 逗号分隔的状态列表，如果当前状态在列表中则返回 Visible，否则返回 Collapsed
/// </summary>
public class StateToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IslandState state && parameter is string stateList)
        {
            // 解析参数中的状态列表
            var states = stateList.Split(',');

            foreach (var s in states)
            {
                if (Enum.TryParse<IslandState>(s.Trim(), out var targetState))
                {
                    if (state == targetState)
                    {
                        return Visibility.Visible;
                    }
                }
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
