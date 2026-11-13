using SudokuLogic;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuTests;

public class LogicTests
{
    [Fact]
    public void Generator_ProducesValidUniquePuzzle()
    {
        var p = SudokuGenerator.Generate(40, new Random(42));
        Assert.True(SudokuGenerator.IsCompleteAndValid(p.Solution));
        Assert.InRange(p.EmptyCount, 30, 40);
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (p.Puzzle[r][c] != 0) Assert.Equal(p.Solution[r][c], p.Puzzle[r][c]);

        var solved = SudokuGenerator.Solve(p.Puzzle);
        Assert.NotNull(solved);
        Assert.Equal(p.Solution.SelectMany(x => x), solved!.SelectMany(x => x));
    }

    [Fact]
    public void PlayerBoard_ScoresCorrectAndWrongMoves()
    {
        var p = SudokuGenerator.Generate(10, new Random(1));
        var b = new PlayerBoard(p);
        var (r, c) = FirstEmpty(p);
        int right = p.Solution[r][c];
        int wrong = right % 9 + 1;

        var w = b.TryPlace(r, c, wrong);
        Assert.Equal(MoveOutcome.Wrong, w.Outcome);
        Assert.Equal(-1, b.Score);
        Assert.Equal(0, b.Cells[r][c]);               // nước sai không ghi vào bảng

        var ok = b.TryPlace(r, c, right);
        Assert.Equal(MoveOutcome.Correct, ok.Outcome);
        Assert.Equal(0, b.Score);                      // -1 + 1
        Assert.Equal(right, b.Cells[r][c]);

        Assert.Equal(MoveOutcome.Locked, b.TryPlace(r, c, wrong).Outcome);   // ô đã đúng: khóa
        Assert.Equal(0, b.Score);
    }

    [Fact]
    public void PlayerBoard_CannotEditGivenCellsOrInvalidInput()
    {
        var p = SudokuGenerator.Generate(10, new Random(2));
        var b = new PlayerBoard(p);
        int gr = 0, gc = 0;
        for (int i = 0; i < 81; i++) if (p.Puzzle[i / 9][i % 9] != 0) { gr = i / 9; gc = i % 9; break; }
        Assert.Equal(MoveOutcome.Locked, b.TryPlace(gr, gc, 5).Outcome);
        Assert.Equal(MoveOutcome.InvalidInput, b.TryPlace(9, 0, 1).Outcome);
        Assert.Equal(MoveOutcome.InvalidInput, b.TryPlace(0, 0, 10).Outcome);
        Assert.Equal(0, b.Score);
    }

    [Fact]
    public void PlayerBoard_CompletesWhenAllCellsCorrect()
    {
        var p = SudokuGenerator.Generate(8, new Random(3));
        var b = new PlayerBoard(p);
        FillAll(b, p);
        Assert.True(b.IsComplete);
        Assert.Equal(8, b.Score);
    }

    [Fact]
    public void Json_RoundTrip()
    {
        var req = JsonHelper.CreateRequest(MessageTypes.SubmitMove, new MovePayload { Row = 1, Col = 2, Value = 3 });
        var line = JsonHelper.Serialize(req);
        var back = JsonHelper.Deserialize<RequestMessage>(line)!;
        var mv = JsonHelper.ReadPayload<MovePayload>(back)!;
        Assert.Equal((1, 2, 3), (mv.Row, mv.Col, mv.Value));

        var resp = JsonHelper.CreateResponse(MessageTypes.MoveResult, "ok", new MoveResultData { Score = 5 });
        var data = JsonHelper.ReadResponseData<MoveResultData>(JsonHelper.Deserialize<ResponseMessage>(JsonHelper.Serialize(resp))!);
        Assert.Equal(5, data!.Score);
        Assert.DoesNotContain('\n', line);
    }

    internal static (int R, int C) FirstEmpty(SudokuPuzzle p)
    {
        for (int i = 0; i < 81; i++) if (p.Puzzle[i / 9][i % 9] == 0) return (i / 9, i % 9);
        throw new InvalidOperationException();
    }

    internal static void FillAll(PlayerBoard b, SudokuPuzzle p)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (p.Puzzle[r][c] == 0) b.TryPlace(r, c, p.Solution[r][c]);
    }
}
