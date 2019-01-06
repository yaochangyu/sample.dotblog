using System;
using JWT;

namespace Server
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        private DateTime? _now;

        public DateTime? Now
        {
            get
            {
                if (this._now.HasValue==false)
                {
                    return DateTime.UtcNow;
                }
                return this._now;
            }
            set { this._now = value; }
        }

        public DateTimeOffset GetNow()
        {
            return this.Now.Value;
        }
    }
}