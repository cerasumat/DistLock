using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistLock.Entity;

namespace DistLock.Locker.Etcd
{
	public class EtcdLocker : ILocker
	{
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string LockId { get; private set; }
		public bool IsAcquired { get; private set; }
		public int ExtendCount { get; private set; }
	}
}
