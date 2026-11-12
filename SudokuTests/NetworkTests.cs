using System.Net;
using System.Net.Sockets;
using System.Text;
using SudokuLogic;
using SudokuServer;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuTests;

internal sealed class TestClient : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly StreamReader _r;
    private readonly StreamWriter _w;
    public string Name = "";

    private TestClient(TcpClient t)
    {
        _tcp = t;
        var s = t.GetStream();
        _r = new StreamReader(s, new UTF8Encoding(false));
        _w = new StreamWriter(s, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public static async Task<TestClient> ConnectAsync(int port)
    {
        for (int i = 0; ; i++)
        {
            try { var t = new TcpClient(); await t.ConnectAsync(IPAddress.Loopback, port); return new TestClient(t); }
            catch when (i < 40) { await Task.Delay(50); }
        }
    }

    public static async Task<TestClient> LoginAsync(int port, string name)
    {
        var c = await ConnectAsync(port);
        c.Name = name;
        await c.SendAsync(MessageTypes.Login, new LoginPayload { Name = name });
        await c.WaitAsync(MessageTypes.LoginOk);
        return c;
    }

    public Task SendAsync(string type, object? payload = null)
        => _w.WriteLineAsync(JsonHelper.Serialize(new RequestMessage { Type = type, Payload = payload == null ? "" : JsonHelper.Serialize(payload) }));

    public Task SendRawAsync(string line) => _w.WriteLineAsync(line);

    /// <summary>Đọc cho tới khi gặp message có Status mong muốn (bỏ qua TIME_UPDATE...).</summary>
    public async Task<ResponseMessage> WaitAsync(string status, int seconds = 15)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        while (true)
        {
            var line = await _r.ReadLineAsync(cts.Token) ?? throw new IOException("closed");
            var m = JsonHelper.Deserialize<ResponseMessage>(line)!;
            if (m.Status == status) return m;
        }
    }

    public async Task SolveAsync(MatchSnapshot snap)
    {
        var sol = SudokuGenerator.Solve(snap.Puzzle)!;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (snap.Puzzle[r][c] == 0)
                    await SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = r, Col = c, Value = sol[r][c] });
    }

    public void Dispose() { try { _tcp.Close(); } catch { } }
}

public sealed class ServerFixture : IDisposable
{
    public int Port { get; }
    private readonly CancellationTokenSource _cts = new();

    public ServerFixture()
    {
        Logger.Quiet = true;
        var l = new TcpListener(IPAddress.Loopback, 0); l.Start();
        Port = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop();
        var dir = Path.Combine(Path.GetTempPath(), "sudoku-test-" + Guid.NewGuid());
        var server = new GameServer(new HistoryStore(dir)) { Holes = 30 };
        _ = Task.Run(() => server.RunAsync(Port, _cts.Token));
    }

    public void Dispose() => _cts.Cancel();
}

public class NetworkTests : IClassFixture<ServerFixture>
{
    private readonly int _port;
    public NetworkTests(ServerFixture f) => _port = f.Port;

    private static T Data<T>(ResponseMessage m) => JsonHelper.ReadResponseData<T>(m)!;

    private async Task<(TestClient A, TestClient B, MatchSnapshot SnapA)> StartMatchAsync(string a, string b)
    {
        var ca = await TestClient.LoginAsync(_port, a);
        var cb = await TestClient.LoginAsync(_port, b);
        await ca.SendAsync(MessageTypes.JoinQueue);
        await ca.WaitAsync(MessageTypes.Waiting);
        await cb.SendAsync(MessageTypes.JoinQueue);
        await ca.WaitAsync(MessageTypes.MatchFound);
        await cb.WaitAsync(MessageTypes.MatchFound);
        var snap = Data<MatchSnapshot>(await ca.WaitAsync(MessageTypes.GameStarted));
        await cb.WaitAsync(MessageTypes.GameStarted);
        return (ca, cb, snap);
    }

    [Fact]
    public async Task Matchmaking_And_FullGame_FirstToCompleteWins()
    {
        var (a, b, snap) = await StartMatchAsync("alice1", "bob1");
        using (a) using (b)
        {
            // Người chơi chỉ nhìn thấy bảng của mình
            Assert.NotNull(snap.Players.First(p => p.Name == "alice1").Board);
            Assert.Null(snap.Players.First(p => p.Name == "bob1").Board);

            await a.SolveAsync(snap);
            var overA = Data<GameOverData>(await a.WaitAsync(MessageTypes.GameOver));
            var overB = Data<GameOverData>(await b.WaitAsync(MessageTypes.GameOver));
            Assert.Equal("WIN", overA.Result);
            Assert.Equal("LOSE", overB.Result);
            Assert.Equal("alice1", overA.Winner);
        }
    }

    [Fact]
    public async Task Spectator_SeesBothBoards_ButCannotMove()
    {
        var (a, b, snapA) = await StartMatchAsync("alice2", "bob2");
        using (a) using (b)
        using (var s = await TestClient.LoginAsync(_port, "viewer2"))
        {
            await s.SendAsync(MessageTypes.ListMatches);
            var list = Data<List<MatchSummary>>(await s.WaitAsync(MessageTypes.MatchList));
            var mine = list.First(x => x.Player1 == "alice2");

            await s.SendAsync(MessageTypes.SpectateMatch, new SpectatePayload { MatchId = mine.MatchId });
            var snap = Data<MatchSnapshot>(await s.WaitAsync(MessageTypes.SpectatorUpdate));
            Assert.All(snap.Players, p => Assert.NotNull(p.Board));

            await s.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = 0, Col = 0, Value = 1 });
            var err = await s.WaitAsync(MessageTypes.Error);
            Assert.Contains("Khán giả", err.Message);

            // người chơi đi một nước -> khán giả nhận cập nhật
            var sol = SudokuGenerator.Solve(snapA.Puzzle)!;
            int idx = Enumerable.Range(0, 81).First(i => snapA.Puzzle[i / 9][i % 9] == 0);
            await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = idx / 9, Col = idx % 9, Value = sol[idx / 9][idx % 9] });
            var upd = Data<MatchSnapshot>(await s.WaitAsync(MessageTypes.SpectatorUpdate));
            Assert.Equal(1, upd.Players.First(p => p.Name == "alice2").Score);
        }
    }

    [Fact]
    public async Task InvalidJson_And_UnknownType_ReturnError_ConnectionSurvives()
    {
        using var c = await TestClient.LoginAsync(_port, "robot3");
        await c.SendRawAsync("{ this is not json");
        await c.WaitAsync(MessageTypes.Error);
        await c.SendAsync("NOT_A_REAL_TYPE");
        await c.WaitAsync(MessageTypes.Error);
        await c.SendAsync(MessageTypes.SubmitMove, new MovePayload());     // chưa vào trận
        await c.WaitAsync(MessageTypes.Error);
        await c.SendAsync(MessageTypes.Ping);
        await c.WaitAsync(MessageTypes.Pong);
    }

    [Fact]
    public async Task DuplicateName_IsRejected_And_RequestBeforeLogin_IsRejected()
    {
        using var first = await TestClient.LoginAsync(_port, "dup4");
        using var second = await TestClient.ConnectAsync(_port);
        await second.SendAsync(MessageTypes.JoinQueue);
        await second.WaitAsync(MessageTypes.Error);
        await second.SendAsync(MessageTypes.Login, new LoginPayload { Name = "dup4" });
        await second.WaitAsync(MessageTypes.Error);
    }

    [Fact]
    public async Task Disconnect_Pauses_And_Reconnect_Resumes()
    {
        var (a, b, _) = await StartMatchAsync("alice5", "bob5");
        using (a)
        {
            b.Dispose();                                             // bob mất kết nối đột ngột
            var paused = await a.WaitAsync(MessageTypes.GamePaused);
            Assert.Equal(MatchStates.Paused, Data<TimeUpdateData>(paused).State);

            using var b2 = await TestClient.LoginAsync(_port, "bob5");   // quay lại trong 1 phút
            var back = Data<MatchSnapshot>(await b2.WaitAsync(MessageTypes.BoardUpdate));
            Assert.Equal("bob5", back.You);
            await a.WaitAsync(MessageTypes.GameResumed);
        }
    }

    [Fact]
    public async Task Challenge_Decline_And_Accept()
    {
        using var x = await TestClient.LoginAsync(_port, "chalX6");
        using var y = await TestClient.LoginAsync(_port, "chalY6");

        await x.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = "chalY6" });
        var got = Data<ChallengeData>(await y.WaitAsync(MessageTypes.ChallengeReceived));
        Assert.Equal("chalX6", got.From);
        await y.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = "chalX6", Accept = false });
        await x.WaitAsync(MessageTypes.ChallengeDeclined);

        await x.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = "chalY6" });
        await y.WaitAsync(MessageTypes.ChallengeReceived);
        await y.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = "chalX6", Accept = true });
        await x.WaitAsync(MessageTypes.GameStarted);
        await y.WaitAsync(MessageTypes.GameStarted);
    }

    [Fact]
    public async Task Surrender_GivesOpponentTheWin()
    {
        var (a, b, _) = await StartMatchAsync("alice7", "bob7");
        using (a) using (b)
        {
            await a.SendAsync(MessageTypes.Surrender);
            Assert.Equal("LOSE", Data<GameOverData>(await a.WaitAsync(MessageTypes.GameOver)).Result);
            Assert.Equal("WIN", Data<GameOverData>(await b.WaitAsync(MessageTypes.GameOver)).Result);
        }
    }

    /// <summary>Stress test: 20 trận chạy đồng thời (40 client).</summary>
    [Fact]
    public async Task Stress_TwentyConcurrentMatches()
    {
        const int pairs = 20;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, pairs).Select(async i =>
        {
            var a = await TestClient.LoginAsync(_port, $"sA{i}");
            var b = await TestClient.LoginAsync(_port, $"sB{i}");
            await a.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = $"sB{i}" });
            await b.WaitAsync(MessageTypes.ChallengeReceived);
            await b.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = $"sA{i}", Accept = true });
            await a.WaitAsync(MessageTypes.GameStarted, 30);
            await b.WaitAsync(MessageTypes.GameStarted, 30);
            using (a) using (b)
            {
                await a.SendAsync(MessageTypes.Surrender);
                await a.WaitAsync(MessageTypes.GameOver, 30);
                await b.WaitAsync(MessageTypes.GameOver, 30);
            }
        }).ToList();
        await Task.WhenAll(tasks);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60), $"Mất {sw.Elapsed.TotalSeconds:F1}s");
    }
}
