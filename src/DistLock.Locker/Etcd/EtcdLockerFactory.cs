using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistLock.Locker.Etcd
{
	public class EtcdLockerFactory : ILockerFactory
	{
		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public ILocker Create(string lockName, long ttlInMillis)
		{
			throw new NotImplementedException();
		}

		public ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis)
		{
			throw new NotImplementedException();
		}

		public ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<ILocker> CreateAsync(string lockName, long ttlInMillis)
		{
			throw new NotImplementedException();
		}

		public Task<ILocker> CreateAsync(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis)
		{
			throw new NotImplementedException();
		}

		public Task<ILocker> CreateAsync(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
