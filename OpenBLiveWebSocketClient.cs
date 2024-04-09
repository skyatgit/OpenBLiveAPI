using System.Net.WebSockets;

namespace OpenBLiveAPI;

public class OpenBLiveWebSocketClient : OpenBLiveEvents
{
    private const string WsHost = "wss://broadcastlv.chat.bilibili.com:443/sub";
    private ClientWebSocket? _clientWebSocket;
    protected CancellationTokenSource? CancelToken;

    protected (bool, string) WebSocketStart(string authBody)
    {
        if (CancelToken is not null) return (true, "WebSocket客户端正在运行");
        try
        {
            CancelToken = new CancellationTokenSource();
            _clientWebSocket = new ClientWebSocket();
            var authPacket = CreateWsPacket(ClientOperation.OpAuth, authBody.ToUtf8Bytes());
            _clientWebSocket.ConnectAsync(new Uri(WsHost), CancelToken.Token).Wait();
            _clientWebSocket.SendAsync(authPacket, WebSocketMessageType.Binary, true, CancelToken.Token).Wait();
            WebSocketStartReceiveMessage();
            WebSocketStartHeartbeat();
            return (true, "WebSocket客户端启动成功");
        }
        catch (Exception e)
        {
            WebSocketStop();
            return (false, $"WebSocket客户端启动失败:{e}");
        }
    }

    protected void WebSocketStop()
    {
        CancelToken?.Cancel();
        _clientWebSocket?.Dispose();
        CancelToken = null;
        _clientWebSocket = null;
    }

    private async void WebSocketStartHeartbeat()
    {
        try
        {
            var heartbeatPacket = CreateWsPacket(ClientOperation.OpHeartbeat, Array.Empty<byte>());
            while (_clientWebSocket?.State == WebSocketState.Open && CancelToken?.IsCancellationRequested == false)
            {
                _clientWebSocket?.SendAsync(heartbeatPacket, WebSocketMessageType.Binary, true, CancelToken.Token);
                await Task.Delay(19000, CancelToken.Token);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"WebSocket心跳出现异常:{e.Message}");
        }
        finally
        {
            WebSocketStop();
        }
    }

    private async void WebSocketStartReceiveMessage()
    {
        try
        {
            var buffer = new List<byte>();
            while (_clientWebSocket?.State == WebSocketState.Open && CancelToken?.IsCancellationRequested == false)
            {
                var tempBuffer = new byte[1024];
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(tempBuffer), CancelToken.Token);
                buffer.AddRange(new ArraySegment<byte>(tempBuffer, 0, result.Count));
                if (!result.EndOfMessage) continue;
                DecodePacket(buffer.ToArray());
                buffer.Clear();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"WebSocket接收消息出现异常:{e.Message}");
        }
        finally
        {
            WebSocketStop();
        }
    }

    private void DecodePacket(byte[] packetData)
    {
        var header = new ArraySegment<byte>(packetData, 0, 16).ToArray();
        var body = new ArraySegment<byte>(packetData, 16, packetData.Length - 16).ToArray();
        var operation = (ServerOperation)new ArraySegment<byte>(header, 8, 4).ToArray().ToInt();
        switch (operation)
        {
            case ServerOperation.OpAuthReply:
                OnOpAuthReply(body.ToJson(), body);
                break;
            case ServerOperation.OpHeartbeatReply:
                OnOpHeartbeatReply(body.ToInt(), body);
                break;
            case ServerOperation.OpSendSmsReply:
                OnOpSendSmsReply(body.ToJson(), body);
                break;
            default:
                throw new UnknownServerOperationException(operation);
        }
    }

    private static ArraySegment<byte> CreateWsPacket(ClientOperation operation, byte[] body)
    {
        var packetLength = 16 + body.Length;
        var result = new byte[packetLength];
        Buffer.BlockCopy(ToBigEndianBytes(packetLength), 0, result, 0, 4);
        Buffer.BlockCopy(ToBigEndianBytes((short)16), 0, result, 4, 2);
        Buffer.BlockCopy(ToBigEndianBytes((short)0), 0, result, 6, 2);
        Buffer.BlockCopy(ToBigEndianBytes((int)operation), 0, result, 8, 4);
        Buffer.BlockCopy(ToBigEndianBytes(0), 0, result, 12, 4);
        Buffer.BlockCopy(body, 0, result, 16, body.Length);
        return new ArraySegment<byte>(result);
    }

    private static byte[] ToBigEndianBytes(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] ToBigEndianBytes(short value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    private enum ClientOperation
    {
        OpHeartbeat = 2,
        OpAuth = 7
    }

    private enum ServerOperation
    {
        OpHeartbeatReply = 3,
        OpSendSmsReply = 5,
        OpAuthReply = 8
    }
}