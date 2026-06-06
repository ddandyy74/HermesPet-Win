using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HermesPet.Models;

namespace HermesPet.Windows;

/// <summary>
/// 动态岛状态 → 背景色转换器
/// 
/// 状态颜色映射：
/// - Idle/Hovering: 黑色（#1C1C1E）
/// - Streaming: 深蓝色（#0A84FF）
/// - ToolProgress: 紫色（#BF5AF2）
/// - VoiceActive: 绿色（#30D158）
/// - Permission: 橙色（#FF9F0A）
/// - Error: 红色（#FF453A）
/// </summary>
public class StateToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IslandState state)
        {
            return state switch
            {
                IslandState.Idle => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1C, 0x1C, 0x1E)),
                IslandState.Hovering => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1C, 0x1C, 0x1E)),
                IslandState.Streaming => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0A, 0x84, 0xFF)),
                IslandState.ToolProgress => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xBF, 0x5A, 0xF2)),
                IslandState.VoiceActive => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0xD1, 0x58)),
                IslandState.Permission => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x9F, 0x0A)),
                IslandState.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x45, 0x3A)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1C, 0x1C, 0x1E))
            };
        }

        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1C, 0x1C, 0x1E));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
