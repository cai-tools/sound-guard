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
        if (rms < 1e-10)
            return 0;

        double db = 20 * Math.Log10(rms);
        db = Math.Max(0, db + DisplayOffset);
        return Math.Min(120, db);
    }
}
