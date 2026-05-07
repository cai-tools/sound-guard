using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SoundMonitor.Models;
using SoundMonitor.Services;

namespace SoundMonitor.ViewModels;

/// <summary>
/// 主视图模型
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly AudioCaptureService _audioService;
    private readonly NotificationService _notificationService;
    private readonly ThresholdConfig _thresholdConfig;
    private readonly ObservableCollection<ObservableValue> _decibelValues;
    private const int MaxDataPoints = 100;

    [ObservableProperty]
    private double _currentDecibel;

    [ObservableProperty]
    private ThresholdLevel _currentLevel = ThresholdLevel.Quiet;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _levelText = "安静";

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _isAlertVisible;

    [ObservableProperty]
    private string _alertText = string.Empty;

    [ObservableProperty]
    private int _selectedDeviceIndex;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string _quietMaxInput = string.Empty;

    [ObservableProperty]
    private string _normalMaxInput = string.Empty;

    [ObservableProperty]
    private string _loudMaxInput = string.Empty;

    [ObservableProperty]
    private int _notificationThresholdIndex;

    public ObservableCollection<string> Devices { get; } = new();

    public string QuietRangeText => $"安静 0-{_thresholdConfig.QuietMax:F0}dB";
    public string NormalRangeText => $"正常 {_thresholdConfig.QuietMax:F0}-{_thresholdConfig.NormalMax:F0}dB";
    public string LoudRangeText => $"嘈杂 {_thresholdConfig.NormalMax:F0}-{_thresholdConfig.LoudMax:F0}dB";
    public string DangerRangeText => $"危险 {_thresholdConfig.LoudMax:F0}+dB";

    public ISeries[] Series { get; }

    public Axis[] XAxes { get; } = {
        new Axis
        {
            Name = "时间 (100ms)",
            NamePaint = new SolidColorPaint(SKColors.Gray),
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            MinLimit = 0,
            MaxLimit = MaxDataPoints,
            IsVisible = false
        }
    };

    public Axis[] YAxes { get; } = {
        new Axis
        {
            Name = "分贝 (dB)",
            NamePaint = new SolidColorPaint(SKColors.Gray),
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            MinLimit = 0,
            MaxLimit = 120,
            MinStep = 20,
            ForceStepToMin = true,
            SeparatorsPaint = new SolidColorPaint(new SKColor(230, 230, 230))
        }
    };

    public MainViewModel()
    {
        AppLogger.Initialize();
        _audioService = new AudioCaptureService();
        _notificationService = new NotificationService();
        _thresholdConfig = new ThresholdConfig();

        _decibelValues = new ObservableCollection<ObservableValue>();
        for (int i = 0; i < MaxDataPoints; i++)
        {
            _decibelValues.Add(new ObservableValue(0));
        }

        Series = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = _decibelValues,
                Fill = new SolidColorPaint(new SKColor(33, 150, 243, 50)),
                Stroke = new SolidColorPaint(new SKColor(33, 150, 243), 2),
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        LoadDevices();
        InitializeSettingsInputs();
        AppLogger.Info("主视图模型初始化完成");
    }

    private void InitializeSettingsInputs()
    {
        QuietMaxInput = _thresholdConfig.QuietMax.ToString("F0", CultureInfo.InvariantCulture);
        NormalMaxInput = _thresholdConfig.NormalMax.ToString("F0", CultureInfo.InvariantCulture);
        LoudMaxInput = _thresholdConfig.LoudMax.ToString("F0", CultureInfo.InvariantCulture);
        NotificationThresholdIndex = GetThresholdIndex(_thresholdConfig.WarningLevel);
    }

    /// <summary>
    /// 加载可用麦克风设备
    /// </summary>
    private void LoadDevices()
    {
        Devices.Clear();
        Devices.Add("系统默认设备");

        var devices = AudioCaptureService.GetDevices();
        if (devices.Count == 0)
        {
            Devices.Add("未检测到麦克风");
        }
        else
        {
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
        }

        // 优先选择麦克风阵列（中英文名称），未找到则回退到系统默认设备。
        SelectedDeviceIndex = 0;
        for (int i = 1; i < Devices.Count; i++)
        {
            var name = Devices[i];
            if (name.Contains("麦克风阵列", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("microphone array", StringComparison.OrdinalIgnoreCase))
            {
                SelectedDeviceIndex = i;
                break;
            }
        }

        AppLogger.Info($"设备加载完成，可用输入设备数量: {Devices.Count - 1}");
    }

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            StopMonitoring();
        }
        else
        {
            StartMonitoring();
        }
    }

    [RelayCommand]
    private void StartMonitoring()
    {
        if (IsMonitoring) return;

        var deviceNumber = SelectedDeviceIndex <= 0 ? -1 : SelectedDeviceIndex - 1;

        _audioService.Start(
            deviceNumber,
            44100,
            OnAudioDataAvailable
        );

        if (!_audioService.IsCapturing)
        {
            IsMonitoring = false;
            StatusText = "启动失败：请检查麦克风设备或权限";
            AppLogger.Warn($"开始监控失败，设备索引: {deviceNumber}");
            return;
        }

        IsMonitoring = true;
        StatusText = "正在监控...";
        AppLogger.Info($"开始监控，设备索引: {deviceNumber}");
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _audioService.Stop();
        IsMonitoring = false;
        StatusText = "已停止";
        AppLogger.Info("停止监控");
    }

    [RelayCommand]
    private void OpenLog()
    {
        try
        {
            AppLogger.Initialize();
            var logPath = AppLogger.CurrentLogFilePath;

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, string.Empty);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = logPath,
                UseShellExecute = true
            });

            StatusText = "已打开日志";
        }
        catch (Exception ex)
        {
            StatusText = "打开日志失败";
            AppLogger.Error("打开日志失败", ex);
        }
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void ApplySettings()
    {
        if (!TryParseInput(QuietMaxInput, out var quietMax) ||
            !TryParseInput(NormalMaxInput, out var normalMax) ||
            !TryParseInput(LoudMaxInput, out var loudMax))
        {
            StatusText = "设置无效：请输入有效数字";
            AppLogger.Warn("应用设置失败：分段阈值存在无效输入");
            return;
        }

        if (!(quietMax > 0 && quietMax < normalMax && normalMax < loudMax && loudMax <= 120))
        {
            StatusText = "设置无效：请确保 0 < 安静 < 正常 < 嘈杂 <= 120";
            AppLogger.Warn($"应用设置失败：阈值顺序错误，quiet={quietMax}, normal={normalMax}, loud={loudMax}");
            return;
        }

        _thresholdConfig.QuietMax = quietMax;
        _thresholdConfig.NormalMax = normalMax;
        _thresholdConfig.LoudMax = loudMax;
        _thresholdConfig.WarningLevel = GetThresholdLevelFromIndex(NotificationThresholdIndex);

        OnPropertyChanged(nameof(QuietRangeText));
        OnPropertyChanged(nameof(NormalRangeText));
        OnPropertyChanged(nameof(LoudRangeText));
        OnPropertyChanged(nameof(DangerRangeText));

        StatusText = "设置已应用";
        AppLogger.Info(
            $"设置已更新，quiet={quietMax:F0}, normal={normalMax:F0}, loud={loudMax:F0}, notify={_thresholdConfig.WarningLevel}");
    }

    private static bool TryParseInput(string input, out double value)
    {
        return double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static ThresholdLevel GetThresholdLevelFromIndex(int index)
    {
        return index switch
        {
            0 => ThresholdLevel.Normal,
            2 => ThresholdLevel.Danger,
            _ => ThresholdLevel.Loud
        };
    }

    private static int GetThresholdIndex(ThresholdLevel level)
    {
        return level switch
        {
            ThresholdLevel.Normal => 0,
            ThresholdLevel.Danger => 2,
            _ => 1
        };
    }

    private void OnAudioDataAvailable(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded <= 0) return;

        // 分贝计算在 UI 线程外完成
        double db = DecibelCalculator.CalculateDecibel(buffer, 2, bytesRecorded);

        // 异步更新 UI，避免阻塞采集回调线程。
        var dispatcher = App.Current?.Dispatcher;
        if (dispatcher == null) return;

        dispatcher.BeginInvoke(() =>
        {
            CurrentDecibel = db;
            UpdateLevel(db);
        });
    }

    private int _updateCounter = 0;
    private DateTime _lastInAppAlertTime = DateTime.MinValue;
    private readonly TimeSpan _inAppAlertCooldown = TimeSpan.FromSeconds(5);

    private void UpdateLevel(double decibel)
    {
        var newLevel = _thresholdConfig.GetLevel(decibel);
        CurrentLevel = newLevel;

        // 更新显示
        (LevelText, StatusText) = newLevel switch
        {
            ThresholdLevel.Quiet => ("安静", IsMonitoring ? "正在监控..." : "就绪"),
            ThresholdLevel.Normal => ("正常", "环境音量正常"),
            ThresholdLevel.Loud => ("嘈杂", "⚠️ 环境较吵"),
            ThresholdLevel.Danger => ("危险", "🚨 请保护听力！"),
            _ => ("未知", "错误")
        };

        // 更新图表数据（每2帧更新一次，避免太快）
        _updateCounter++;
        if (_updateCounter % 2 == 0)
        {
            _decibelValues.RemoveAt(0);
            _decibelValues.Add(new ObservableValue(decibel));
        }

        // 触发通知
        _notificationService.ShowDecibelNotification(decibel, newLevel, _thresholdConfig.WarningLevel);

        bool isWarningLevel = newLevel >= _thresholdConfig.WarningLevel;
        var now = DateTime.UtcNow;
        if (isWarningLevel && now - _lastInAppAlertTime >= _inAppAlertCooldown)
        {
            _lastInAppAlertTime = now;
            AlertText = newLevel switch
            {
                ThresholdLevel.Danger => $"危险提醒: {decibel:F1} dB",
                ThresholdLevel.Loud => $"嘈杂提醒: {decibel:F1} dB",
                _ => $"音量提醒: {decibel:F1} dB"
            };
            IsAlertVisible = true;
        }
        else if (!isWarningLevel)
        {
            IsAlertVisible = false;
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        _notificationService.Dispose();
        _audioService.Dispose();
        AppLogger.Info("主视图模型释放完成");
    }
}
