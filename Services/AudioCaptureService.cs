using NAudio.Wave;

namespace SoundMonitor.Services;

/// <summary>
/// 麦克风捕获服务
/// </summary>
public class AudioCaptureService : IDisposable
{
    private WaveInEvent? _waveIn;
    private bool _isCapturing;
    private readonly object _lock = new();

    /// <summary>
    /// 获取可用的麦克风设备列表
    /// </summary>
    public static List<string> GetDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var caps = WaveIn.GetCapabilities(i);
            devices.Add($"{i}: {caps.ProductName}");
        }
        return devices;
    }

    /// <summary>
    /// 启动捕获
    /// </summary>
    /// <param name="deviceNumber">设备编号，-1 表示默认设备</param>
    /// <param name="sampleRate">采样率，默认 44100</param>
    /// <param name="onDataAvailable">数据可用回调</param>
    public void Start(int deviceNumber = -1, int sampleRate = 44100,
        Action<byte[], int>? onDataAvailable = null)
    {
        lock (_lock)
        {
            if (_isCapturing) return;

            try
            {
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber >= 0 ? deviceNumber : 0,
                    WaveFormat = new WaveFormat(sampleRate, 16, 1) // 单声道，16位
                };

                _waveIn.DataAvailable += (sender, e) =>
                {
                    onDataAvailable?.Invoke(e.Buffer, e.BytesRecorded);
                };

                _waveIn.StartRecording();
                _isCapturing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动录音失败: {ex.Message}");
                _isCapturing = false;
            }
        }
    }

    /// <summary>
    /// 停止捕获
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (_waveIn != null)
            {
                try
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                }
                catch { }
                _waveIn = null;
            }
            _isCapturing = false;
        }
    }

    /// <summary>
    /// 是否正在捕获
    /// </summary>
    public bool IsCapturing => _isCapturing;

    public void Dispose()
    {
        Stop();
    }
}
