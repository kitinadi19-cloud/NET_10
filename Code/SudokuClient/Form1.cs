
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SudokuClient;

public partial class Form1 : Form
{
    private readonly TextBox[,] cells = new TextBox[9, 9];
    private readonly bool[,] fixedCells = new bool[9, 9];
    private readonly Label status = new();
    private readonly Label clock = new();
    private readonly TextBox ip = new();
    private readonly NumericUpDown port = new();
    private readonly System.Windows.Forms.Timer timer = new();

    private int selectedRow = -1, selectedCol = -1, seconds = 600;

    private readonly int[,] puzzle =
    {
        {5,3,0,0,7,0,0,0,0}, {6,0,0,1,9,5,0,0,0},
        {0,9,8,0,0,0,0,6,0}, {8,0,0,0,6,0,0,0,3},
        {4,0,0,8,0,3,0,0,1}, {7,0,0,0,2,0,0,0,6},
        {0,6,0,0,0,0,2,8,0}, {0,0,0,4,1,9,0,0,5},
        {0,0,0,0,8,0,0,7,9}
    };

    public Form1()
    {
        InitializeComponent();
        Text = "Sudoku Battle - Client";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1050, 760);
        BackColor = Color.FromArgb(242, 245, 249);
        Font = new Font("Segoe UI", 10);

        BuildInterface();
        LoadPuzzle();
        UpdateClock();

        timer.Interval = 1000;
        timer.Tick += (_, _) =>
        {
            if (seconds > 0)
                seconds--;
            else
            {
                timer.Stop();
                status.Text = "Đã hết thời gian (demo).";
            }
            UpdateClock();
        };
    }

    private Label MakeLabel(string text, int x, int y, int width = 0,
                            int size = 10, bool bold = false)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = width == 0,
            Size = new Size(width, 60),
            Font = new Font("Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular)
        };
    }

    private Button MakeButton(string text, int x, int y, int w, int h,
                              Action action)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            Font = new Font("Segoe UI", 11),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void BuildInterface()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90,
            BackColor = Color.FromArgb(30, 41, 59)
        };
        var title = MakeLabel("SUDOKU BATTLE", 25, 12, size: 22, bold: true);
        title.ForeColor = Color.White;
        var subtitle = MakeLabel("Đối kháng qua mạng • Giao diện thử nghiệm",
            25, 55);
        subtitle.ForeColor = Color.LightGray;
        header.Controls.AddRange(new Control[] { title, subtitle });
        Controls.Add(header);

        var connection = new Panel
        {
            Location = new Point(24, 110),
            Size = new Size(1000, 75),
            BackColor = Color.White
        };

        ip.Text = "127.0.0.1";
        ip.SetBounds(16, 28, 180, 28);
        port.SetBounds(220, 28, 90, 28);
        port.Minimum = 1;
        port.Maximum = 65535;
        port.Value = 8888;

        var connect = MakeButton("Kết nối", 350, 25, 120, 35,
            () => status.Text = $"Demo: chưa kết nối {ip.Text}:{port.Value}");
        connect.BackColor = Color.RoyalBlue;
        connect.ForeColor = Color.White;

        connection.Controls.AddRange(new Control[]
        {
            MakeLabel("IP Server", 16, 5), ip,
            MakeLabel("Port", 220, 5), port, connect
        });
        Controls.Add(connection);

        var game = new Panel
        {
            Location = new Point(24, 205),
            Size = new Size(620, 510),
            BackColor = Color.White
        };

        var board = new TableLayoutPanel
        {
            Location = new Point(17, 17),
            Size = new Size(450, 450),
            RowCount = 9,
            ColumnCount = 9,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };

        for (int i = 0; i < 9; i++)
        {
            board.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 9));
            board.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 9));
        }

        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            var cell = new TextBox
            {
                Dock = DockStyle.Fill,
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 1,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Margin = Padding.Empty,
                Tag = new Point(r, c)
            };

            int row = r, col = c;
            cell.Enter += (_, _) => SelectCell(row, col);
            cell.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) &&
                    (e.KeyChar < '1' || e.KeyChar > '9'))
                    e.Handled = true;
            };

            cells[r, c] = cell;
            board.Controls.Add(cell, c, r);
        }

        game.Controls.Add(board);
        game.Controls.Add(MakeLabel(
            "Chọn ô trống rồi nhập số từ 1 đến 9.", 20, 475));
        Controls.Add(game);

        var info = new Panel
        {
            Location = new Point(665, 205),
            Size = new Size(359, 510),
            BackColor = Color.White
        };

        info.Controls.Add(MakeLabel("THÔNG TIN TRẬN ĐẤU",
            20, 20, size: 13, bold: true));

        clock.SetBounds(20, 60, 300, 55);
        clock.Font = new Font("Segoe UI", 25, FontStyle.Bold);
        clock.ForeColor = Color.RoyalBlue;
        info.Controls.Add(clock);

        info.Controls.Add(MakeLabel("Đối thủ: Chưa tìm thấy", 20, 120));

        status.Text = "Trạng thái: Chưa kết nối";
        status.SetBounds(20, 155, 320, 65);
        info.Controls.Add(status);

        info.Controls.Add(MakeLabel("NHẬP SỐ", 20, 245,
            size: 13, bold: true));

        for (int n = 1; n <= 9; n++)
        {
            int number = n;
            int x = 20 + ((n - 1) % 5) * 58;
            int y = 285 + ((n - 1) / 5) * 48;
            info.Controls.Add(MakeButton(number.ToString(), x, y, 48, 40,
                () => EnterNumber(number)));
        }

        info.Controls.Add(MakeButton("Xóa ô", 20, 390, 115, 36,
            () => EnterNumber(0)));

        info.Controls.Add(MakeButton("Chạy đồng hồ demo", 20, 445, 200, 36,
            () =>
            {
                seconds = 600;
                UpdateClock();
                timer.Start();
                status.Text = "Trạng thái: Đang chạy demo";
            }));

        Controls.Add(info);
    }

    private void LoadPuzzle()
    {
        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            int value = puzzle[r, c];
            fixedCells[r, c] = value != 0;
            cells[r, c].Text = value == 0 ? "" : value.ToString();
            cells[r, c].ReadOnly = fixedCells[r, c];
            cells[r, c].BackColor = fixedCells[r, c]
                ? Color.FromArgb(226, 232, 240) : Color.White;
            cells[r, c].ForeColor = Color.FromArgb(30, 41, 59);
        }
    }

    private void SelectCell(int r, int c)
    {
        selectedRow = r;
        selectedCol = c;

        for (int i = 0; i < 9; i++)
        for (int j = 0; j < 9; j++)
            cells[i, j].BackColor = fixedCells[i, j]
                ? Color.FromArgb(226, 232, 240) : Color.White;

        if (!fixedCells[r, c])
            cells[r, c].BackColor = Color.FromArgb(219, 234, 254);
    }

    private void EnterNumber(int number)
    {
        if (selectedRow < 0)
        {
            status.Text = "Hãy chọn một ô trước.";
            return;
        }

        if (fixedCells[selectedRow, selectedCol])
        {
            status.Text = "Không thể sửa ô đề bài.";
            return;
        }

        cells[selectedRow, selectedCol].Text =
            number == 0 ? "" : number.ToString();
        status.Text = number == 0 ? "Đã xóa ô." : $"Đã nhập số {number} (demo).";
    }

    private void UpdateClock()
    {
        clock.Text = $"⏱ {seconds / 60:00}:{seconds % 60:00}";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Dispose();
        base.OnFormClosed(e);
    }
}
