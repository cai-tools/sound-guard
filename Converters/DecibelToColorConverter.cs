using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SoundMonitor.Models;

namespace SoundMonitor.Converters;

/// <summary>
/// 分贝级别到颜色转换器
/// </summary>
public class DecibelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ThresholdLevel level)
        {
            return level switch
            {
                ThresholdLevel.Quiet => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // 绿色
                ThresholdLevel.Normal => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色
                ThresholdLevel.Loud => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // 橙色
                ThresholdLevel.Danger => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // 红色
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到按钮文本转换器
/// </summary>
public class BoolToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "停止监控" : "开始监控";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
