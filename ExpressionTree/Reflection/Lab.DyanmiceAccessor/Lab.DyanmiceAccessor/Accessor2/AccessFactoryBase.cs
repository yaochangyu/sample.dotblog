using System.Collections.Concurrent;

namespace Lab.DynamicAccessor.Accessor2
{
    public interface IAccessorCache<TKey, TValue>
    {
        TValue Get(TKey key);
    }

    public abstract class AccessorFactoryBase<TKey, TValue> : IAccessorCache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _cache;

        public AccessorFactoryBase()
        {
            if (this._cache == null)
            {
                this._cache = new ConcurrentDictionary<TKey, TValue>();
            }
        }

        public TValue Get(TKey key)
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