# 麦克风分贝监控器

一个 Windows 原生的 WPF 应用，实时监听麦克风输入并显示分贝曲线，支持多级阈值警告。

## 功能特性

- 🎤 实时麦克风音频捕获
- 📊 分贝值实时显示（0-120 dB）
- 📈 实时分贝曲线绘制（最近 10 秒）
- 🚦 多级阈值警告：
  - 安静（0-40 dB）：绿色
  - 正常（40-60 dB）：蓝色
  - 嘈杂（60-80 dB）：橙色 + 通知
  - 危险（80+ dB）：红色 + 警报通知
- 🔔 Windows Toast 系统通知

## 技术栈

- **.NET 8** - WPF 框架
- **NAudio 2.2** - 音频捕获
- **LiveChartsCore 2.0** - 实时图表
- **CommunityToolkit.Mvvm** - MVVM 模式
- **Microsoft.Toolkit.Uwp.Notifications** - Windows 通知

## 运行

```bash
# 恢复依赖
dotnet restore

# 运行
dotnet run
```

## 构建

```bash
dotnet build -c Release
```

构建输出位于 `bin/Release/net8.0-windows10.0.19041.0/` 目录。

## 项目结构

```
SoundMonitor/
├── App.xaml(.cs)                 # 应用程序入口
├── MainWindow.xaml(.cs)          # 主窗口 UI
├── ViewModels/
│   └── MainViewModel.cs          # 视图模型
├── Services/
│   ├── AudioCaptureService.cs    # 麦克风捕获
│   ├── DecibelCalculator.cs      # 分贝计算
│   └── NotificationService.cs    # 通知服务
├── Models/
│   └── ThresholdLevel.cs        # 阈值级别
└── Converters/
    └── DecibelToColorConverter.cs # UI 转换器
```
