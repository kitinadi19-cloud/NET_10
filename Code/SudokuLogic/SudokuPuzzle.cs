namespace SudokuLogic;

/// <summary>Một đề Sudoku: đề (0 = ô trống) và đáp án đầy đủ.</summary>
public sealed class SudokuPuzzle
{
    public int[][] Puzzle { get; }
    public int[][] Solution { get; }
    public int EmptyCount { get; }

    public SudokuPuzzle(int[][] puzzle, int[][] solution)
    {
        Puzzle = puzzle;
        Solution = solution;
        EmptyCount = puzzle.Sum(r => r.Count(v => v == 0));
    }
}