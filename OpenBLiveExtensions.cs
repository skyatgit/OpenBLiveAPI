using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OpenBLiveAPI;

public static class OpenBLiveExtensions
{
    public static byte[] ToUtf8Bytes(this string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }

    public static string ToMd5(this string input)
    {
        return BitConverter.ToString(MD5.HashData(input.ToUtf8Bytes())).Replace("-", "").ToLower();
    }

    public static string ToHmacSha256(this string input, string key)
    {
        return BitConverter.ToString(HMACSHA256.HashData(key.ToUtf8Bytes(), input.ToUtf8Bytes())).Replace("-", "").ToLower();
    }

    public static JsonElement ToJson(this string input)
    {
        return JsonDocument.Parse(input).RootElement;
    }

    public static JsonElement ToJson(this byte[] input)
    {
        return JsonDocument.Parse(input).RootElement;
    }

    public static int ToInt(this byte[] input)
    {
        var temp = BitConverter.IsLittleEndian ? input.Reverse().ToArray() : input;
        return temp.Length switch
        {
            2 => BitConverter.ToInt16(temp, 0),
            4 => BitConverter.ToInt32(temp, 0),
            _ => throw new Exception("不支持的字节长度")
        };
    }

    public static JsonElement? Get(this JsonElement input, string key)
    {
        return input.TryGetProperty(key, out var result) ? result : null;
    }
}