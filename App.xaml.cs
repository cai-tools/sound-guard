using System.Windows;
using System.Windows.Threading;
using SoundMonitor.Services;

namespace SoundMonitor;

/// <summary>
/// 应用程序入口
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppLogger.Initialize();
        AppLogger.Info("应用启动");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Info("应用退出");

        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.Error("UI 线程未处理异常", e.Exception);
    }

    private static void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogger.Error("非 UI 线程未处理异常", ex);
        }
        else
        {
            AppLogger.Error("非 UI 线程未处理异常：未知异常对象");
        }
    }
}
