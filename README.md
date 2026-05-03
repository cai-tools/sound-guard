# 麦克风分贝监控器

Windows WPF 应用：实时采集麦克风、显示分贝曲线、阈值告警。

## 快速开始

```bash
dotnet restore
dotnet run --project .\SoundMonitor.csproj
```

## 阈值规则

- 安静：0-30
- 正常：30-50
- 嘈杂：50-70
- 危险：70+

## 通知规则

- 分贝 >= 50 才触发通知。
- 通知采用边沿触发：超过阈值后只提示一次。
- 回落到 45 以下后，下一次超过阈值才会再次提示。
- 专注模式下可通过应用内告警横幅继续看到提醒。

## 构建

```bash
dotnet build -c Release
```

## 依赖

- .NET 8 (WPF)
- NAudio
- LiveChartsCore
- CommunityToolkit.Mvvm
