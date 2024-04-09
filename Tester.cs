using System.Text.Json;

namespace OpenBLiveAPI;

public static class Tester
{
    public static void Test()
    {
        var configFilePath = Path.GetFullPath("config.json");
        if (!File.Exists(configFilePath))
        {
            var configStr = JsonSerializer.Serialize(new { code = "", app_id = 0, accessKeyId = "", accessKeySecret = "" });
            File.WriteAllText(configFilePath, configStr);
            Console.WriteLine($"请在{configFilePath}文件内填写code,app_id,accessKeyId,accessKeySecret");
            return;
        }

        var config = File.ReadAllText(configFilePath).ToJson();
        var code = config.Get("code")?.GetString()!;
        var appId = (long)config.Get("app_id")?.GetInt64()!;
        var accessKeyId = config.Get("accessKeyId")?.GetString()!;
        var accessKeySecret = config.Get("accessKeySecret")?.GetString()!;
        var client = new OpenBLiveClient(code, appId, accessKeyId, accessKeySecret);
        client.OpSendSmsReply += OpSendSmsReplyEvent;
        client.OpAuthReply += OpAuthReplyEvent;
        client.OpHeartbeatReply += OpHeartbeatReplyEvent;
        var clientStartResult = client.Start();
        Console.WriteLine(clientStartResult);
        if (clientStartResult.Item1)
        {
            Console.ReadLine();
            client.Stop();
        }

        client.WaitProjectStop().Wait();
    }

    private static void OpHeartbeatReplyEvent((int heartbeatReply, byte[] rawData) args)
    {
        Console.WriteLine($"心跳回复:{args}");
    }

    private static void OpAuthReplyEvent((JsonElement authReply, byte[] rawData) args)
    {
        Console.WriteLine($"认证回复:{args}");
    }

    private static void OpSendSmsReplyEvent((string cmd, string hitCmd, JsonElement jsonRawData, byte[] rawData) args)
    {
        Console.WriteLine($"弹幕消息:{args.jsonRawData}");
    }
}