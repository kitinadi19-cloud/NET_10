namespace SudokuClient;

/// <summary>Vẽ bảng Sudoku 9x9; có thể chọn ô và nhập số bằng bàn phím khi Interactive = true.</summary>
public sealed class BoardControl : Control
{
    private int _selR = -1, _selC = -1;
    private (int R, int C, int V)? _wrong;
    private readonly System.Windows.Forms.Timer _flash = new() { Interval = 800 };

    public int[][]? Puzzle { get; set; }
    public int[][]? Cells { get; set; }
    public bool Interactive { get; set; }

    /// <summary>(row, col, value) – người dùng muốn điền số.</summary>
    public event Action<int, int, int>? CellEntered;

    public BoardControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        MinimumSize = new Size(270, 270);
        _flash.Tick += (_, _) => { _flash.Stop(); _wrong = null; Invalidate(); };
    }

    public void ClearSelection() { _selR = _selC = -1; Invalidate(); }

    public void FlashWrong(int r, int c, int v)
    {
        _wrong = (r, c, v);
        _flash.Stop(); _flash.Start();
        Invalidate();
    }

    /// <summary>Dùng cho bàn phím số trên màn hình.</summary>
    public void Enter(int value)
    {
        if (Interactive && _selR >= 0 && CanEdit(_selR, _selC))
            CellEntered?.Invoke(_selR, _selC, value);
    }

    private bool CanEdit(int r, int c)
        => Puzzle != null && Cells != null && Puzzle[r][c] == 0 && Cells[r][c] == 0;

    protected override bool IsInputKey(Keys keyData)
        => keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        float cs = Math.Min(Width, Height) / 9f;
        int c = (int)(e.X / cs), r = (int)(e.Y / cs);
        if (r is >= 0 and < 9 && c is >= 0 and < 9) { _selR = r; _selC = c; Invalidate(); }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_selR < 0) { _selR = _selC = 0; }
        switch (e.KeyCode)
        {
            case Keys.Up: _selR = Math.Max(0, _selR - 1); break;
            case Keys.Down: _selR = Math.Min(8, _selR + 1); break;
            case Keys.Left: _selC = Math.Max(0, _selC - 1); break;
            case Keys.Right: _selC = Math.Min(8, _selC + 1); break;
            default:
                int v = e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9 ? e.KeyCode - Keys.D0
                      : e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9 ? e.KeyCode - Keys.NumPad0 : 0;
                if (v > 0) Enter(v);
                break;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.White);
        float cs = Math.Min(Width, Height) / 9f;
        using var fGiven = new Font("Segoe UI", cs * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var fUser = new Font("Segoe UI", cs * 0.42f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                var rect = new RectangleF(c * cs, r * cs, cs, cs);
                bool sel = r == _selR && c == _selC;
                bool related = _selR >= 0 && (r == _selR || c == _selC);
                if (_wrong is { } w && w.R == r && w.C == c) g.FillRectangle(Brushes.MistyRose, rect);
                else if (sel) g.FillRectangle(Brushes.LightSkyBlue, rect);
                else if (related) g.FillRectangle(Brushes.AliceBlue, rect);

                if (_wrong is { } w2 && w2.R == r && w2.C == c)
                {
                    g.DrawString(w2.V.ToString(), fUser, Brushes.Red, rect, sf);
                    continue;
                }
                int v = Cells != null ? Cells[r][c] : (Puzzle != null ? Puzzle[r][c] : 0);
                if (v == 0) continue;
                bool given = Puzzle != null && Puzzle[r][c] != 0;
                g.DrawString(v.ToString(), given ? fGiven : fUser, given ? Brushes.Black : Brushes.RoyalBlue, rect, sf);
            }

        using var thin = new Pen(Color.Silver, 1);
        using var thick = new Pen(Color.Black, 2.5f);
        for (int i = 0; i <= 9; i++)
        {
            var pen = i % 3 == 0 ? thick : thin;
            g.DrawLine(pen, i * cs, 0, i * cs, 9 * cs);
            g.DrawLine(pen, 0, i * cs, 9 * cs, i * cs);
        }
        if (Focused) g.DrawRectangle(Pens.DodgerBlue, 0, 0, 9 * cs - 1, 9 * cs - 1);
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _flash.Dispose();
        base.Dispose(disposing);
    }
}
