namespace SoundMonitor.Services;

/// <summary>
/// 分贝计算器
/// </summary>
public static class DecibelCalculator
{
    // 将 dBFS 粗略映射到 UI 的 0-120 区间，便于实时观察曲线变化。
    private const double DisplayOffset = 90.0;

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
        if (rms < 1e-10)
            return 0;

        // samples 已经在 [-1, 1] 区间，直接计算 dBFS。
        double db = 20 * Math.Log10(rms);

        // 将负值范围映射到 0-100 dB
        // 典型范围：-60 dBFS 到 0 dBFS
        // 映射到 0-100 dB SPL 近似值
        db = Math.Max(0, db + DisplayOffset); // 偏移使静音接近 0 dB

        return Math.Min(120, db); // 限制最大值
    }

    /// <summary>
    /// 计算分贝值（从字节数组）
    /// </summary>
    public static double CalculateDecibel(byte[] audioData, int bytesPerSample)
    {
        if (audioData == null || audioData.Length == 0)
            return 0;

        var samples = new float[audioData.Length / bytesPerSample];

        for (int i = 0; i < samples.Length; i++)
        {
            if (bytesPerSample == 2)
            {
                // 16位采样
                short sample = (short)(audioData[i * 2] | (audioData[i * 2 + 1] << 8));
                samples[i] = sample / 32768f;
            }
            else if (bytesPerSample == 4)
            {
                // 32位采样
                int sample = audioData[i * 4] | (audioData[i * 4 + 1] << 8) |
                             (audioData[i * 4 + 2] << 16) | (audioData[i * 4 + 3] << 24);
                samples[i] = BitConverter.Int32BitsToSingle(sample);
            }
        }

        return CalculateDecibel(samples);
    }
}
