using System;
using DistLock.Entity;

namespace DistLock.Locker
{
    public interface ILocker : IDisposable
    {
		bool IsAcquired { get; }
		bool Lock(LockEntity locker);
		bool Unlock(LockEntity locker);
    }
}
