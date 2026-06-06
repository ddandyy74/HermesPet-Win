using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HermesPet.Models;

namespace HermesPet.Converters;

/// <summary>
/// 将 MessageRole 转换为 HorizontalAlignment。
/// User 消息右对齐，Assistant/System 消息左对齐。
/// 
/// 参考 macOS：ChatView.swift 中消息气泡的 alignment 逻辑
/// </summary>
public class RoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role switch
            {
                MessageRole.User => System.Windows.HorizontalAlignment.Right,
                MessageRole.Assistant => System.Windows.HorizontalAlignment.Left,
                MessageRole.System => System.Windows.HorizontalAlignment.Left,
                _ => System.Windows.HorizontalAlignment.Left
            };
        }
        return System.Windows.HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
