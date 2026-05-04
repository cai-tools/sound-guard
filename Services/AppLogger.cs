using System.Globalization;
using System.IO;
using System.Text;

namespace SoundMonitor.Services;

/// <summary>
/// 轻量文件日志：固定单文件，跨天自动覆盖。
/// </summary>
public static class AppLogger
{
    private static readonly object SyncRoot = new();

    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SoundMonitor",
        "logs");

    public static string CurrentLogFilePath => Path.Combine(
        LogDirectory,
        "sound-monitor.log");

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            EnsureCurrentDayLogFile();
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var finalMessage = ex == null
            ? message
            : $"{message} | {ex.GetType().Name}: {ex.Message}";
        Write("ERROR", finalMessage);
    }

    private static void Write(string level, string message)
    {
        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            EnsureCurrentDayLogFile();

            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}");

            File.AppendAllText(CurrentLogFilePath, line, Encoding.UTF8);
        }
    }

    private static void EnsureCurrentDayLogFile()
    {
        if (!File.Exists(CurrentLogFilePath))
        {
            return;
        }

        var lastWriteDate = File.GetLastWriteTimeUtc(CurrentLogFilePath).Date;
        var currentDate = DateTime.UtcNow.Date;
        if (lastWriteDate == currentDate)
        {
            return;
        }

        File.WriteAllText(CurrentLogFilePath, string.Empty, Encoding.UTF8);
    }
}
