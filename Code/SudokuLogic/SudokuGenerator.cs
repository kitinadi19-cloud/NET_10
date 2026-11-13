using System.Numerics;

namespace SudokuLogic;

/// <summary>Sinh đề Sudoku hợp lệ, có nghiệm duy nhất.</summary>
public static class SudokuGenerator
{
    public const int Size = 9;

    public static int[][] NewGrid() => Enumerable.Range(0, Size).Select(_ => new int[Size]).ToArray();

    public static int[][] Clone(int[][] g) => g.Select(r => (int[])r.Clone()).ToArray();

    public static SudokuPuzzle Generate(int holes = 40, Random? rng = null)
    {
        rng ??= Random.Shared;
        holes = Math.Clamp(holes, 1, 55);

        var solution = NewGrid();
        FillRandom(solution, rng);

        var puzzle = Clone(solution);
        var order = Enumerable.Range(0, Size * Size).OrderBy(_ => rng.Next()).ToList();
        int removed = 0;
        foreach (int idx in order)
        {
            if (removed >= holes) break;
            int r = idx / Size, c = idx % Size;
            int backup = puzzle[r][c];
            puzzle[r][c] = 0;
            if (CountSolutions(Clone(puzzle), 2) != 1) puzzle[r][c] = backup; // giữ nghiệm duy nhất
            else removed++;
        }
        return new SudokuPuzzle(puzzle, solution);
    }

    /// <summary>Giải một bảng (trả về null nếu vô nghiệm).</summary>
    public static int[][]? Solve(int[][] grid)
    {
        var g = Clone(grid);
        return SolveInPlace(g) ? g : null;
    }

    public static bool IsValidPlacement(int[][] g, int r, int c, int v)
    {
        for (int i = 0; i < Size; i++)
        {
            if (i != c && g[r][i] == v) return false;
            if (i != r && g[i][c] == v) return false;
        }
        int br = r / 3 * 3, bc = c / 3 * 3;
        for (int i = br; i < br + 3; i++)
            for (int j = bc; j < bc + 3; j++)
                if ((i != r || j != c) && g[i][j] == v) return false;
        return true;
    }

    /// <summary>Bảng đã đầy và thỏa mọi luật hàng/cột/khối.</summary>
    public static bool IsCompleteAndValid(int[][] g)
    {
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                int v = g[r][c];
                if (v < 1 || v > 9 || !IsValidPlacement(g, r, c, v)) return false;
            }
        return true;
    }

    // ----- nội bộ -----
    private static int Candidates(int[][] g, int r, int c)
    {
        int used = 0;
        for (int i = 0; i < Size; i++) { used |= 1 << g[r][i]; used |= 1 << g[i][c]; }
        int br = r / 3 * 3, bc = c / 3 * 3;
        for (int i = br; i < br + 3; i++)
            for (int j = bc; j < bc + 3; j++) used |= 1 << g[i][j];
        return ~used & 0x3FE; // bit 1..9
    }

    private static bool FillRandom(int[][] g, Random rng)
    {
        if (!FindBest(g, out int r, out int c, out int mask)) return true;
        var digits = Enumerable.Range(1, 9).Where(v => (mask & (1 << v)) != 0).OrderBy(_ => rng.Next()).ToList();
        foreach (int v in digits)
        {
            g[r][c] = v;
            if (FillRandom(g, rng)) return true;
        }
        g[r][c] = 0;
        return false;
    }

    private static bool SolveInPlace(int[][] g)
    {
        if (!FindBest(g, out int r, out int c, out int mask)) return true;
        for (int v = 1; v <= 9; v++)
        {
            if ((mask & (1 << v)) == 0) continue;
            g[r][c] = v;
            if (SolveInPlace(g)) return true;
        }
        g[r][c] = 0;
        return false;
    }

    private static int CountSolutions(int[][] g, int limit)
    {
        if (!FindBest(g, out int r, out int c, out int mask)) return 1;
        if (mask == 0) return 0;
        int total = 0;
        for (int v = 1; v <= 9; v++)
        {
            if ((mask & (1 << v)) == 0) continue;
            g[r][c] = v;
            total += CountSolutions(g, limit - total);
            if (total >= limit) break;
        }
        g[r][c] = 0;
        return total;
    }

    /// <summary>Tìm ô trống có ít ứng viên nhất. Trả false nếu bảng đã đầy.</summary>
    private static bool FindBest(int[][] g, out int br, out int bc, out int bmask)
    {
        br = bc = -1; bmask = 0;
        int best = 10;
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (g[r][c] != 0) continue;
                int mask = Candidates(g, r, c);
                int n = BitOperations.PopCount((uint)mask);
                if (n < best) { best = n; br = r; bc = c; bmask = mask; if (n == 0) return true; }
            }
        return br >= 0;
    }
}