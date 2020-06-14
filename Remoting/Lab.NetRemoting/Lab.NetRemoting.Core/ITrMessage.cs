using System;

namespace Lab.NetRemoting.Core
{
    public interface ITrMessage
    {
        string GetName();

        DateTime GetNow();
    }
}