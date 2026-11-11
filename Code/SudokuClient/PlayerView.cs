using System;
using System.Drawing;
using System.Windows.Forms;
using SudokuShared.Messages;

namespace SudokuClient;

internal sealed class SpectatorPlayerView : GroupBox
{
    private readonly Label _info = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Font = new Font("Segoe UI", 11)
    };

    private readonly ProgressBar _progress = new()
    {
        Dock = DockStyle.Fill,
        Minimum = 0,
        Maximum = 100
    };

    private readonly BoardControl _board = new()
    {
        Dock = DockStyle.Fill,
        Interactive = false, // Không phát sinh nước đi.
        Enabled = false,     // Không nhận chuột hoặc bàn phím.
        TabStop = false
    };

    public SpectatorPlayerView(string title)
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        layout.Controls.Add(_info, 0, 0);
        layout.Controls.Add(_board, 0, 1);
        layout.Controls.Add(_progress, 0, 2);
        Controls.Add(layout);

        ClearBoard(title);
    }

    public void Display(int[][] puzzle, PlayerSnapshot player)
    {
        Text = player.Name;
        _info.Text = $"Điểm: {player.Score}    " +
                     $"Tiến độ: {player.Filled}/{player.Total} ô";

        int percent = player.Total > 0
            ? (int)(player.Filled * 100.0 / player.Total) : 0;

        _progress.Value = Math.Clamp(percent, 0, 100);
        _board.Puzzle = puzzle;
        _board.Cells = player.Board;
        _board.ClearSelection();
        _board.Invalidate();
    }

    public void ClearBoard(string title)
    {
        Text = title;
        _info.Text = "Chưa có dữ liệu trận đấu";
        _progress.Value = 0;
        _board.Puzzle = _board.Cells = null;
        _board.ClearSelection();
        _board.Invalidate();
    }
}