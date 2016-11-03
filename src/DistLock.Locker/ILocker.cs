using System;
using DistLock.Entity;

namespace DistLock.Locker
{
    public interface ILocker : IDisposable
    {
		string LockId { get; }
		bool IsAcquired { get; }
		int ExtendCount { get; }
		bool Lock(LockEntity locker);
		bool Unlock(LockEntity locker);
    }
}
