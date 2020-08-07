using System.Collections.Concurrent;

namespace Lab.DynamicAccessor
{
    public interface IAccessorFactory<TKey, TValue>
    {
        TValue GetOrCreate(TKey key);
    }

    public abstract class AccessorFactoryBase<TKey, TValue> : IAccessorFactory<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _cache;

        public AccessorFactoryBase()
        {
            if (this._cache == null)
            {
                this._cache = new ConcurrentDictionary<TKey, TValue>();
            }
        }

        public TValue GetOrCreate(TKey key)
        {
            if (this._cache.TryGetValue(key, out var result) == false)
            {
                result = this.Create(key);
                this._cache.TryAdd(key, result);
            }

            return result;
        }

        protected abstract TValue Create(TKey key);
    }
}