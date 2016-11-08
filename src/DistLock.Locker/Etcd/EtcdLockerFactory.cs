using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace DistLock.Locker.Etcd
{
	public class EtcdLockerFactory : ILockerFactory
	{
		private readonly IList<EtcdConnection> _etcdConnections;

		public EtcdLockerFactory(IEnumerable<Uri> etcdUrls) : this(etcdUrls.ToArray()) { }

		public EtcdLockerFactory(params Uri[] etcdUrls)
		{
			_etcdConnections = CreateEtcdConnections(etcdUrls);
		}

		public void Dispose()
		{
			foreach (var etcdConnection in _etcdConnections)
			{
				etcdConnection.EtcdClient = null;
			}
		}

		public ILocker Create(string lockName, long ttlInMillis)
		{
			return EtcdLocker.Create(_etcdConnections, lockName, TimeSpan.FromMilliseconds(ttlInMillis));
		}

		public ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis)
		{
			return EtcdLocker.Create(_etcdConnections, lockName, TimeSpan.FromMilliseconds(ttlInMillis),TimeSpan.FromMilliseconds(waitInMillis),TimeSpan.FromMilliseconds(retryInMillis));
		}

		public ILocker Create(string lockName, long ttlInMillis, long waitInMillis, long retryInMillis,
			CancellationToken cancellationToken)
		{
			return EtcdLocker.Create(_etcdConnections, lockName, TimeSpan.FromMilliseconds(ttlInMillis),
				TimeSpan.FromMilliseconds(waitInMillis), TimeSpan.FromMilliseconds(retryInMillis), cancellationToken);
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

		private static IList<EtcdConnection> CreateEtcdConnections(ICollection<Uri> etcdUrls)
		{
			if (!etcdUrls.Any())
			{
				throw new ArgumentException("No etcd endpoints specified.");
			}
			var connections = new List<EtcdConnection>(etcdUrls.Count);
			foreach (var etcdUrl in etcdUrls)
			{
				connections.Add(new EtcdConnection
				{
					EtcdClient = new RestClient(etcdUrl),
					EtcdKeyFormat = EtcdLocker.DefaultEtcdKeyFormat
				});
			}
			return connections;
		}
	}
}
