using System;

namespace DistLock.Entity
{
	[Serializable]
	public class LockEntity
	{
		public string LockName { get; private set; }
		public string LockValue { get; private set; }
		public long TtlInMillis { get; private set; }

		public LockEntity(string lockName, string lockValue, long ttlInMillis)
		{
			LockName = lockName;
			LockValue = lockValue;
			TtlInMillis = ttlInMillis;
		}
	}
}
