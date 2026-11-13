using SudokuLogic;
using SudokuServer;
using SudokuShared.Messages;

namespace SudokuTests;

public class MatchTests
{
    private static GameMatch NewMatch(int holes = 5)
    {
        var puzzle = SudokuGenerator.Generate(holes, new Random(7));
        return new GameMatch("T1", puzzle, ("A", null), ("B", null));
    }

    private static void SolveAll(GameMatch m, MatchPlayer p, Outbox o)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (m.Puzzle.Puzzle[r][c] == 0)
                    m.Submit(p, new MovePayload { Row = r, Col = c, Value = m.Puzzle.Solution[r][c] }, o);
    }

    private static void Tick(GameMatch m, int n) { for (int i = 0; i < n; i++) m.Tick(new Outbox()); }

    [Fact]
    public void FirstToComplete_Wins()
    {
        var m = NewMatch(); var o = new Outbox(); bool fired = false;
        m.OnFinished = _ => fired = true;
        SolveAll(m, m.Players[0], o);
        Assert.True(m.IsFinished); Assert.True(fired);
        Assert.Equal("A", m.Winner);
    }

    [Fact]
    public void WrongMove_CostsOnePoint_AndIsNotRecorded()
    {
        var m = NewMatch(); var o = new Outbox();
        var (r, c) = LogicTests.FirstEmpty(m.Puzzle);
        int wrong = m.Puzzle.Solution[r][c] % 9 + 1;
        m.Submit(m.Players[0], new MovePayload { Row = r, Col = c, Value = wrong }, o);
        Assert.Equal(-1, m.Players[0].Board.Score);
        Assert.Equal(0, m.Players[0].Board.Cells[r][c]);
        Assert.Equal(0, m.Players[1].Board.Score);      // bảng đối thủ không bị ảnh hưởng
    }

    [Fact]
    public void Timeout_HigherScoreWins_EqualIsDraw()
    {
        var m = NewMatch(10); var o = new Outbox();
        var (r, c) = LogicTests.FirstEmpty(m.Puzzle);
        m.Submit(m.Players[1], new MovePayload { Row = r, Col = c, Value = m.Puzzle.Solution[r][c] }, o);
        Tick(m, ProtocolConstants.MatchSeconds);
        Assert.True(m.IsFinished);
        Assert.Equal("B", m.Winner);

        var d = NewMatch(10);
        Tick(d, ProtocolConstants.MatchSeconds);
        Assert.True(d.IsFinished);
        Assert.Null(d.Winner);                            // hòa
    }

    [Fact]
    public void Surrender_OpponentWins()
    {
        var m = NewMatch();
        m.Surrender(m.Players[0], new Outbox());
        Assert.Equal("B", m.Winner);
    }

    [Fact]
    public void Disconnect_PausesThenForfeitsAfterOneMinute()
    {
        var m = NewMatch(10); var o = new Outbox();
        m.PlayerDisconnected(m.Players[1], o);
        Assert.True(m.IsPaused);

        var (r, c) = LogicTests.FirstEmpty(m.Puzzle);
        m.Submit(m.Players[0], new MovePayload { Row = r, Col = c, Value = m.Puzzle.Solution[r][c] }, o);
        Assert.Equal(0, m.Players[0].Board.Score);        // đang tạm dừng: không nhận nước đi

        int before = m.TimeLeft;
        Tick(m, ProtocolConstants.ReconnectSeconds - 1);
        Assert.Equal(before, m.TimeLeft);                 // đồng hồ trận đứng yên khi tạm dừng
        Assert.False(m.IsFinished);
        Tick(m, 1);
        Assert.True(m.IsFinished);
        Assert.Equal("A", m.Winner);
    }

    [Fact]
    public void Reconnect_WithinOneMinute_ResumesMatch()
    {
        var m = NewMatch(10);
        m.PlayerDisconnected(m.Players[1], new Outbox());
        Tick(m, 30);
        m.PlayerReconnected(m.Players[1], null, new Outbox());
        Assert.False(m.IsPaused);
        Tick(m, 100);
        Assert.False(m.IsFinished);
        Assert.Equal(ProtocolConstants.MatchSeconds - 100, m.TimeLeft);
    }
}