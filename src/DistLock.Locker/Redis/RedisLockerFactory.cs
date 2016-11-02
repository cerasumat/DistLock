using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistLock.Locker.Redis
{
	public class RedisLockerFactory
	{
		public ILocker CreateLocker(string lockName, long ttlInMills)
		{
			
		}
	}
}
