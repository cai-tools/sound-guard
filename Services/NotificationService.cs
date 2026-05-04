using SoundMonitor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace SoundMonitor.Services;

/// <summary>
/// 系统通知服务
/// </summary>
public class NotificationService : IDisposable
{
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private readonly TimeSpan _notificationCooldown = TimeSpan.FromSeconds(5);
    private NotifyIcon? _notifyIcon;
    private bool _alertLatched;

    private NotifyIcon EnsureNotifyIcon()
    {
        _notifyIcon ??= new NotifyIcon
        {
            Visible = true,
            Icon = SystemIcons.Information,
            Text = "SoundMonitor"
        };

        return _notifyIcon;
    }

    /// <summary>
    /// 显示分贝警告通知
    /// </summary>
    public bool ShowDecibelNotification(double decibel, ThresholdLevel level)
    {
        // 回落到安全级别后，重新允许下一次告警触发。
        if (level < ThresholdLevel.Loud)
        {
            _alertLatched = false;
            return false;
        }

        if (_alertLatched) return false;

        // 冷却时间检查，避免频繁通知
        var now = DateTime.UtcNow;
        if (now - _lastNotificationTime < _notificationCooldown)
        {
            return false;
        }

        _lastNotificationTime = now;
        _alertLatched = true;

        var (title, message) = level switch
        {
            ThresholdLevel.Loud => ("🔔 嘈杂提醒", $"当前音量 {decibel:F1} dB，请注意环境噪音"),
            ThresholdLevel.Danger => ("🚨 危险警告", $"当前音量 {decibel:F1} dB，可能损伤听力！"),
            _ => ("📢 声音监控", $"当前音量 {decibel:F1} dB")
        };

        try
        {
            var notifyIcon = EnsureNotifyIcon();
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipIcon = level == ThresholdLevel.Danger
                ? ToolTipIcon.Error
                : ToolTipIcon.Warning;
            notifyIcon.ShowBalloonTip(3000);
            AppLogger.Warn($"触发系统通知，级别: {level}, 分贝: {decibel:F1}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"气泡通知失败: {ex.Message}");
            AppLogger.Error("系统通知失败", ex);
        }

        return true;
    }

    public void Dispose()
    {
        if (_notifyIcon == null) return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _notifyIcon = null;
        AppLogger.Info("通知服务资源已释放");
    }
}
