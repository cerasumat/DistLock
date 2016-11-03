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
		public const string DefaultRedisKeyFormat = "redlock-{0}";
		private readonly object _lockObject = new object();
		private readonly ICollection<RedisConnection> _redisConnections;
		public readonly string KeyName;




		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string LockId { get; private set; }
		public bool IsAcquired { get; private set; }
		public int ExtendCount { get; private set; }

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
