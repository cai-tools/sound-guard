namespace SoundMonitor.Models;

/// <summary>
/// 阈值级别枚举
/// </summary>
public enum ThresholdLevel
{
    /// <summary>安静：0-30 dB</summary>
    Quiet = 0,

    /// <summary>正常：30-60 dB</summary>
    Normal = 1,

    /// <summary>嘈杂：60-80 dB</summary>
    Loud = 2,

    /// <summary>危险：80+ dB</summary>
    Danger = 3
}

/// <summary>
/// 阈值级别配置
/// </summary>
public class ThresholdConfig
{
    public double QuietMax { get; set; } = 30;
    public double NormalMax { get; set; } = 60;
    public double LoudMax { get; set; } = 80;

    public ThresholdLevel WarningLevel { get; set; } = ThresholdLevel.Loud;

    public ThresholdLevel GetLevel(double decibel)
    {
        if (decibel < QuietMax) return ThresholdLevel.Quiet;
        if (decibel < NormalMax) return ThresholdLevel.Normal;
        if (decibel < LoudMax) return ThresholdLevel.Loud;
        return ThresholdLevel.Danger;
    }
}
