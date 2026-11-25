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

    public static async Task<TestClient> LoginAsync(int port, string name, int seconds = 15)
    {
        var c = await ConnectAsync(port);
        c.Name = name;
        await c.SendAsync(MessageTypes.Login, new LoginPayload { Name = name });
        await c.WaitAsync(MessageTypes.LoginOk, seconds);
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
    public string HistoryDir { get; }
    private readonly CancellationTokenSource _cts = new();

    public ServerFixture()
    {
        Logger.Quiet = true;
        var l = new TcpListener(IPAddress.Loopback, 0); l.Start();
        Port = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop();
        HistoryDir = Path.Combine(Path.GetTempPath(), "sudoku-test-" + Guid.NewGuid());
        var server = new GameServer(new HistoryStore(HistoryDir)) { Holes = 30 };
        _ = Task.Run(() => server.RunAsync(Port, _cts.Token));
    }

    public void Dispose() => _cts.Cancel();
}

public class NetworkTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _fx;
    private readonly int _port;
    public NetworkTests(ServerFixture f) { _fx = f; _port = f.Port; }

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
        await c.SendAsync(MessageTypes.SubmitMove, new MovePayload());
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
            b.Dispose();
            var paused = await a.WaitAsync(MessageTypes.GamePaused);
            Assert.Equal(MatchStates.Paused, Data<TimeUpdateData>(paused).State);

            using var b2 = await TestClient.LoginAsync(_port, "bob5");
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

    [Fact]
    public async Task SamePuzzle_And_BoardsStayIndependent()
    {
        var a = await TestClient.LoginAsync(_port, "sameA");
        var b = await TestClient.LoginAsync(_port, "sameB");
        using (a) using (b)
        {
            await a.SendAsync(MessageTypes.JoinQueue);
            await a.WaitAsync(MessageTypes.Waiting);
            await b.SendAsync(MessageTypes.JoinQueue);
            await a.WaitAsync(MessageTypes.MatchFound);
            await b.WaitAsync(MessageTypes.MatchFound);
            var sa = Data<MatchSnapshot>(await a.WaitAsync(MessageTypes.GameStarted));
            var sb = Data<MatchSnapshot>(await b.WaitAsync(MessageTypes.GameStarted));
            Assert.Equal(sa.Puzzle.SelectMany(r => r), sb.Puzzle.SelectMany(r => r));

            var sol = SudokuGenerator.Solve(sa.Puzzle)!;
            int i = Enumerable.Range(0, 81).First(k => sa.Puzzle[k / 9][k % 9] == 0);
            await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = i / 9, Col = i % 9, Value = sol[i / 9][i % 9] });
            var mr = Data<MoveResultData>(await a.WaitAsync(MessageTypes.MoveResult));
            Assert.True(mr.Accepted);
            Assert.Equal(1, mr.ScoreDelta);

            var upd = Data<MatchSnapshot>(await b.WaitAsync(MessageTypes.BoardUpdate));
            Assert.Equal(0, upd.Players.First(p => p.Name == "sameB").Score);
            Assert.Null(upd.Players.First(p => p.Name == "sameA").Board);
        }
    }

    [Fact]
    public async Task BadMove_OutOfRange_GivenCell_And_WrongValue()
    {
        var (a, b, snap) = await StartMatchAsync("badA", "badB");
        using (a) using (b)
        {
            int empty = Enumerable.Range(0, 81).First(k => snap.Puzzle[k / 9][k % 9] == 0);
            await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = empty / 9, Col = empty % 9, Value = 10 });
            var bad = Data<MoveResultData>(await a.WaitAsync(MessageTypes.MoveResult));
            Assert.False(bad.Accepted);
            Assert.Equal(0, bad.Score);
            Assert.Contains("không hợp lệ", bad.Reason);

            int given = Enumerable.Range(0, 81).First(k => snap.Puzzle[k / 9][k % 9] != 0);
            await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = given / 9, Col = given % 9, Value = 1 });
            var locked = Data<MoveResultData>(await a.WaitAsync(MessageTypes.MoveResult));
            Assert.False(locked.Accepted);
            Assert.Equal(0, locked.ScoreDelta);
            Assert.Contains("không được sửa", locked.Reason);

            var sol = SudokuGenerator.Solve(snap.Puzzle)!;
            int wrong = sol[empty / 9][empty % 9] % 9 + 1;
            await a.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = empty / 9, Col = empty % 9, Value = wrong });
            var miss = Data<MoveResultData>(await a.WaitAsync(MessageTypes.MoveResult));
            Assert.False(miss.Accepted);
            Assert.Equal(-1, miss.ScoreDelta);
            Assert.Equal(-1, miss.Score);

            var board = Data<MatchSnapshot>(await a.WaitAsync(MessageTypes.BoardUpdate));
            Assert.Equal(0, board.Players.First(p => p.Name == "badA").Board![empty / 9][empty % 9]);
        }
    }

    [Fact]
    public async Task MissingType_IsRejected_MissingPayload_IsNot()
    {
        using var c = await TestClient.LoginAsync(_port, "raw8");
        await c.SendRawAsync("{\"Payload\":\"\"}");
        var err = await c.WaitAsync(MessageTypes.Error);
        Assert.Contains("Type", err.Message);
        await c.SendRawAsync("{\"Type\":\"PING\"}");
        await c.WaitAsync(MessageTypes.Pong);
    }

    [Fact]
    public async Task JoinQueue_Twice_And_DisconnectWhileWaiting()
    {
        using var dup = await TestClient.LoginAsync(_port, "dupQ");
        await dup.SendAsync(MessageTypes.JoinQueue);
        await dup.WaitAsync(MessageTypes.Waiting);
        await dup.SendAsync(MessageTypes.JoinQueue);
        Assert.Contains("rảnh", (await dup.WaitAsync(MessageTypes.Error)).Message);
        await dup.SendAsync(MessageTypes.LeaveQueue);
        await dup.WaitAsync(MessageTypes.Ok);

        var gone = await TestClient.LoginAsync(_port, "goneQ");
        await gone.SendAsync(MessageTypes.JoinQueue);
        await gone.WaitAsync(MessageTypes.Waiting);
        gone.Dispose();
        await Task.Delay(400);

        using var next = await TestClient.LoginAsync(_port, "nextQ");
        await next.SendAsync(MessageTypes.JoinQueue);
        var stillWaiting = await next.WaitAsync(MessageTypes.Waiting);
        Assert.Equal(MessageTypes.Waiting, stillWaiting.Status);
    }

    [Fact]
    public async Task TwoMatches_DoNotShareScore()
    {
        async Task<(TestClient A, TestClient B, MatchSnapshot S)> Pair(string x, string y)
        {
            var a = await TestClient.LoginAsync(_port, x);
            var b = await TestClient.LoginAsync(_port, y);
            await a.SendAsync(MessageTypes.Challenge, new ChallengePayload { Target = y });
            await b.WaitAsync(MessageTypes.ChallengeReceived);
            await b.SendAsync(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = x, Accept = true });
            var s = Data<MatchSnapshot>(await a.WaitAsync(MessageTypes.GameStarted, 30));
            await b.WaitAsync(MessageTypes.GameStarted, 30);
            return (a, b, s);
        }

        var m1 = await Pair("m1a", "m1b");
        var m2 = await Pair("m2a", "m2b");
        using var watcher = await TestClient.LoginAsync(_port, "watch15");
        using (m1.A) using (m1.B) using (m2.A) using (m2.B)
        {
            Assert.NotEqual(m1.S.MatchId, m2.S.MatchId);
            var sol = SudokuGenerator.Solve(m1.S.Puzzle)!;
            int i = Enumerable.Range(0, 81).First(k => m1.S.Puzzle[k / 9][k % 9] == 0);
            await m1.A.SendAsync(MessageTypes.SubmitMove, new MovePayload { Row = i / 9, Col = i % 9, Value = sol[i / 9][i % 9] });
            Assert.Equal(1, Data<MoveResultData>(await m1.A.WaitAsync(MessageTypes.MoveResult)).Score);

            await watcher.SendAsync(MessageTypes.ListMatches);
            var list = Data<List<MatchSummary>>(await watcher.WaitAsync(MessageTypes.MatchList));
            var moved = list.First(x => x.MatchId == m1.S.MatchId);
            var other = list.First(x => x.MatchId == m2.S.MatchId);
            Assert.Equal(1, moved.Score1);
            Assert.Equal(0, moved.Score2);
            Assert.Equal(0, other.Score1);
            Assert.Equal(0, other.Score2);
        }
    }

    [Fact]
    public async Task HistoryFile_HasFinishedMatch()
    {
        var (a, b, _) = await StartMatchAsync("histA", "histB");
        using (a) using (b)
        {
            await a.SendAsync(MessageTypes.Surrender);
            var over = Data<GameOverData>(await b.WaitAsync(MessageTypes.GameOver));
            string path = Path.Combine(_fx.HistoryDir, "match_history.jsonl");
            var line = File.ReadAllLines(path)
                .Select(x => JsonHelper.Deserialize<MatchRecord>(x))
                .FirstOrDefault(r => r != null && r.Player1 == "histA" && r.Player2 == "histB");
            Assert.NotNull(line);
            Assert.Equal(over.MatchId, line.MatchId);
            Assert.Equal("histB", line.Winner);
        }
    }

    [Fact]
    public async Task AfterGameOver_Disconnect_OtherClientStillServed()
    {
        var (a, b, _) = await StartMatchAsync("endA", "endB");
        using (b)
        {
            await a.SendAsync(MessageTypes.Surrender);
            await a.WaitAsync(MessageTypes.GameOver);
            await b.WaitAsync(MessageTypes.GameOver);
            a.Dispose();
            await Task.Delay(200);
            await b.SendAsync(MessageTypes.Ping);
            await b.WaitAsync(MessageTypes.Pong);
        }
    }

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