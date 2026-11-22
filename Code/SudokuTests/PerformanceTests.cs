using System.Diagnostics;
using SudokuLogic;
using SudokuShared;
using SudokuShared.Messages;
using Xunit.Abstractions;

namespace SudokuTests;

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
    [InlineData(20, 10)]  // Mức tải 1 (Performance): 10 trận (20 clients), 5 nước đi/trận
    [InlineData(60, 20)] // Mức tải 2 (Stress): 30 trận (60 clients), 10 nước đi/trận
    public async Task Performance_And_Stress_TwoLoadLevels(int pairs, int movesPerMatch)
    {
        long totalMsgs = 0, totalErrors = 0, totalLatencyMs = 0;
        long memBefore = GC.GetTotalMemory(true) / (1024 * 1024);
        var totalSw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, pairs).Select(async i =>
        {
            try
            {
                var a = await TestClient.LoginAsync(_port, $"pA_{pairs}_{i}");
                var b = await TestClient.LoginAsync(_port, $"pB_{pairs}_{i}");
                using (a) using (b)
                {
                    await a.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = $"pB_{pairs}_{i}" });
                    await b.WaitAsync(MessageTypes.ChallengeReceived);
                    await b.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = $"pA_{pairs}_{i}", Accept = true });

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
            }
            catch
            {
                Interlocked.Increment(ref totalErrors);
            }
        });

        await Task.WhenAll(tasks);
        totalSw.Stop();
        long memAfter = GC.GetTotalMemory(false) / (1024 * 1024);

        double avgLatency = totalMsgs > 0 ? (double)totalLatencyMs / totalMsgs : 0;
        double throughput = totalMsgs / totalSw.Elapsed.TotalSeconds;
        double errorRate = (double)totalErrors / pairs * 100;

        _output.WriteLine($"=== MUC TAI: {pairs} TRAN ({pairs * 2} CLIENTS), {movesPerMatch} NUOC/TRAN ===");
        _output.WriteLine($"Tong thoi gian : {totalSw.Elapsed.TotalSeconds:F2} s");
        _output.WriteLine($"Latency TB     : {avgLatency:F2} ms");
        _output.WriteLine($"Throughput     : {throughput:F2} msg/s");
        _output.WriteLine($"Ty le loi      : {errorRate:F2} %");
        _output.WriteLine($"RAM (truoc/sau): {memBefore} MB -> {memAfter} MB");

        Assert.Equal(0, totalErrors);
    }
}