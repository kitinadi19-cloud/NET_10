using System.Drawing;
using System.Windows.Forms;
using SudokuShared.Messages;

namespace SudokuClient;

public sealed partial class SpectatorViewForm
{
    private readonly TextBox _txtHost =
        new() { Text = "127.0.0.1", Width = 120 };

    private readonly NumericUpDown _numPort = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = ProtocolConstants.DefaultPort,
        Width = 75
    };

    private readonly Label _lblViewer = MakeLabel("Khán giả: chưa kết nối");
    private readonly Label _lblMatch = MakeLabel("Chọn một trận để theo dõi");
    private readonly Label _lblTime = MakeLabel("Còn lại: --:--");

    private readonly Label _lblStatus = new()
    {
        Text = "Chưa kết nối",
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Button _btnConnect = MakeButton("Kết nối");
    private readonly Button _btnDisconnect = MakeButton("Ngắt kết nối");
    private readonly Button _btnRefresh = MakeButton("Làm mới");
    private readonly Button _btnWatch = MakeButton("Xem trận");
    private readonly Button _btnLeave = MakeButton("Rời phòng xem");

    private readonly ListView _matches = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false
    };

    private readonly ListBox _log =
        new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };

    private readonly SpectatorPlayerView _playerA = new("Người chơi 1");
    private readonly SpectatorPlayerView _playerB = new("Người chơi 2");

    private static Label MakeLabel(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(5) };

    private static Button MakeButton(string text) =>
        new() { Text = text, Width = 125, Height = 32 };

    private void BuildUi()
    {
        _lblMatch.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _lblTime.Font = new Font("Segoe UI", 11);

        _matches.Columns.Add("Mã trận", 90);
        _matches.Columns.Add("Người chơi 1", 150);
        _matches.Columns.Add("Người chơi 2", 150);
        _matches.Columns.Add("Điểm", 100);
        _matches.Columns.Add("Còn lại", 90);
        _matches.Columns.Add("Trạng thái", 120);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(10)
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var connection = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true
        };

        connection.Controls.AddRange(new Control[]
        {
            MakeLabel("Server:"), _txtHost,
            MakeLabel("Port:"), _numPort,
            _btnConnect, _btnDisconnect, _lblViewer
        });

        var matchArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        matchArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        matchArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

        var matchGroup = new GroupBox
        {
            Text = "Các trận đang diễn ra",
            Dock = DockStyle.Fill
        };
        matchGroup.Controls.Add(_matches);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4, 15, 0, 0)
        };
        actions.Controls.AddRange(
            new Control[] { _btnRefresh, _btnWatch, _btnLeave });

        matchArea.Controls.Add(matchGroup, 0, 0);
        matchArea.Controls.Add(actions, 1, 0);

        var info = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        info.Controls.Add(_lblMatch);
        info.Controls.Add(_lblTime);
        info.Controls.Add(MakeLabel(
            "Chỉ xem • Số đen: đề bài • Số xanh: người chơi đã điền"));

        var boards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        boards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        boards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        boards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        boards.Controls.Add(_playerA, 0, 0);
        boards.Controls.Add(_playerB, 1, 0);

        var logGroup = new GroupBox
        {
            Text = "Diễn biến trận đấu",
            Dock = DockStyle.Fill
        };
        logGroup.Controls.Add(_log);

        root.Controls.Add(connection, 0, 0);
        root.Controls.Add(matchArea, 0, 1);
        root.Controls.Add(info, 0, 2);
        root.Controls.Add(boards, 0, 3);
        root.Controls.Add(logGroup, 0, 4);
        root.Controls.Add(_lblStatus, 0, 5);
        Controls.Add(root);
    }
}