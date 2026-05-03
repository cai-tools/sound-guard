using SoundMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace SoundMonitor.Services;

/// <summary>
/// 系统通知服务
/// </summary>
public class NotificationService
{
    private const double NotifyThreshold = 50.0;
    private const double RearmThreshold = 45.0;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private readonly TimeSpan _notificationCooldown = TimeSpan.FromSeconds(5);
    private readonly NotifyIcon _notifyIcon;
    private bool _alertLatched;

    public NotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = SystemIcons.Information,
            Text = "SoundMonitor"
        };
    }

    /// <summary>
    /// 显示分贝警告通知
    /// </summary>
    public bool ShowDecibelNotification(double decibel, ThresholdLevel level)
    {
        // 回落到安全值后，重新允许下一次告警触发。
        if (decibel < RearmThreshold)
        {
            _alertLatched = false;
            return false;
        }

        if (decibel < NotifyThreshold) return false;
        if (_alertLatched) return false;

        // 冷却时间检查，避免频繁通知
        if (DateTime.Now - _lastNotificationTime < _notificationCooldown)
        {
            return false;
        }

        _lastNotificationTime = DateTime.Now;
        _alertLatched = true;

        var (title, message, icon) = level switch
        {
            ThresholdLevel.Loud => ("🔔 嘈杂提醒", $"当前音量 {decibel:F1} dB，请注意环境噪音", "⚠️"),
            ThresholdLevel.Danger => ("🚨 危险警告", $"当前音量 {decibel:F1} dB，可能损伤听力！", "🚨"),
            _ => ("📢 声音监控", $"当前音量 {decibel:F1} dB", "🔊")
        };

        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = level == ThresholdLevel.Danger
                ? ToolTipIcon.Error
                : ToolTipIcon.Warning;
            _notifyIcon.ShowBalloonTip(3000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"气泡通知失败: {ex.Message}");
        }

        return true;
    }

    /// <summary>
    /// 清除所有通知
    /// </summary>
    public static void ClearAll()
    {
        // 当前使用气泡通知，不写入通知中心历史。
    }
}
