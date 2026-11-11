using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuClient;

public sealed partial class SpectatorViewForm
{
    private async Task ConnectAsync()
    {
        if (_net != null || _pending != Request.None)
            return;

        string host = _txtHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            SetStatus("Hãy nhập địa chỉ server.", true);
            return;
        }

        string viewerName = "Viewer_" +
                            Guid.NewGuid().ToString("N")[..10];

        var net = new NetworkClient();
        _net = net;
        _lblViewer.Text = "Khán giả: " + viewerName;

        net.MessageReceived += message =>
            PostToUi(net, () => HandleMessage(message));

        net.Disconnected += reason =>
            PostToUi(net, () => Disconnect("Mất kết nối: " + reason));

        BeginRequest(Request.Login);
        SetStatus("Đang kết nối...");

        try
        {
            await net.ConnectAsync(
                host, (int)_numPort.Value, TimeSpan.FromSeconds(5));

            if (_closing || !ReferenceEquals(_net, net))
            {
                net.Dispose();
                return;
            }

            SetStatus("Đang đăng nhập...");
            await net.SendAsync(
                MessageTypes.Login,
                new LoginPayload { Name = viewerName });
        }
        catch (Exception ex)
        {
            if (!_closing && ReferenceEquals(_net, net))
                Disconnect("Kết nối thất bại: " + ex.Message);

            net.Dispose();
        }
    }

    private void PostToUi(NetworkClient source, Action action)
    {
        if (_closing || IsDisposed || !IsHandleCreated)
            return;

        try
        {
            BeginInvoke(new Action(() =>
            {
                if (!_closing && !IsDisposed &&
                    ReferenceEquals(_net, source))
                {
                    action();
                }
            }));
        }
        catch (InvalidOperationException)
        {
            // Cửa sổ đã đóng hoặc bị hủy
        }
    }

    private void Disconnect(string reason, bool error = true)
    {
        NetworkClient? old = _net;
        _net = null;
        old?.Dispose();

        _loggedIn = false;
        _pending = Request.None;
        _requestedId = _watchingId = null;
        _matches.Items.Clear();

        if (_lastSnapshot != null)
        {
            _lblMatch.Text =
                $"Trận {_lastSnapshot.MatchId} — dữ liệu cuối đã nhận";
            _lblTime.Text = "Đã ngắt kết nối — ngừng cập nhật";
        }

        SetStatus(reason, error);
        Log(reason);
        UpdateButtons();
    }

    private async Task SendAsync(Request request, object? payload = null)
    {
        if (!_loggedIn || _net == null || _pending != Request.None)
            return;

        // Chỉ cho phép yêu cầu xem
        string type = request switch
        {
            Request.List => MessageTypes.ListMatches,
            Request.Watch => MessageTypes.SpectateMatch,
            Request.Leave => MessageTypes.LeaveSpectate,
            _ => throw new InvalidOperationException("Yêu cầu không hợp lệ.")
        };

        NetworkClient net = _net;
        BeginRequest(request);

        try
        {
            await net.SendAsync(type, payload);
        }
        catch (Exception ex)
        {
            if (!_closing && ReferenceEquals(_net, net))
                Disconnect("Gửi yêu cầu thất bại: " + ex.Message);
        }
    }

    private void HandleMessage(ResponseMessage message)
    {
        try
        {
            switch (message.Status)
            {
                case MessageTypes.LoginOk:
                    if (_pending != Request.Login)
                        return;

                    _loggedIn = true;
                    CompleteRequest();
                    Log("Đăng nhập khán giả thành công.");
                    SetStatus("Đã kết nối.");
                    _ = SendAsync(Request.List);
                    break;

                case MessageTypes.MatchList:
                    if (_pending != Request.List)
                        return;

                    var matches =
                        JsonHelper.ReadResponseData<List<MatchSummary>>(
                            message) ?? new List<MatchSummary>();

                    FillMatches(matches);
                    CompleteRequest();
                    SetStatus($"Có {matches.Count} trận. Chọn trận để xem.");
                    break;

                case MessageTypes.SpectatorUpdate:
                    ReceiveSnapshot(message);
                    break;

                case MessageTypes.TimeUpdate:
                case MessageTypes.GamePaused:
                case MessageTypes.GameResumed:
                    ReceiveTime(message);
                    break;

                case MessageTypes.GameOver:
                    ReceiveGameOver(message);
                    break;

                case MessageTypes.Ok:
                    if (_pending == Request.Leave)
                    {
                        _watchingId = _requestedId = null;
                        ClearMatch();
                        CompleteRequest();
                        Log("Đã rời phòng xem.");
                        _ = SendAsync(Request.List);
                    }
                    break;

                case MessageTypes.Error:
                    ReceiveError(message.Message);
                    break;

                case MessageTypes.ChallengeReceived:
                    Log("Bỏ qua lời mời chơi: đây là cửa sổ khán giả.");
                    break;

                case MessageTypes.GameStarted:
                case MessageTypes.BoardUpdate:
                case MessageTypes.MatchFound:
                    Disconnect(
                        "Nhận nhầm trạng thái người chơi. Hãy kết nối lại.");
                    break;

                case MessageTypes.Pong:
                    break;
            }
        }
        catch (JsonException)
        {
            Disconnect("Server gửi dữ liệu JSON không hợp lệ.");
        }
    }

    private void ReceiveError(string text)
    {
        if (_pending == Request.Login || _pending == Request.Leave)
        {
            Disconnect("Server: " + text);
            return;
        }

        if (_pending == Request.Watch)
            _requestedId = null;

        CompleteRequest();
        SetStatus("Lỗi: " + text, true);
        Log("Server: " + text);
    }
}
