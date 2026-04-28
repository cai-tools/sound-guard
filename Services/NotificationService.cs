using Microsoft.Toolkit.Uwp.Notifications;
using SoundMonitor.Models;

namespace SoundMonitor.Services;

/// <summary>
/// 系统通知服务
/// </summary>
public class NotificationService
{
    private ThresholdLevel _lastNotifiedLevel = ThresholdLevel.Quiet;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private readonly TimeSpan _notificationCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 显示分贝警告通知
    /// </summary>
    public void ShowDecibelNotification(double decibel, ThresholdLevel level)
    {
        // 冷却时间检查，避免频繁通知
        if (DateTime.Now - _lastNotificationTime < _notificationCooldown)
            return;

        if (level <= ThresholdLevel.Normal) return; // 只在嘈杂以上通知

        _lastNotificationTime = DateTime.Now;
        _lastNotifiedLevel = level;

        var (title, message, icon) = level switch
        {
            ThresholdLevel.Loud => ("🔔 嘈杂提醒", $"当前音量 {decibel:F1} dB，请注意环境噪音", "⚠️"),
            ThresholdLevel.Danger => ("🚨 危险警告", $"当前音量 {decibel:F1} dB，可能损伤听力！", "🚨"),
            _ => ("📢 声音监控", $"当前音量 {decibel:F1} dB", "🔊")
        };

        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddAttributionText($"SoundMonitor - {decibel:F1} dB")
                .Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"通知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除所有通知
    /// </summary>
    public static void ClearAll()
    {
        ToastNotificationManagerCompat.History.Clear();
    }
}
