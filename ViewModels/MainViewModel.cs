using System.Collections.ObjectModel;
using System.Windows.Threading;
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
    private readonly DispatcherTimer _timer;
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
    private string _selectedDevice = "默认麦克风";

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private int _selectedDeviceIndex;

    public ObservableCollection<string> Devices { get; } = new();

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
            SeparatorsPaint = new SolidColorPaint(new SKColor(230, 230, 230))
        }
    };

    public MainViewModel()
    {
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

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        LoadDevices();
    }

    /// <summary>
    /// 加载可用麦克风设备
    /// </summary>
    private void LoadDevices()
    {
        Devices.Clear();
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

        _audioService.Start(
            SelectedDeviceIndex,
            44100,
            OnAudioDataAvailable
        );

        _timer.Start();
        IsMonitoring = true;
        StatusText = "正在监控...";
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _timer.Stop();
        _audioService.Stop();
        IsMonitoring = false;
        StatusText = "已停止";
    }

    private void OnAudioDataAvailable(byte[] buffer, int bytesRecorded)
    {
        // 分贝计算在 UI 线程外完成
        double db = DecibelCalculator.CalculateDecibel(buffer, 2);

        // 更新到 UI（需要 Dispatcher）
        App.Current?.Dispatcher.Invoke(() =>
        {
            CurrentDecibel = db;
            UpdateLevel(db);
        });
    }

    private int _updateCounter = 0;

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
        _notificationService.ShowDecibelNotification(decibel, newLevel);
    }

    public void Dispose()
    {
        StopMonitoring();
        _audioService.Dispose();
        _timer.Stop();
    }
}
