using System;
using System.Diagnostics;
using TranslationApp.Domain;
using Xunit;
using Xunit.Abstractions;

namespace TranslationApp.Tests;

public class EfficiencyBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public EfficiencyBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_TextDirectionDetector_ThroughputAndLatency()
    {
        string largeArabicText = new string('م', 5000);
        string largeEnglishText = new string('E', 5000);

        // Warmup
        for (int i = 0; i < 100; i++)
        {
            _ = TextDirectionDetector.Detect(largeArabicText);
        }

        var sw = Stopwatch.StartNew();
        int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            _ = TextDirectionDetector.Detect(largeArabicText);
            _ = TextDirectionDetector.Detect(largeEnglishText);
        }
        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgMicroseconds = (totalMs / (iterations * 2)) * 1000.0;

        _output.WriteLine($"BiDi Detection Total Time: {totalMs:F2} ms for {iterations * 2} calls");
        _output.WriteLine($"BiDi Average Latency: {avgMicroseconds:F4} µs (microseconds)");

        // Must take less than 10 microseconds per call (extremely fast)
        Assert.True(avgMicroseconds < 10.0, $"Expected < 10µs, got {avgMicroseconds}µs");
    }
}
