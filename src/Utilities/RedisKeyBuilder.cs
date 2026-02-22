using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;

namespace Synthient.Edge.Utilities;

internal static class RedisKeyBuilder
{
    // [IP bytes (4B IPv4 | 16B IPv6)][Bucket ID (4B big-endian int32)]
    public static RedisKey BucketKey(IPAddress ip, int bucketId)
    {
        ip = Normalize(ip);
        var ipSize = ip.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        var key = new byte[ipSize + sizeof(int)];

        ip.TryWriteBytes(key, out _);
        BinaryPrimitives.WriteInt32BigEndian(key.AsSpan(ipSize), bucketId);

        return key;
    }

    // [IP bytes (4B IPv4 | 16B IPv6)]
    public static RedisKey IpKey(IPAddress ip)
    {
        ip = Normalize(ip);
        var ipSize = ip.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        var key = new byte[ipSize];

        ip.TryWriteBytes(key, out _);
        return key;
    }

    private static IPAddress Normalize(IPAddress ip) => ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
}