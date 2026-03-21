using System.Buffers;
using MessagePack;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HybridCache.Serialize.Serializers;

/// <summary>
/// 以 MessagePack 實作 HybridCache 的 L2 序列化器
/// </summary>
public sealed class MessagePackHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    private readonly MessagePackSerializerOptions _options;

    public MessagePackHybridCacheSerializer(MessagePackSerializerOptions? options = null)
        => _options = options ?? MessagePackSerializerOptions.Standard;

    public T Deserialize(ReadOnlySequence<byte> source)
        => MessagePackSerializer.Deserialize<T>(source, _options);

    public void Serialize(T value, IBufferWriter<byte> target)
        => MessagePackSerializer.Serialize(target, value, _options);
}
