using SoundMonitor.Services;

namespace SoundMonitor.Tests;

public class DecibelCalculatorTests
{
    [Fact]
    public void CalculateDecibel_WithSilence_ReturnsZero()
    {
        var silence = new byte[2048];

        double db = DecibelCalculator.CalculateDecibel(silence, 2, silence.Length);

        Assert.Equal(0, db);
    }

    [Fact]
    public void SoundLevelMeter_WithConstantTone_ReturnsPositiveValue()
    {
        var meter = new SoundLevelMeter(44100)
        {
            CalibrationOffsetDb = 94,
            TimeWeighting = TimeWeighting.Fast,
            FrequencyWeighting = FrequencyWeighting.None
        };

        var buffer = CreatePcm16SineBuffer(1000, 0.2, 44100, 100);
        double db = meter.ProcessBuffer(buffer, 2, buffer.Length);

        Assert.InRange(db, 40, 100);
    }

    private static byte[] CreatePcm16SineBuffer(int frequencyHz, double amplitude, int sampleRate, int durationMs)
    {
        int sampleCount = sampleRate * durationMs / 1000;
        var buffer = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleRate;
            short sample = (short)(Math.Sin(2 * Math.PI * frequencyHz * t) * amplitude * short.MaxValue);
            buffer[i * 2] = (byte)(sample & 0xFF);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }
}