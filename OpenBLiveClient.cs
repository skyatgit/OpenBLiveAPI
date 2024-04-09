using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenBLiveAPI;

public class OpenBLiveClient(string code, long appId, string accessKeyId, string accessKeySecret) : OpenBLiveWebSocketClient
{
    private const string ProjectBaseUrl = "https://live-open.biliapi.com";

    public (bool, string) Start()
    {
        if (CancelToken is not null) return (true, "项目正在运行");
        try
        {
            var projectInfo = ProjectStart();
            if (projectInfo.Get("code")?.GetInt32() != 0) return (false, $"项目启动失败:{projectInfo}");
            var gameId = projectInfo.Get("data")?.Get("game_info")?.Get("game_id")?.GetString()!;
            var authBody = projectInfo.Get("data")?.Get("websocket_info")?.Get("auth_body")?.GetString()!;
            Console.WriteLine($"gameId:{gameId}");
            Console.WriteLine($"authBody:{authBody}");
            var websocketStartResult = WebSocketStart(authBody);
            if (!websocketStartResult.Item1) return websocketStartResult;
            ProjectStartHeartbeat(gameId);
            return (true, "项目启动成功");
        }
        catch (Exception e)
        {
            Stop();
            return (false, $"项目启动失败:{e}");
        }
    }

    public void Stop()
    {
        WebSocketStop();
    }

    public async Task WaitProjectStop()
    {
        while (CancelToken?.IsCancellationRequested == false) await Task.Delay(1000, CancelToken.Token);
    }

    private JsonElement ProjectStart()
    {
        const string projectStartUrl = $"{ProjectBaseUrl}/v2/app/start";
        var data = JsonSerializer.Serialize(new { code, app_id = appId });
        return OpenBLivePost(projectStartUrl, data);
    }

    private JsonElement ProjectEnd(string gameId)
    {
        const string projectEndUrl = $"{ProjectBaseUrl}/v2/app/end";
        var data = JsonSerializer.Serialize(new { app_id = appId, game_id = gameId });
        return OpenBLivePost(projectEndUrl, data);
    }

    private async void ProjectStartHeartbeat(string gameId)
    {
        const string projectHeartbeatUrl = $"{ProjectBaseUrl}/v2/app/heartbeat";
        var data = JsonSerializer.Serialize(new { game_id = gameId });
        try
        {
            while (CancelToken?.IsCancellationRequested == false)
            {
                var result = OpenBLivePost(projectHeartbeatUrl, data);
                Console.WriteLine(result);
                if (result.Get("code")?.GetInt32() == 0)
                {
                    await Task.Delay(19000, CancelToken.Token);
                }
                else
                {
                    Console.WriteLine(ProjectEnd(gameId));
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"项目心跳异常:{e.Message}");
        }
        finally
        {
            Stop();
            ProjectEnd(gameId);
        }
    }

    private JsonElement OpenBLivePost(string url, string data)
    {
        var headers = new Dictionary<string, string>
        {
            { "x-bili-accesskeyid", accessKeyId },
            { "x-bili-content-md5", data.ToMd5() },
            { "x-bili-signature-method", "HMAC-SHA256" },
            { "x-bili-signature-nonce", Guid.NewGuid().ToString() },
            { "x-bili-signature-version", "1.0" },
            { "x-bili-timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        };
        headers["Authorization"] = headers.Aggregate("", (current, pair) => current + $"{(current == "" ? "" : "\n")}{pair.Key}:{pair.Value}").ToHmacSha256(accessKeySecret);
        headers["Accept"] = "application/json";
        using var client = new HttpClient();
        foreach (var (k, v) in headers) client.DefaultRequestHeaders.Add(k, v);
        using var response = client.PostAsync(url, new StringContent(data, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))).Result;
        return response.Content.ReadAsStringAsync().Result.ToJson();
    }
}