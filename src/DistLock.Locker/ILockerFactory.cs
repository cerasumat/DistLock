using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLock.Locker
{
	public interface ILockerFactory	: IDisposable
	{
		ILocker Create(string lockName, long ttlInMillis);

		ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis);

		ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis,
			CancellationToken cancellationToken);

		Task<ILocker> CreateAsync(string lockName, long ttlInMillis);
		Task<ILocker> CreateAsync(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis);

		Task<ILocker> CreateAsync(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis,
			CancellationToken cancellationToken);
	}
}
