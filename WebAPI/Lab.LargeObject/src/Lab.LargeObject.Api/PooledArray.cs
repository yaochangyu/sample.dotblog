using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lab.LargeObject.Api;

/// <summary>
/// 包住一塊從 <see cref="ArrayPool{T}"/> 租來的陣列。
/// Rent 出來的陣列長度不等於實際資料長度，必須額外記錄 <see cref="Length"/>；
/// Dispose 時歸還租用陣列，歸還後不得再存取 <see cref="Span"/>。
/// </summary>
public readonly struct PooledArray<T> : IDisposable
{
    private readonly T[] _rented;

    public PooledArray(T[] rented, int length)
    {
        _rented = rented;
        Length = length;
    }

    public int Length { get; }

    public ReadOnlySpan<T> Span => _rented.AsSpan(0, Length);

    public void Dispose()
    {
        if (_rented is null)
        {
            return;
        }

        ArrayPool<T>.Shared.Return(_rented, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }
}
