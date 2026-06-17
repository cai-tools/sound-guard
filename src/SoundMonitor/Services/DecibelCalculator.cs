namespace SoundMonitor.Services;

/// <summary>
/// 分贝计算器
/// </summary>
public static class DecibelCalculator
{
    private const double MinRms = 1e-10;
    private const double DefaultCalibrationOffsetDb = 94.0;

    /// <summary>
    /// 计算分贝值（dB）
    /// </summary>
    /// <param name="samples">音频采样数组</param>
    /// <returns>分贝值（dB）</returns>
    public static double CalculateDecibel(float[] samples)
    {
        if (samples == null || samples.Length == 0)
            return 0;

        // 计算 RMS（均方根）
        double sum = 0;
        foreach (var sample in samples)
        {
            sum += sample * sample;
        }
        double rms = Math.Sqrt(sum / samples.Length);

        // 转换为分贝
        // 使用 20 * log10(rms / reference) 公式
        // 由于麦克风不是校准的 SPL 设备，我们使用相对分贝
        if (rms < MinRms)
            return 0;

        // samples 已经在 [-1, 1] 区间，先计算 dBFS，再用校准偏移估算 dB SPL。
        double db = 20 * Math.Log10(rms);
        db += DefaultCalibrationOffsetDb;
        return Math.Clamp(db, 0, 120);
    }

    /// <summary>
    /// 计算分贝值（从字节数组）
    /// </summary>
    public static double CalculateDecibel(byte[] audioData, int bytesPerSample)
    {
        if (audioData == null)
            return 0;

        return CalculateDecibel(audioData, bytesPerSample, audioData.Length);
    }

    /// <summary>
    /// 计算分贝值（从字节数组）
    /// </summary>
    /// <param name="audioData">音频字节数据</param>
    /// <param name="bytesPerSample">每个采样占用字节数</param>
    /// <param name="bytesRecorded">本次实际写入的字节数</param>
    public static double CalculateDecibel(byte[] audioData, int bytesPerSample, int bytesRecorded)
    {
        if (audioData == null || audioData.Length == 0 || bytesRecorded <= 0 || bytesPerSample <= 0)
            return 0;

        int effectiveBytes = Math.Min(bytesRecorded, audioData.Length);
        int sampleCount = effectiveBytes / bytesPerSample;
        if (sampleCount <= 0)
            return 0;

        double sum = 0;

        if (bytesPerSample == 2)
        {
            int offset = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(audioData[offset] | (audioData[offset + 1] << 8));
                double normalized = sample / 32768d;
                sum += normalized * normalized;
                offset += 2;
            }
        }
        else if (bytesPerSample == 4)
        {
            int offset = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                int sampleBits = audioData[offset] | (audioData[offset + 1] << 8) |
                                 (audioData[offset + 2] << 16) | (audioData[offset + 3] << 24);
                double normalized = BitConverter.Int32BitsToSingle(sampleBits);
                sum += normalized * normalized;
                offset += 4;
            }
        }
        else
        {
            return 0;
        }

        double rms = Math.Sqrt(sum / sampleCount);
        if (rms < MinRms)
            return 0;

        double db = 20 * Math.Log10(rms);
        db += DefaultCalibrationOffsetDb;
        return Math.Clamp(db, 0, 120);
    }
}

/// <summary>
/// 时间加权类型（IEC 常见时间常数）
/// </summary>
public enum TimeWeighting
{
    Fast,
    Slow
}

/// <summary>
/// 频率加权类型
/// </summary>
public enum FrequencyWeighting
{
    None,
    AApproximate
}

/// <summary>
/// 状态化声级计：支持频率加权近似、时间加权与校准偏移。
/// </summary>
public sealed class SoundLevelMeter
{
    private const double MinRms = 1e-10;

    private readonly int _sampleRate;
    private double _smoothedDecibel;
    private bool _hasSmoothedValue;

    // 一阶高通滤波器状态（用于 A 加权近似，抑制低频）
    private double _lastInput;
    private double _lastOutput;

    public SoundLevelMeter(int sampleRate)
    {
        _sampleRate = Math.Max(8000, sampleRate);
    }

    /// <summary>
    /// 将 dBFS 映射到近似 dB SPL 的偏移量，建议结合实测校准。
    /// </summary>
    public double CalibrationOffsetDb { get; set; } = 94.0;

    public TimeWeighting TimeWeighting { get; set; } = TimeWeighting.Fast;

    public FrequencyWeighting FrequencyWeighting { get; set; } = FrequencyWeighting.AApproximate;

    public void Reset()
    {
        _smoothedDecibel = 0;
        _hasSmoothedValue = false;
        _lastInput = 0;
        _lastOutput = 0;
    }

    public double ProcessBuffer(byte[] audioData, int bytesPerSample, int bytesRecorded)
    {
        if (audioData == null || audioData.Length == 0 || bytesRecorded <= 0 || bytesPerSample <= 0)
            return 0;

        int effectiveBytes = Math.Min(bytesRecorded, audioData.Length);
        int sampleCount = effectiveBytes / bytesPerSample;
        if (sampleCount <= 0)
            return 0;

        double sumSquares = 0;
        int offset = 0;

        if (bytesPerSample == 2)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(audioData[offset] | (audioData[offset + 1] << 8));
                double normalized = sample / 32768d;
                normalized = ApplyFrequencyWeighting(normalized);
                sumSquares += normalized * normalized;
                offset += 2;
            }
        }
        else if (bytesPerSample == 4)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                int sampleBits = audioData[offset] | (audioData[offset + 1] << 8) |
                                 (audioData[offset + 2] << 16) | (audioData[offset + 3] << 24);
                double normalized = BitConverter.Int32BitsToSingle(sampleBits);
                normalized = ApplyFrequencyWeighting(normalized);
                sumSquares += normalized * normalized;
                offset += 4;
            }
        }
        else
        {
            return 0;
        }

        double rms = Math.Sqrt(sumSquares / sampleCount);
        if (rms < MinRms)
            return SmoothAndClamp(0, sampleCount);

        double dbFs = 20 * Math.Log10(rms);
        double dbSplEstimate = dbFs + CalibrationOffsetDb;
        return SmoothAndClamp(dbSplEstimate, sampleCount);
    }

    private double ApplyFrequencyWeighting(double sample)
    {
        if (FrequencyWeighting != FrequencyWeighting.AApproximate)
        {
            return sample;
        }

        // 约 150Hz 一阶高通，作为 A 加权的轻量近似。
        const double cutoffHz = 150.0;
        double dt = 1.0 / _sampleRate;
        double rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        double alpha = rc / (rc + dt);

        double output = alpha * (_lastOutput + sample - _lastInput);
        _lastInput = sample;
        _lastOutput = output;
        return output;
    }

    private double SmoothAndClamp(double currentDb, int sampleCount)
    {
        double frameSeconds = sampleCount / (double)_sampleRate;
        double tau = TimeWeighting == TimeWeighting.Slow ? 1.0 : 0.125;
        double alpha = 1.0 - Math.Exp(-frameSeconds / tau);

        if (!_hasSmoothedValue)
        {
            _smoothedDecibel = currentDb;
            _hasSmoothedValue = true;
        }
        else
        {
            _smoothedDecibel += alpha * (currentDb - _smoothedDecibel);
        }

        return Math.Clamp(_smoothedDecibel, 0, 120);
    }
}
