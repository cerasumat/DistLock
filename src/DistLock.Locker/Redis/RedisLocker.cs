using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistLock.Entity;

namespace DistLock.Locker.Redis
{
	public class RedisLocker : ILocker
	{
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public bool IsAcquired { get; private set; }
		public bool Lock(LockEntity locker)
		{
			throw new NotImplementedException();
		}

		public bool Unlock(LockEntity locker)
		{
			throw new NotImplementedException();
		}
	}
}
