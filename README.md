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

## 分发打包

在 Windows PowerShell 执行：

```powershell
./scripts/build-release.ps1 -Version 0.0.1
```

会在 `dist/` 产出三类可分发文件：

- `dist/single-exe-win-x64/SoundMonitor.exe`：单文件可执行版
- `dist/SoundMonitor-portable-win-x64-v0.0.1.zip`：便携版 ZIP（解压即用）
- `dist/installer/*.exe`：安装程序（需 Inno Setup）

## GitHub CI 产物

- PR 流水线会上传：单文件 EXE、便携 ZIP、安装程序 EXE
- main 分支版本号变更时会自动创建 Release，并附带上述 3 个产物

## 依赖

- .NET 8 (WPF)
- NAudio
- LiveChartsCore
- CommunityToolkit.Mvvm
