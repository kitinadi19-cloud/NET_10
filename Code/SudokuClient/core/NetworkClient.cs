
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SudokuShared.Messages;

namespace SudokuClient.Core;

public class NetworkClient : IDisposable
{
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private CancellationTokenSource? _cancellation;

    public event Action<ResponseMessage>? MessageReceived;
    public event Action<string>? ConnectionClosed;

    public bool IsConnected => _client?.Connected == true;

    public async Task ConnectAsync(string ip, int port)
    {
        if (_client != null)
            Disconnect();

        _cancellation = new CancellationTokenSource();
        _client = new TcpClient();

        await _client.ConnectAsync(ip, port, _cancellation.Token);

        NetworkStream stream = _client.GetStream();

        _reader = new StreamReader(
            stream,
            new UTF8Encoding(false));

        _writer = new StreamWriter(
            stream,
            new UTF8Encoding(false))
        {
            AutoFlush = true
        };

        _ = ReceiveMessagesAsync(_cancellation.Token);
    }

    public async Task SendAsync(string type, object? payload = null)
    {
        if (_writer == null || !IsConnected)
            throw new InvalidOperationException("Client chưa kết nối Server.");

        string payloadJson = payload == null
            ? ""
            : JsonSerializer.Serialize(payload);

        var request = new RequestMessage
        {
            Type = type,
            Payload = payloadJson
        };

        string json = JsonSerializer.Serialize(request);

        await _writeLock.WaitAsync();

        try
        {
            if (_writer == null)
                throw new InvalidOperationException("Kết nối đã đóng.");

            await _writer.WriteLineAsync(json);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _reader != null)
            {
                string? line = await _reader.ReadLineAsync(
                    cancellationToken);

                if (line == null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var response = JsonSerializer.Deserialize<ResponseMessage>(line);

                if (response != null)
                    MessageReceived?.Invoke(response);
            }
        }
        catch (OperationCanceledException)
        {
            // Client chủ động ngắt kết nối.
        }
        catch (Exception ex)
        {
            ConnectionClosed?.Invoke(ex.Message);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                ConnectionClosed?.Invoke("Server đã đóng kết nối.");
        }
    }

    public void Disconnect()
    {
        _cancellation?.Cancel();

        _reader?.Dispose();
        _writer?.Dispose();
        _client?.Close();

        _reader = null;
        _writer = null;
        _client = null;

        _cancellation?.Dispose();
        _cancellation = null;
    }

    public void Dispose()
    {
        Disconnect();
        _writeLock.Dispose();
    }
}
