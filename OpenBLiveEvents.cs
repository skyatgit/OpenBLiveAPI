using System.Text.Json;

namespace OpenBLiveAPI;

/// <summary>
///     OpenBLiveAPI的各种事件
/// </summary>
public abstract class OpenBLiveEvents
{
    /// <inheritdoc />
    public delegate void OpenBLiveEventHandler<in TEventArgs>(TEventArgs args);

    /// <summary>
    ///     服务器回复的认证消息
    /// </summary>
    public event OpenBLiveEventHandler<(JsonElement authReply, byte[] rawData)>? OpAuthReply;

    /// <inheritdoc cref="OpAuthReply" />
    protected void OnOpAuthReply(JsonElement authReply, byte[] rawData)
    {
        OpAuthReply?.Invoke((authReply, rawData));
    }

    /// <summary>
    ///     服务器回复的心跳消息
    /// </summary>
    public event OpenBLiveEventHandler<(int heartbeatReply, byte[] rawData)>? OpHeartbeatReply;

    /// <inheritdoc cref="OpHeartbeatReply" />
    protected void OnOpHeartbeatReply(int heartbeatReply, byte[] rawData)
    {
        OpHeartbeatReply?.Invoke((heartbeatReply, rawData));
    }

    /// <summary>
    ///     服务器发送的SMS消息
    /// </summary>
    public event OpenBLiveEventHandler<(string cmd, string hitCmd, JsonElement jsonRawData, byte[] rawData)>? OpSendSmsReply;

    /// <inheritdoc cref="OpSendSmsReply" />
    protected void OnOpSendSmsReply(JsonElement jsonRawData, byte[] rawData)
    {
        if (OpSendSmsReply is null) return;
        var hit = false;
        var waitInvokeList = OpSendSmsReply.GetInvocationList().ToList();
        var cmd = jsonRawData.Get("cmd")?.GetString()!;
        foreach (var invocation in OpSendSmsReply.GetInvocationList())
            if (invocation.Method.GetCustomAttributes(typeof(TargetCmdAttribute), false).FirstOrDefault() is not TargetCmdAttribute targetCmdAttribute)
            {
                invocation.DynamicInvoke((cmd, "ALL", jsonRawData, rawData));
                waitInvokeList.Remove(invocation);
            }
            else if (targetCmdAttribute.HasCmd(cmd))
            {
                invocation.DynamicInvoke((cmd, cmd, jsonRawData, rawData));
                waitInvokeList.Remove(invocation);
                hit = true;
            }
            else if (targetCmdAttribute.HasCmd("ALL"))
            {
                invocation.DynamicInvoke((cmd, "ALL", jsonRawData, rawData));
                waitInvokeList.Remove(invocation);
            }
            else if (!targetCmdAttribute.HasCmd("OTHERS"))
            {
                waitInvokeList.Remove(invocation);
            }

        if (hit) return;
        foreach (var invocation in waitInvokeList) invocation.DynamicInvoke(this, (cmd, "OTHERS", jsonRawData, rawData));
    }
}