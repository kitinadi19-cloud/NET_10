using SudokuClient.Core;
using SudokuShared.Messages;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SudokuClient;

public partial class Form1 : Form
{
    private readonly NetworkClient networkClient = new();
    private readonly TextBox[,] cells = new TextBox[9, 9];
    private readonly bool[,] fixedCells = new bool[9, 9];

    private readonly TextBox ip = new();
    private readonly NumericUpDown port = new();

    private readonly Label clock = new();
    private readonly Label status = new();
    private readonly Label playerScore = new();
    private readonly Label opponentScore = new();
    private readonly Label opponentName = new();

    private readonly System.Windows.Forms.Timer timer = new();

    private int selectedRow = -1;
    private int selectedCol = -1;
    private int seconds = 600;

    private int myScore = 0;
    private int enemyScore = 0;

    private bool matchStarted = false;

    private readonly int[,] puzzle =
    {
        {0,1,2,0,0,0,3,4,0},
        {4,0,0,5,0,0,0,0,2},
        {0,0,6,0,2,3,0,1,0},
        {7,0,0,0,4,0,0,0,0},
        {5,0,8,0,0,0,2,0,9},
        {0,0,0,0,8,0,0,0,6},
        {0,8,0,2,1,0,5,0,0},
        {1,0,0,0,0,6,0,0,3},
        {0,9,5,0,0,0,7,2,0}
    };

    public Form1()
    {
        InitializeComponent();

        Text = "Sudoku Battle";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1100, 800);
        MinimumSize = new Size(1000, 750);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);

        BuildInterface();
        LoadPuzzle();

        timer.Interval = 1000;
        timer.Tick += (_, _) =>
        {
            if (seconds > 0)
            {
                seconds--;
                UpdateClock();
            }
            else
            {
                timer.Stop();
                status.Text = "Trận đấu đã hết thời gian.";
                matchStarted = false;
            }
        };

        UpdateClock();
        UpdateScores();
    }

    private Label MakeLabel(
        string text, int x, int y, int width = 0,
        int height = 30, int size = 10, bool bold = false)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width == 0 ? 300 : width, height),
            AutoSize = width == 0,
            Font = new Font(
                "Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = Color.FromArgb(30, 41, 59)
        };
    }

    private Button MakeButton(
        string text, int x, int y, int width, int height,
        Action action)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 64, 175),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();

        return button;
    }

    private void BuildInterface()
    {
        // HEADER
        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(1100, 95),
            BackColor = Color.White
        };

        var title = MakeLabel(
            "SUDOKU BATTLE", 30, 10, 400, 40, 20, true);

        var subtitle = MakeLabel(
            "ĐỐI KHÁNG SUDOKU TRỰC TUYẾN",
            32, 57, 500, 25, 10);

        subtitle.ForeColor = Color.Gray;

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        // CONNECTION
        var connection = new Panel
        {
            Location = new Point(30, 110),
            Size = new Size(1020, 65),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        ip.Text = "127.0.0.1";
        ip.SetBounds(20, 25, 180, 27);

        port.Minimum = 1;
        port.Maximum = 65535;
        port.Value = 8888;
        port.SetBounds(220, 25, 90, 27);

        var connectButton = MakeButton(
         "Kết nối", 335, 21, 110, 34,
         async () => await ConnectToServerAsync());

         connection.Controls.AddRange(new Control[]
        {
            MakeLabel("IP SERVER", 20, 3, 150, 20, 8, true),
            ip,
            MakeLabel("PORT", 220, 3, 80, 20, 8, true),
            port,
            connectButton
        });

        Controls.Add(connection);

        // PLAYER INFORMATION
        var playerPanel = new Panel
        {
            Location = new Point(30, 195),
            Size = new Size(1020, 95),
            BackColor = Color.White
        };

        var myPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(300, 85),
            BackColor = Color.FromArgb(240, 253, 244)
        };

        myPanel.Controls.Add(MakeLabel(
            "BẠN", 15, 10, 200, 22, 10, true));

        playerScore.SetBounds(15, 38, 250, 35);
        playerScore.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        playerScore.ForeColor = Color.FromArgb(22, 163, 74);
        myPanel.Controls.Add(playerScore);

        var enemyPanel = new Panel
        {
            Location = new Point(720, 0),
            Size = new Size(300, 85),
            BackColor = Color.FromArgb(239, 246, 255)
        };

        opponentName.SetBounds(15, 10, 250, 22);
        opponentName.Text = "ĐỐI THỦ: CHƯA TÌM THẤY";
        opponentName.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        enemyPanel.Controls.Add(opponentName);

        opponentScore.SetBounds(15, 38, 250, 35);
        opponentScore.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        opponentScore.ForeColor = Color.FromArgb(37, 99, 235);
        enemyPanel.Controls.Add(opponentScore);

        // TIMER
        var timerPanel = new Panel
        {
            Location = new Point(380, 0),
            Size = new Size(260, 85),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        timerPanel.Controls.Add(MakeLabel(
            "THỜI GIAN CÒN LẠI",
            0, 7, 260, 20, 9, true));

        clock.SetBounds(0, 25, 260, 55);
        clock.TextAlign = ContentAlignment.MiddleCenter;
        clock.Font = new Font("Segoe UI", 27, FontStyle.Bold);
        clock.ForeColor = Color.FromArgb(30, 41, 59);
        timerPanel.Controls.Add(clock);

        playerPanel.Controls.Add(myPanel);
        playerPanel.Controls.Add(enemyPanel);
        playerPanel.Controls.Add(timerPanel);

        Controls.Add(playerPanel);

        // SUDOKU BOARD
        var board = new Panel
        {
           Size = new Size(540, 540),
           BackColor = Color.White,
           Anchor = AnchorStyles.None
        };

        board.Location = new Point(
              (ClientSize.Width - board.Width) / 2,
               300
        );

        board.Paint += DrawBoardLines;

        int cellSize = 60;

        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            var cell = new TextBox
            {
                Location = new Point(
                    c * cellSize + 2,
                    r * cellSize + 2),
                Size = new Size(cellSize - 4, cellSize - 4),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 25, FontStyle.Regular),
                MaxLength = 1,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            int row = r;
            int col = c;

            cell.Enter += (_, _) => SelectCell(row, col);

            cell.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) &&
                    (e.KeyChar < '1' || e.KeyChar > '9'))
                {
                    e.Handled = true;
                }
            };

            cells[r, c] = cell;
            board.Controls.Add(cell);
        }

        Controls.Add(board);

        // STATUS
        status.SetBounds(30, 850, 1020, 30);
        status.Text = "Đang chờ kết nối và ghép trận...";
        status.ForeColor = Color.Gray;
        Controls.Add(status);
    }

    private void DrawBoardLines(object? sender, PaintEventArgs e)
    {
        using var thinPen = new Pen(Color.LightGray, 1);
        using var thickPen = new Pen(Color.Black, 2);

        for (int i = 0; i <= 9; i++)
        {
            Pen pen = i % 3 == 0 ? thickPen : thinPen;
            int position = i * 60;

            e.Graphics.DrawLine(
                pen, position, 0, position, 540);

            e.Graphics.DrawLine(
                pen, 0, position, 540, position);
        }
    }

    private void LoadPuzzle()
    {
        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            int value = puzzle[r, c];

            fixedCells[r, c] = value != 0;

            cells[r, c].Text =
                value == 0 ? "" : value.ToString();

            cells[r, c].ReadOnly = fixedCells[r, c];

            cells[r, c].ForeColor = fixedCells[r, c]
            ? Color.Black
            : Color.FromArgb(37, 99, 235);
        }
    }

    private void SelectCell(int r, int c)
    {
        selectedRow = r;
        selectedCol = c;
    }

    // Gọi khi Server thông báo trận đấu bắt đầu.
    public void StartMatch(string name)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => StartMatch(name)));
            return;
        }

        seconds = 600;
        myScore = 0;
        enemyScore = 0;

        matchStarted = true;

        opponentName.Text = $"ĐỐI THỦ: {name}";
        status.Text = "Trận đấu đang diễn ra.";

        UpdateScores();
        UpdateClock();

        timer.Start();
    }

    // Gọi khi Server gửi điểm số mới.
    public void UpdateMatchScores(int myNewScore, int enemyNewScore)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() =>
                UpdateMatchScores(myNewScore, enemyNewScore)));
            return;
        }

        myScore = myNewScore;
        enemyScore = enemyNewScore;

        UpdateScores();
    }

    private void UpdateScores()
    {
        playerScore.Text = $"{myScore} ĐIỂM";
        opponentScore.Text = $"{enemyScore} ĐIỂM";
    }

    private void UpdateClock()
    {
        clock.Text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private async Task ConnectToServerAsync()
{
    try
    {
        status.Text = "Đang kết nối Server...";

        await networkClient.ConnectAsync(
            ip.Text.Trim(),
            (int)port.Value);

        status.Text = "Kết nối thành công! Đang tìm đối thủ...";

        networkClient.MessageReceived -= OnServerMessage;
        networkClient.MessageReceived += OnServerMessage;

        networkClient.ConnectionClosed -= OnConnectionClosed;
        networkClient.ConnectionClosed += OnConnectionClosed;

        await networkClient.SendAsync(MessageTypes.JoinQueue);
    }
    catch (Exception ex)
    {
        status.Text = "Kết nối thất bại: " + ex.Message;
    }
}

private void OnServerMessage(ResponseMessage response)
{
    if (IsDisposed || !IsHandleCreated)
        return;

    BeginInvoke(new Action(() =>
    {
        switch (response.Status)
        {
            case "WAITING":
                status.Text = "Đang chờ người chơi khác...";
                break;

            case "MATCH_FOUND":
                status.Text = "Đã tìm thấy đối thủ!";
                StartMatch("Đối thủ");
                break;

            case "ERROR":
                status.Text = "Lỗi Server: " + response.Message;
                break;

            default:
                status.Text = response.Message;
                break;
        }
    }));
}

private void OnConnectionClosed(string message)
{
    if (IsDisposed || !IsHandleCreated)
        return;

    BeginInvoke(new Action(() =>
    {
        timer.Stop();
        matchStarted = false;
        status.Text = "Mất kết nối Server: " + message;
    }));
}
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        timer.Dispose();
        networkClient.Dispose();
        
        base.OnFormClosed(e);
    }
}
