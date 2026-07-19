using System.Diagnostics;
using Microsoft.Win32;

namespace SoundMonitor.Services;

/// <summary>
/// 当前用户自启动注册服务。
/// </summary>
public sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SoundMonitor";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key == null)
        {
            return false;
        }

        var registeredValue = key.GetValue(ValueName) as string;
        if (string.IsNullOrWhiteSpace(registeredValue))
        {
            return false;
        }

        return string.Equals(registeredValue.Trim(), BuildCommand(GetCurrentExecutablePath()), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("无法打开自启动注册表项");

        if (enabled)
        {
            key.SetValue(ValueName, BuildCommand(GetCurrentExecutablePath()), RegistryValueKind.String);
            return;
        }

        if (key.GetValue(ValueName) != null)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string GetCurrentExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            return processPath;
        }

        var fallbackPath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            return fallbackPath;
        }

        throw new InvalidOperationException("无法获取当前程序路径");
    }

    private static string BuildCommand(string executablePath)
    {
        return $"\"{executablePath}\"";
    }
}