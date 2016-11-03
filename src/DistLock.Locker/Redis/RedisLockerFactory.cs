using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DistLock.Locker.Redis
{
	public class RedisLockerFactory : ILockerFactory
	{
		private const int DefaultConnectionTimeout = 100;
		private const int DefaultRedisDatabase = 0;
		private const int DefaultConfigCheckSeconds = 10;
		private readonly IList<RedisConnection> _redisConnections;

		public RedisLockerFactory(IEnumerable<EndPoint> redisEndPoints):this(redisEndPoints.ToArray()){}

		public RedisLockerFactory(IEnumerable<RedisLockEndPoint> redisEndPoints):this(redisEndPoints.ToArray()){}

		public RedisLockerFactory(params EndPoint[] redisEndPoints)
		{
			var endPoints = redisEndPoints.Select(ep => new RedisLockEndPoint
			{
				EndPoint = ep
			});
			_redisConnections = CreateRedisConnections(endPoints.ToArray());
		}

		public RedisLockerFactory(params RedisLockEndPoint[] redisEndPoints)
		{
			_redisConnections = CreateRedisConnections(redisEndPoints.ToArray());
		}

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

		private static IList<RedisConnection> CreateRedisConnections(ICollection<RedisLockEndPoint> redisEndPoints)
		{
			if (!redisEndPoints.Any())
			{
				throw new ArgumentException("No redis endpoints specified.");
			}
			var connections = new List<RedisConnection>(redisEndPoints.Count);
			foreach (var endPoint in redisEndPoints)
			{
				var configuration = new ConfigurationOptions
				{
					AbortOnConnectFail = false,
					ConnectTimeout = endPoint.ConnectionTimeout ?? DefaultConnectionTimeout,
					Ssl = endPoint.Ssl,
					Password = endPoint.Password,
					ConfigCheckSeconds = endPoint.ConfigCheckSeconds ?? DefaultConfigCheckSeconds
				};
				foreach (var ep in endPoint.EndPoints)
				{
					configuration.EndPoints.Add(ep);
				}
				var redisConnection = new RedisConnection
				{
					ConnectionMultiplexer = ConnectionMultiplexer.Connect(configuration),
					RedisDatabase = endPoint.RedisDatabase ?? DefaultRedisDatabase,
					RedisKeyFormat =
						string.IsNullOrEmpty(endPoint.RedisKeyFormat) ? RedisLocker.DefaultRedisKeyFormat : endPoint.RedisKeyFormat
				};
				connections.Add(redisConnection);
			}
			return connections;
		} 
	}
}
