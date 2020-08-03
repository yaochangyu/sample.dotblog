using System.Collections.Generic;
using System.Threading;

namespace Lab.DyanmiceAccessor
{
    public abstract class FastReflectionCache<TKey, TValue> : IFastReflectionCache<TKey, TValue>
    {
        private Dictionary<TKey, TValue> m_cache = new Dictionary<TKey, TValue>();
        private ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

        public TValue Get(TKey key)
        {
            TValue value = default(TValue);

            this.m_rwLock.EnterReadLock();
            bool cacheHit = this.m_cache.TryGetValue(key, out value);
            this.m_rwLock.ExitReadLock();

            if (cacheHit) return value;

            this.m_rwLock.EnterWriteLock();
            if (!this.m_cache.TryGetValue(key, out value))
            {
                try
                {
                    value = this.Create(key);
                    this.m_cache[key] = value;
                }
                finally
                {
                    this.m_rwLock.ExitWriteLock();
                }
            }

            return value;
        }

        protected abstract TValue Create(TKey key);
    }
}
