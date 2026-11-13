using System.Text.Json;
using SudokuShared.Messages;

namespace SudokuShared;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    // Chuyển object thành JSON.
    public static string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    // Chuyển JSON thành object.
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    // Tạo Request gửi từ Client lên Server.
    public static RequestMessage CreateRequest<T>(
        string type,
        T payload)
    {
        return new RequestMessage
        {
            Type = type,
            Payload = Serialize(payload)
        };
    }

    // Đọc Payload trong Request.
    public static T? ReadPayload<T>(RequestMessage request)
    {
        return Deserialize<T>(request.Payload);
    }

    // Tạo Response gửi từ Server về Client.
    public static ResponseMessage CreateResponse<T>(
        string status,
        string message,
        T? data = default)
    {
        return new ResponseMessage
        {
            Status = status,
            Message = message,
            Data = data == null
                ? null
                : JsonSerializer.SerializeToElement(data, Options)
        };
    }

    // Đọc Data trong Response.
    public static T? ReadResponseData<T>(ResponseMessage response)
    {
        if (response.Data == null)
            return default;

        return response.Data.Value.Deserialize<T>(Options);
    }
}