using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SudokuLogic;
using SudokuShared;
using SudokuShared.Messages;
using Xunit.Abstractions;

namespace SudokuTests;

/// <summary>
/// TC_18. Chạy riêng để số đo không bị test khác chiếm CPU:
/// dotnet test --filter Performance_And_Stress --logger "console;verbosity=detailed"
/// </summary>
public class PerformanceTests : IClassFixture<ServerFixture>
{
    private readonly int _port;
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ServerFixture f, ITestOutputHelper output)
    {
        _port = f.Port;
        _output = output;
    }

    private static T Data<T>(ResponseMessage m) => JsonHelper.ReadResponseData<T>(m)!;

    [Theory]
    [InlineData(20, 10)] // Mức 1: 20 trận, 40 client, 10 nước/trận
    [InlineData(60, 20)] // Mức 2: 60 trận, 120 client, 20 nước/trận
    public async Task Performance_And_Stress_TwoLoadLevels(int pairs, int movesPerMatch)
    {
        long totalMsgs = 0, totalErrors = 0, totalLatencyMs = 0;
        var errors = new ConcurrentBag<string>();
        long memBefore = GC.GetTotalMemory(true) / (1024 * 1024);

        var proc = Process.GetCurrentProcess();
        var cpuBefore = proc.TotalProcessorTime;
        var totalSw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, pairs).Select(async i =>
        {
            TestClient? a = null;
            TestClient? b = null;
            try
            {
                a = await TestClient.LoginAsync(_port, $"pA{pairs}_{i}", 30);
                b = await TestClient.LoginAsync(_port, $"pB{pairs}_{i}", 30);

                await a.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = $"pB{pairs}_{i}" });
                await b.WaitAsync(MessageTypes.ChallengeReceived, 30);
                await b.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = $"pA{pairs}_{i}", Accept = true });

                var snap = Data<MatchSnapshot>(await a.WaitAsync(MessageTypes.GameStarted, 30));
                await b.WaitAsync(MessageTypes.GameStarted, 30);

                var sol = SudokuGenerator.Solve(snap.Puzzle)!;
                int sent = 0;
                for (int r = 0; r < 9 && sent < movesPerMatch; r++)
                {
                    for (int c = 0; c < 9 && sent < movesPerMatch; c++)
                    {
                        if (snap.Puzzle[r][c] != 0) continue;
                        var moveSw = Stopwatch.StartNew();
                        await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = r, Col = c, Value = sol[r][c] });
                        await a.WaitAsync(MessageTypes.MoveResult, 30);
                        moveSw.Stop();

                        Interlocked.Increment(ref totalMsgs);
                        Interlocked.Add(ref totalLatencyMs, moveSw.ElapsedMilliseconds);
                        sent++;
                    }
                }

                await a.SendAsync(MessageTypes.Surrender);
                await a.WaitAsync(MessageTypes.GameOver, 30);
                await b.WaitAsync(MessageTypes.GameOver, 30);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref totalErrors);
                errors.Add($"pair {i}: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                a?.Dispose();
                b?.Dispose();
            }
        });

        await Task.WhenAll(tasks);
        totalSw.Stop();

        proc.Refresh();
        double cpuSeconds = (proc.TotalProcessorTime - cpuBefore).TotalSeconds;
        double cpuPercent = totalSw.Elapsed.TotalSeconds > 0
            ? cpuSeconds / (totalSw.Elapsed.TotalSeconds * Environment.ProcessorCount) * 100
            : 0;

        long memAfter = GC.GetTotalMemory(false) / (1024 * 1024);

        double avgLatency = totalMsgs > 0 ? (double)totalLatencyMs / totalMsgs : 0;
        double throughput = totalSw.Elapsed.TotalSeconds > 0 ? totalMsgs / totalSw.Elapsed.TotalSeconds : 0;
        double errorRate = (double)totalErrors / pairs * 100;

        _output.WriteLine($"May            : {Environment.MachineName}, CPU logic {Environment.ProcessorCount}, {RuntimeInformation.OSDescription}");
        _output.WriteLine($"=== MUC TAI: {pairs} TRAN ({pairs * 2} CLIENTS), {movesPerMatch} NUOC/TRAN ===");
        _output.WriteLine($"Tong thoi gian : {totalSw.Elapsed.TotalSeconds:F2} s");
        _output.WriteLine($"Latency TB     : {avgLatency:F2} ms (SUBMIT_MOVE -> MOVE_RESULT, localhost)");
        _output.WriteLine($"Throughput     : {throughput:F2} msg/s (chi tinh nuoc di, mau so la ca phien)");
        _output.WriteLine($"Ty le loi      : {errorRate:F2} % ({totalErrors}/{pairs} cap)");
        _output.WriteLine($"CPU TB         : {cpuPercent:F1} % tren {Environment.ProcessorCount} core ({cpuSeconds:F2} s CPU / {totalSw.Elapsed.TotalSeconds:F2} s, ca tien trinh test, khong phai server rieng)");
        _output.WriteLine($"RAM (truoc/sau): {memBefore} MB -> {memAfter} MB (GC heap ca tien trinh test, khong phai RAM rieng server)");
        foreach (var e in errors.Take(8))
            _output.WriteLine("LOI: " + e);

        Assert.Equal(0, totalErrors);
    }
}