﻿namespace OpenBLiveAPI;

/// <summary>
///     未知的ServerOperation异常
/// </summary>
public class UnknownServerOperationException : Exception
{
    /// <inheritdoc cref="UnknownServerOperationException" />
    public UnknownServerOperationException(object value) : base($"未知的ServerOperation:{value}")
    {
    }
}

/// <summary>
///     网络异常
/// </summary>
public class NetworkException : Exception
{
    /// <inheritdoc cref="NetworkException" />
    public NetworkException() : base("网络异常")
    {
    }
}

/// <summary>
///     主机用户名编码异常
/// </summary>
public class DomainNameEncodingException : Exception
{
    /// <inheritdoc cref="DomainNameEncodingException" />
    public DomainNameEncodingException() : base("主机用户名编码异常,请检查主机用户名中是否有非ASCII字符")
    {
    }
}

/// <summary>
///     字节集长度错误
/// </summary>
public class InvalidBytesLengthException : Exception
{
    /// <inheritdoc cref="InvalidBytesLengthException" />
    public InvalidBytesLengthException() : base("字节集长度错误")
    {
    }
}

/// <summary>
///     WebSocket主动关闭
/// </summary>
public class WebSocketCloseException : Exception
{
    /// <inheritdoc cref="WebSocketCloseException" />
    public WebSocketCloseException() : base("WebSocket主动关闭")
    {
    }
}

/// <summary>
///     WebSocket异常关闭
/// </summary>
public class WebSocketErrorException : Exception
{
    /// <inheritdoc cref="WebSocketErrorException" />
    public WebSocketErrorException() : base("WebSocket异常关闭")
    {
    }
}