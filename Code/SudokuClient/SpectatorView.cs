using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SudokuShared.Messages;

namespace SudokuClient;

public sealed partial class SpectatorViewForm : Form
{
    private enum Request
    {
        None, Login, List, Watch, Leave
    }

    private NetworkClient? _net;
    private Request _pending;
    private DateTime _deadline;
    private bool _loggedIn;
    private bool _closing;

    private string? _requestedId;
    private string? _watchingId;
    private MatchSnapshot? _lastSnapshot;

    private readonly System.Windows.Forms.Timer _timeoutTimer =
        new() { Interval = 500 };

    public SpectatorViewForm()
    {
        Text = "Sudoku - Khán giả";
        ClientSize = new Size(1140, 880);
        MinimumSize = new Size(1020, 800);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        WireEvents();
        UpdateButtons();
        _timeoutTimer.Start();
    }

    private void WireEvents()
    {
        _btnConnect.Click += async (_, _) => await ConnectAsync();
        _btnDisconnect.Click += (_, _) =>
            Disconnect("Đã ngắt kết nối.", false);

        _btnRefresh.Click += async (_, _) =>
            await SendAsync(Request.List);

        _btnWatch.Click += async (_, _) => await WatchAsync();
        _btnLeave.Click += async (_, _) => await LeaveAsync();
        _matches.DoubleClick += async (_, _) => await WatchAsync();
        _matches.SelectedIndexChanged += (_, _) => UpdateButtons();

        _timeoutTimer.Tick += (_, _) =>
        {
            if (_pending != Request.None && DateTime.UtcNow >= _deadline)
                Disconnect("Hết thời gian chờ server. Hãy kết nối lại.");
        };
    }

    private void BeginRequest(Request request)
    {
        _pending = request;
        _deadline = DateTime.UtcNow.AddSeconds(15);
        UpdateButtons();
    }

    private void CompleteRequest()
    {
        _pending = Request.None;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool idle = _pending == Request.None;
        bool connected = _net != null;
        bool watching = _watchingId != null;

        _btnConnect.Enabled = !connected && idle;
        _btnDisconnect.Enabled = connected;
        _txtHost.Enabled = _numPort.Enabled = !connected;

        _matches.Enabled = _loggedIn && idle && !watching;
        _btnRefresh.Enabled = _loggedIn && idle && !watching;
        _btnWatch.Enabled = _matches.Enabled &&
                            _matches.SelectedItems.Count > 0;

        _btnLeave.Enabled = idle && (watching || _lastSnapshot != null);
        _btnLeave.Text = watching ? "Rời phòng xem" : "Đóng bảng";
    }

    private void SetStatus(string text, bool error = false)
    {
        _lblStatus.Text = text;
        _lblStatus.ForeColor = error ? Color.Firebrick : Color.DarkBlue;
    }

    private void Log(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _log.Items.Add($"{DateTime.Now:HH:mm:ss}  {text}");
        while (_log.Items.Count > 200)
            _log.Items.RemoveAt(0);

        _log.TopIndex = _log.Items.Count - 1;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closing = true;
        _timeoutTimer.Stop();
        _timeoutTimer.Dispose();

        NetworkClient? old = _net;
        _net = null;
        old?.Dispose();

        base.OnFormClosed(e);
    }
}