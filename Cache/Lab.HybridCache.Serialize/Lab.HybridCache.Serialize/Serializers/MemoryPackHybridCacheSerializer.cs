using System.Buffers;
using MemoryPack;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HybridCache.Serialize.Serializers;

/// <summary>
/// 以 MemoryPack 實作 HybridCache 的 L2 序列化器
/// </summary>
public sealed class MemoryPackHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    public T Deserialize(ReadOnlySequence<byte> source)
        => MemoryPackSerializer.Deserialize<T>(source)
           ?? throw new InvalidDataException($"MemoryPack 無法反序列化 {typeof(T).Name}");

    public void Serialize(T value, IBufferWriter<byte> target)
        => MemoryPackSerializer.Serialize(target, value);
}
