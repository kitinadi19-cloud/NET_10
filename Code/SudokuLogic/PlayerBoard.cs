namespace SudokuLogic;

public enum MoveOutcome
{
    Correct,      // đúng: ghi vào bảng, +1 điểm
    Wrong,        // sai: KHÔNG ghi vào bảng, -1 điểm
    Locked,       // ô đề bài hoặc ô đã điền đúng: không được sửa, không tính điểm
    InvalidInput  // dữ liệu ngoài phạm vi: bỏ qua, không tính điểm
}

public readonly record struct MoveResult(MoveOutcome Outcome, int ScoreDelta);

/// <summary>Bảng riêng của một người chơi. Server dùng lớp này để kiểm tra nước đi và tính điểm.</summary>
public sealed class PlayerBoard
{
    private readonly SudokuPuzzle _puzzle;

    public int[][] Cells { get; }
    public int Score { get; private set; }
    public int CorrectCount { get; private set; }
    public int WrongCount { get; private set; }
    public int Total => _puzzle.EmptyCount;
    public bool IsComplete => CorrectCount == _puzzle.EmptyCount;

    public PlayerBoard(SudokuPuzzle puzzle)
    {
        _puzzle = puzzle;
        Cells = SudokuGenerator.Clone(puzzle.Puzzle);
    }

    public MoveResult TryPlace(int row, int col, int value)
    {
        if (row < 0 || row > 8 || col < 0 || col > 8 || value < 1 || value > 9)
            return new(MoveOutcome.InvalidInput, 0);

        if (Cells[row][col] != 0)                       // ô đề bài hoặc đã điền đúng
            return new(MoveOutcome.Locked, 0);

        if (_puzzle.Solution[row][col] == value)
        {
            Cells[row][col] = value;
            CorrectCount++; Score++;
            return new(MoveOutcome.Correct, +1);
        }

        WrongCount++; Score--;
        return new(MoveOutcome.Wrong, -1);
    }
}
