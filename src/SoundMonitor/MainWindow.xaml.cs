using System.Windows;
using SoundMonitor.ViewModels;

namespace SoundMonitor;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && !vm.IsMonitoring)
        {
            vm.StartMonitoringCommand.Execute(null);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is MainViewModel vm)
        {
            vm.Dispose();
        }
        base.OnClosed(e);
    }
}
