using SudokuShared;
using SudokuShared.Messages;

namespace SudokuClient;

public sealed class MainForm : Form
{
    private enum View { Lobby, Game, Spectate }

    private readonly NetworkClient _net = new();

    // Kết nối
    private readonly TextBox _txtHost = new() { Text = "127.0.0.1", Width = 120 };
    private readonly NumericUpDown _numPort = new() { Minimum = 1, Maximum = 65535, Value = ProtocolConstants.DefaultPort, Width = 70 };
    private readonly TextBox _txtName = new() { Text = "Player" + Random.Shared.Next(100, 999), Width = 120 };
    private readonly Button _btnConnect = new() { Text = "Kết nối", AutoSize = true };
    private readonly Button _btnDisconnect = new() { Text = "Ngắt", AutoSize = true, Enabled = false };
    private readonly Label _lblStatus = new() { AutoSize = true, Text = "Chưa kết nối", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(10, 6, 0, 0) };

    // Sảnh
    private readonly Panel _pnlLobby = new() { Dock = DockStyle.Fill, Enabled = false };
    private readonly ListView _lvPlayers = NewList(("Người chơi", 160), ("Trạng thái", 100));
    private readonly ListView _lvMatches = NewList(("Mã", 60), ("Người chơi 1", 110), ("Người chơi 2", 110), ("Điểm", 70), ("Còn lại", 60), ("Trạng thái", 80));
    private readonly Button _btnJoin = new() { Text = "Tìm trận", AutoSize = true };
    private readonly Button _btnLeaveQueue = new() { Text = "Hủy chờ", AutoSize = true, Enabled = false };
    private readonly Button _btnChallenge = new() { Text = "Thách đấu người đã chọn", AutoSize = true };
    private readonly Button _btnRefresh = new() { Text = "Làm mới danh sách", AutoSize = true };
    private readonly Button _btnWatch = new() { Text = "Xem trận đã chọn", AutoSize = true };

    // Trận đấu
    private readonly Panel _pnlGame = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly BoardControl _boardMe = new() { Dock = DockStyle.Fill, Interactive = true };
    private readonly Label _lblTime = new() { Text = "10:00", Font = new Font("Segoe UI", 26, FontStyle.Bold), AutoSize = true };
    private readonly Label _lblMe = new() { AutoSize = true, Font = new Font("Segoe UI", 11) };
    private readonly Label _lblOpp = new() { AutoSize = true, Font = new Font("Segoe UI", 11) };
    private readonly Label _lblInfo = new() { AutoSize = true, MaximumSize = new Size(240, 0), ForeColor = Color.DarkRed };
    private readonly Button _btnSurrender = new() { Text = "Bỏ cuộc", AutoSize = true, BackColor = Color.MistyRose };

    // Khán giả
    private readonly Panel _pnlSpec = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly BoardControl _boardA = new() { Dock = DockStyle.Fill, Interactive = false };
    private readonly BoardControl _boardB = new() { Dock = DockStyle.Fill, Interactive = false };
    private readonly Label _lblSpecA = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly Label _lblSpecB = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly Label _lblSpecTime = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Button _btnLeaveSpec = new() { Text = "Rời phòng xem", AutoSize = true };

    private readonly ListBox _lstLog = new() { Dock = DockStyle.Bottom, Height = 110, HorizontalScrollbar = true };

    private string _host = "127.0.0.1";
    private int _port = ProtocolConstants.DefaultPort;
    private string _myName = "";
    private bool _loggedIn, _inGame, _inQueue, _intentionalDisconnect, _reconnecting;

    public MainForm()
    {
        Text = "Sudoku Đối Kháng - UDM_17";
        ClientSize = new Size(1000, 720);
        MinimumSize = new Size(900, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        WireEvents();
        ShowView(View.Lobby);
    }

    // ======================= Giao diện =======================
    private static ListView NewList(params (string Name, int Width)[] cols)
    {
        var lv = new ListView { Dock = DockStyle.Fill, View = System.Windows.Forms.View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false };
        foreach (var (n, w) in cols) lv.Columns.Add(n, w);
        return lv;
    }

    private void BuildUi()
    {
        // Thanh kết nối
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 0, 0) };
        top.Controls.AddRange(new Control[]
        {
            new Label { Text = "Server IP:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _txtHost,
            new Label { Text = "Port:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _numPort,
            new Label { Text = "Tên:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _txtName,
            _btnConnect, _btnDisconnect, _lblStatus
        });

        // Sảnh
        var gbPlayers = new GroupBox { Text = "Người chơi online", Dock = DockStyle.Fill };
        var pb = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        pb.Controls.AddRange(new Control[] { _btnChallenge, _btnRefresh });
        gbPlayers.Controls.Add(_lvPlayers); gbPlayers.Controls.Add(pb);

        var gbMatches = new GroupBox { Text = "Các trận đang diễn ra", Dock = DockStyle.Fill };
        var mb = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        mb.Controls.Add(_btnWatch);
        gbMatches.Controls.Add(_lvMatches); gbMatches.Controls.Add(mb);

        var lobbyTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        lobbyTop.Controls.AddRange(new Control[] { _btnJoin, _btnLeaveQueue });

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        grid.Controls.Add(gbPlayers, 0, 0); grid.Controls.Add(gbMatches, 1, 0);
        _pnlLobby.Controls.Add(grid); _pnlLobby.Controls.Add(lobbyTop);

        // Trận đấu
        var side = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 270, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10) };
        var pad = new TableLayoutPanel { ColumnCount = 3, RowCount = 3, AutoSize = true };
        for (int v = 1; v <= 9; v++)
        {
            int val = v;
            var b = new Button { Text = v.ToString(), Width = 70, Height = 50, Font = new Font("Segoe UI", 14, FontStyle.Bold), TabStop = false };
            b.Click += (_, _) => { _boardMe.Enter(val); _boardMe.Focus(); };
            pad.Controls.Add(b, (v - 1) % 3, (v - 1) / 3);
        }
        side.Controls.AddRange(new Control[]
        {
            new Label { Text = "Thời gian còn lại", AutoSize = true }, _lblTime, _lblMe, _lblOpp, pad,
            new Label { Text = "Chọn ô rồi nhấn phím 1-9 (hoặc bấm số bên trên).\nĐúng +1, sai -1 (số sai không được ghi vào bảng).", AutoSize = true, MaximumSize = new Size(240, 0), ForeColor = Color.DimGray },
            _lblInfo, _btnSurrender
        });
        _pnlGame.Controls.Add(_boardMe); _pnlGame.Controls.Add(side);

        // Khán giả
        var specGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8) };
        specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        specGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        specGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        specGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        specGrid.Controls.Add(_lblSpecA, 0, 0); specGrid.Controls.Add(_lblSpecB, 1, 0);
        specGrid.Controls.Add(_boardA, 0, 1); specGrid.Controls.Add(_boardB, 1, 1);
        var specTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        specTop.Controls.AddRange(new Control[] { _btnLeaveSpec, _lblSpecTime });
        _pnlSpec.Controls.Add(specGrid); _pnlSpec.Controls.Add(specTop);

        Controls.Add(_pnlLobby); Controls.Add(_pnlGame); Controls.Add(_pnlSpec);
        Controls.Add(_lstLog); Controls.Add(top);
    }

    private void WireEvents()
    {
        _btnConnect.Click += async (_, _) => await ConnectAsync();
        _btnDisconnect.Click += (_, _) => DisconnectByUser();
        _btnJoin.Click += async (_, _) => await SendSafe(MessageTypes.JoinQueue);
        _btnLeaveQueue.Click += async (_, _) => { _inQueue = false; UpdateButtons(); await SendSafe(MessageTypes.LeaveQueue); };
        _btnRefresh.Click += async (_, _) => await RefreshListsAsync();
        _btnChallenge.Click += async (_, _) =>
        {
            if (_lvPlayers.SelectedItems.Count == 0) { SetStatus("Hãy chọn một người chơi", Color.DarkOrange); return; }
            await SendSafe(MessageTypes.Challenge, new ChallengePayload { Target = _lvPlayers.SelectedItems[0].Text });
        };
        _btnWatch.Click += async (_, _) =>
        {
            if (_lvMatches.SelectedItems.Count == 0) { SetStatus("Hãy chọn một trận để xem", Color.DarkOrange); return; }
            await SendSafe(MessageTypes.SpectateMatch, new SpectatePayload { MatchId = (string)_lvMatches.SelectedItems[0].Tag! });
        };
        _btnSurrender.Click += async (_, _) =>
        {
            if (MessageBox.Show("Bạn chắc chắn muốn bỏ cuộc? Đối thủ sẽ thắng.", "Bỏ cuộc", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                await SendSafe(MessageTypes.Surrender);
        };
        _btnLeaveSpec.Click += async (_, _) => { await SendSafe(MessageTypes.LeaveSpectate); ShowView(View.Lobby); await RefreshListsAsync(); };
        _boardMe.CellEntered += async (r, c, v) => await SendSafe(MessageTypes.SubmitMove, new MovePayload { Row = r, Col = c, Value = v });

        _net.MessageReceived += m => OnUi(() => Handle(m));
        _net.Disconnected += reason => OnUi(() => OnDisconnected(reason));
        FormClosing += (_, _) => { _intentionalDisconnect = true; _net.Dispose(); };
    }

    private void OnUi(Action a)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(a); } catch (ObjectDisposedException) { }
    }

    private void SetStatus(string text, Color color) { _lblStatus.Text = text; _lblStatus.ForeColor = color; }

    private void Log(string text)
    {
        _lstLog.Items.Add($"{DateTime.Now:HH:mm:ss}  {text}");
        if (_lstLog.Items.Count > 500) _lstLog.Items.RemoveAt(0);
        _lstLog.TopIndex = _lstLog.Items.Count - 1;
    }

    private void ShowView(View v)
    {
        _pnlLobby.Visible = v == View.Lobby;
        _pnlGame.Visible = v == View.Game;
        _pnlSpec.Visible = v == View.Spectate;
        _inGame = v == View.Game;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _pnlLobby.Enabled = _loggedIn;
        _btnJoin.Enabled = !_inQueue;
        _btnLeaveQueue.Enabled = _inQueue;
        _btnConnect.Enabled = !_loggedIn && !_reconnecting && !_net.IsConnected;
        _btnDisconnect.Enabled = _net.IsConnected || _reconnecting;
        _txtHost.Enabled = _txtName.Enabled = _numPort.Enabled = _btnConnect.Enabled;
    }

    // ======================= Mạng =======================
    private async Task ConnectAsync()
    {
        string name = _txtName.Text.Trim();
        if (name.Length == 0) { SetStatus("Hãy nhập tên", Color.DarkOrange); return; }
        _host = _txtHost.Text.Trim();
        _port = (int)_numPort.Value;
        _myName = name;
        _intentionalDisconnect = false;

        _btnConnect.Enabled = false;
        SetStatus("Đang kết nối...", Color.DarkOrange);
        try
        {
            await _net.ConnectAsync(_host, _port, TimeSpan.FromSeconds(5));
            SetStatus("Đã kết nối, đang đăng nhập...", Color.DarkOrange);
            await _net.SendAsync(MessageTypes.Login, new LoginPayload { Name = _myName });
        }
        catch (Exception ex)
        {
            SetStatus("Kết nối thất bại: " + ex.Message, Color.Firebrick);
            Log("Kết nối thất bại: " + ex.Message);
        }
        UpdateButtons();
    }

    private void DisconnectByUser()
    {
        _intentionalDisconnect = true;
        _reconnecting = false;
        _net.Disconnect();
        _loggedIn = false; _inQueue = false;
        ShowView(View.Lobby);
        SetStatus("Đã ngắt kết nối", Color.Gray);
    }

    private async Task SendSafe(string type, object? payload = null)
    {
        try { await _net.SendAsync(type, payload); }
        catch (Exception ex) { Log($"Gửi {type} thất bại: {ex.Message}"); }
    }

    private async Task RefreshListsAsync()
    {
        await SendSafe(MessageTypes.ListPlayers);
        await SendSafe(MessageTypes.ListMatches);
    }

    private void OnDisconnected(string reason)
    {
        _loggedIn = false; _inQueue = false;
        Log("Mất kết nối: " + reason);
        if (_intentionalDisconnect) { UpdateButtons(); return; }
        SetStatus("Mất kết nối: " + reason, Color.Firebrick);
        UpdateButtons();
        if (!_reconnecting) _ = ReconnectLoopAsync();
    }

    /// <summary>Tự kết nối lại (tối đa ~1 phút) để quay lại trận đang tạm dừng.</summary>
    private async Task ReconnectLoopAsync()
    {
        _reconnecting = true;
        UpdateButtons();
        for (int i = 1; i <= 20 && _reconnecting && !_intentionalDisconnect && !IsDisposed; i++)
        {
            SetStatus($"Mất kết nối. Đang thử kết nối lại ({i}/20)...", Color.DarkOrange);
            try
            {
                await _net.ConnectAsync(_host, _port, TimeSpan.FromSeconds(3));
                await _net.SendAsync(MessageTypes.Login, new LoginPayload { Name = _myName });
                for (int j = 0; j < 50 && !_loggedIn && _net.IsConnected; j++) await Task.Delay(100);
                if (_loggedIn) { _reconnecting = false; UpdateButtons(); return; }
                _net.Disconnect();            // server chưa nhả tên cũ -> thử lại sau
                await Task.Delay(3000);
            }
            catch { await Task.Delay(3000); }
        }
        _reconnecting = false;
        if (!_intentionalDisconnect && !IsDisposed)
        {
            SetStatus("Không thể kết nối lại tới server", Color.Firebrick);
            ShowView(View.Lobby);
        }
        UpdateButtons();
    }

    // ======================= Xử lý message từ server =======================
    private void Handle(ResponseMessage m)
    {
        switch (m.Status)
        {
            case MessageTypes.LoginOk:
                _loggedIn = true;
                SetStatus($"Đã kết nối - {_myName}", Color.SeaGreen);
                Log(m.Message);
                ShowView(View.Lobby);   // nếu đang quay lại trận, BOARD_UPDATE sẽ chuyển sang màn chơi
                _ = RefreshListsAsync();
                break;

            case MessageTypes.Waiting:
                _inQueue = true; UpdateButtons();
                SetStatus(m.Message, Color.DarkOrange);
                break;

            case MessageTypes.MatchFound:
                _inQueue = false;
                Log(m.Message);
                break;

            case MessageTypes.GameStarted:
            case MessageTypes.BoardUpdate:
                if (JsonHelper.ReadResponseData<MatchSnapshot>(m) is { } snap) ShowGameState(snap);
                if (m.Status == MessageTypes.GameStarted) SetStatus("Trận đấu bắt đầu!", Color.SeaGreen);
                break;

            case MessageTypes.MoveResult:
                if (JsonHelper.ReadResponseData<MoveResultData>(m) is { } mr)
                {
                    _lblInfo.ForeColor = mr.ScoreDelta < 0 ? Color.Firebrick : mr.ScoreDelta > 0 ? Color.SeaGreen : Color.DarkOrange;
                    _lblInfo.Text = mr.Reason;
                    if (mr.ScoreDelta < 0) _boardMe.FlashWrong(mr.Row, mr.Col, mr.Value);
                }
                break;

            case MessageTypes.TimeUpdate:
            case MessageTypes.GamePaused:
            case MessageTypes.GameResumed:
                if (JsonHelper.ReadResponseData<TimeUpdateData>(m) is { } tu) ShowTime(tu, m);
                break;

            case MessageTypes.SpectatorUpdate:
                if (JsonHelper.ReadResponseData<MatchSnapshot>(m) is { } ss) ShowSpectate(ss);
                break;

            case MessageTypes.GameOver:
                OnGameOver(m);
                break;

            case MessageTypes.PlayerList:
                FillPlayers(JsonHelper.ReadResponseData<List<PlayerListItem>>(m) ?? new());
                break;

            case MessageTypes.MatchList:
                FillMatches(JsonHelper.ReadResponseData<List<MatchSummary>>(m) ?? new());
                break;

            case MessageTypes.ChallengeReceived:
                OnChallenge(m);
                break;

            case MessageTypes.ChallengeDeclined:
                Log(m.Message); SetStatus(m.Message, Color.DarkOrange);
                break;

            case MessageTypes.Ok:
                Log(m.Message); SetStatus(m.Message, Color.SeaGreen);
                break;

            case MessageTypes.Error:
                Log("Lỗi: " + m.Message);
                SetStatus("Lỗi: " + m.Message, Color.Firebrick);
                if (!_loggedIn && !_reconnecting) { _net.Disconnect(); UpdateButtons(); }   // đăng nhập thất bại (vd trùng tên)
                break;

            case MessageTypes.Pong:
                break;

            default:
                Log($"Message lạ: {m.Status}");
                break;
        }
    }

    private void ShowGameState(MatchSnapshot s)
    {
        if (!_pnlGame.Visible) { _lblInfo.Text = ""; _boardMe.ClearSelection(); }
        ShowView(View.Game);
        _inQueue = false;
        var me = s.Players.FirstOrDefault(p => p.Name == s.You);
        var opp = s.Players.FirstOrDefault(p => p.Name != s.You);
        _boardMe.Puzzle = s.Puzzle;
        _boardMe.Cells = me?.Board;
        _boardMe.Invalidate();
        if (me != null) _lblMe.Text = $"{me.Name} (bạn)\nĐiểm: {me.Score}   Đã điền: {me.Filled}/{me.Total}\n";
        if (opp != null) _lblOpp.Text = $"{opp.Name}{(opp.Connected ? "" : " (mất kết nối)")}\nĐiểm: {opp.Score}   Đã điền: {opp.Filled}/{opp.Total}\n";
        _lblTime.Text = Fmt(s.TimeLeft);
        if (s.State == MatchStates.Paused) { _lblInfo.ForeColor = Color.DarkOrange; _lblInfo.Text = $"Trận tạm dừng, còn {s.PauseLeft}s chờ người chơi quay lại"; }
    }

    private void ShowTime(TimeUpdateData tu, ResponseMessage m)
    {
        string t = Fmt(tu.TimeLeft);
        _lblTime.Text = t;
        _lblSpecTime.Text = "Còn lại: " + t + (tu.State == MatchStates.Paused ? "  (TẠM DỪNG)" : "");
        if (m.Status == MessageTypes.GamePaused) Log(m.Message);
        if (tu.State == MatchStates.Paused)
        {
            _lblInfo.ForeColor = Color.DarkOrange;
            _lblInfo.Text = $"{tu.PausedPlayer} mất kết nối. Tạm dừng, còn {tu.PauseLeft}s";
        }
        else if (m.Status == MessageTypes.GameResumed)
        {
            _lblInfo.ForeColor = Color.SeaGreen; _lblInfo.Text = m.Message; Log(m.Message);
        }
    }

    private void ShowSpectate(MatchSnapshot s)
    {
        ShowView(View.Spectate);
        if (s.Players.Length < 2) return;
        _boardA.Puzzle = _boardB.Puzzle = s.Puzzle;
        _boardA.Cells = s.Players[0].Board; _boardB.Cells = s.Players[1].Board;
        _boardA.Invalidate(); _boardB.Invalidate();
        _lblSpecA.Text = $"{s.Players[0].Name}   Điểm {s.Players[0].Score}  ({s.Players[0].Filled}/{s.Players[0].Total})";
        _lblSpecB.Text = $"{s.Players[1].Name}   Điểm {s.Players[1].Score}  ({s.Players[1].Filled}/{s.Players[1].Total})";
        _lblSpecTime.Text = "Còn lại: " + Fmt(s.TimeLeft);
    }

    private void OnGameOver(ResponseMessage m)
    {
        var d = JsonHelper.ReadResponseData<GameOverData>(m);
        string title = d?.Result switch
        {
            "WIN" => "Bạn THẮNG!",
            "LOSE" => "Bạn THUA",
            "DRAW" => "HÒA",
            _ => "Trận đấu kết thúc"
        };
        string detail = m.Message;
        if (d != null && d.Final.Players.Length == 2)
            detail += $"\n\n{d.Final.Players[0].Name}: {d.Final.Players[0].Score} điểm\n{d.Final.Players[1].Name}: {d.Final.Players[1].Score} điểm";
        Log($"{title} - {m.Message}");
        SetStatus(title, d?.Result == "WIN" ? Color.SeaGreen : d?.Result == "LOSE" ? Color.Firebrick : Color.DarkOrange);
        ShowView(View.Lobby);
        _ = RefreshListsAsync();
        MessageBox.Show(this, detail, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void OnChallenge(ResponseMessage m)
    {
        var d = JsonHelper.ReadResponseData<ChallengeData>(m);
        if (d == null) return;
        var res = MessageBox.Show(this, $"{d.From} muốn thách đấu bạn. Chấp nhận?", "Lời thách đấu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        await SendSafe(MessageTypes.ChallengeResponse, new ChallengeResponsePayload { Challenger = d.From, Accept = res == DialogResult.Yes });
    }

    private void FillPlayers(List<PlayerListItem> list)
    {
        _lvPlayers.BeginUpdate();
        _lvPlayers.Items.Clear();
        foreach (var p in list.Where(p => p.Name != _myName))
            _lvPlayers.Items.Add(new ListViewItem(new[] { p.Name, p.Status }));
        _lvPlayers.EndUpdate();
    }

    private void FillMatches(List<MatchSummary> list)
    {
        _lvMatches.BeginUpdate();
        _lvMatches.Items.Clear();
        foreach (var x in list)
            _lvMatches.Items.Add(new ListViewItem(new[] { x.MatchId, x.Player1, x.Player2, $"{x.Score1} - {x.Score2}", Fmt(x.TimeLeft), x.State }) { Tag = x.MatchId });
        _lvMatches.EndUpdate();
    }

    private static string Fmt(int seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"mm\:ss");
}
