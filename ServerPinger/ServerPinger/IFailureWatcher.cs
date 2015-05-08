using System;

namespace ServerPinger
{
    public interface IFailureWatcher
    {
        void RequestFailureHasOccurred(Uri failedRequestUri);
    }
}
