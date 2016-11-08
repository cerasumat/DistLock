using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DistLock.Locker.Redis
{
	public class RedisLocker : ILocker
	{
		public const string DefaultRedisKeyFormat = "redlock-{0}";
		private readonly object _lockObject = new object();
		private readonly ICollection<RedisConnection> _redisConnections;

		private readonly int _quorum;
		private readonly int _quorumRetryCount;
		private readonly int _quorumRetryDelayMs;
		private readonly double _clockDriftFactor;
		private bool _isDisposed;
		private Timer _lockKeepaliveTimer;

		private readonly TimeSpan _expiryTime;
		private readonly TimeSpan? _waitTime;
		private readonly TimeSpan? _retryTime;
		private readonly CancellationToken _cancellationToken;
		private readonly TimeSpan _minExpiryTime = TimeSpan.FromMilliseconds(10);
		private readonly TimeSpan _minRetryTime = TimeSpan.FromMilliseconds(10);

		public readonly string KeyName;
		public string LockId { get; private set; }
		public bool IsAcquired { get; private set; }
		public int ExtendCount { get; private set; }

		private RedisLocker(ICollection<RedisConnection> redisConnections, string keyName, TimeSpan expiryTime,
			TimeSpan? waitTime = null, TimeSpan? retryTime = null, CancellationToken? cancellationToken = null)
		{
			if (expiryTime < _minExpiryTime)
				expiryTime = _minExpiryTime;
			if (retryTime.HasValue && retryTime.Value < _minRetryTime)
				retryTime = _retryTime;
			_redisConnections = redisConnections;
			_quorum = redisConnections.Count/2 + 1;
			_quorumRetryCount = 3;
			_quorumRetryDelayMs = 400;
			_clockDriftFactor = 0.01;
			KeyName = keyName;
			LockId = Guid.NewGuid().ToString("N");
			_expiryTime = expiryTime;
			_waitTime = waitTime;
			_retryTime = retryTime;
			_cancellationToken = cancellationToken ?? CancellationToken.None;
		}

		internal static RedisLocker Create(ICollection<RedisConnection> redisConnections, string keyName, TimeSpan expiryTime,
			TimeSpan? waitTime = null, TimeSpan? retryTime = null, CancellationToken? cancellationToken = null)
		{
			var redisLock = new RedisLocker(redisConnections, keyName, expiryTime, waitTime, retryTime, cancellationToken);
			redisLock.Start();
			return redisLock;
		}

		internal static async Task<RedisLocker> CreateAsync(ICollection<RedisConnection> redisConnections, string keyName,
			TimeSpan expiryTime, TimeSpan? waitTime = null, TimeSpan? retryTime = null,
			CancellationToken? cancellationToken = null)
		{
			var redisLock = new RedisLocker(redisConnections, keyName, expiryTime, waitTime, retryTime, cancellationToken);
			await redisLock.StartAsync().ConfigureAwait(false);
			return redisLock;
		}

		private void Start()
		{
			if (_waitTime.HasValue && _retryTime.HasValue && _waitTime.Value.TotalMilliseconds > 0 &&
			    _retryTime.Value.TotalMilliseconds > 0)
			{
				var stopWatch = Stopwatch.StartNew();
				while (!IsAcquired && stopWatch.Elapsed <= _waitTime.Value)
				{
					IsAcquired = Acquire();
					if (!IsAcquired)
					{
						Task.Delay(_retryTime.Value, _cancellationToken).Wait(_cancellationToken);
					}
				}
			}
			else
			{
				IsAcquired = Acquire();
			}

			if (IsAcquired)
			{
				StartAutoExtendTimer();
			}
		}

		private async Task StartAsync()
		{
			if (_waitTime.HasValue && _retryTime.HasValue && _waitTime.Value.TotalMilliseconds > 0 &&
			    _retryTime.Value.TotalMilliseconds > 0)
			{
				var stopWatch = Stopwatch.StartNew();
				while (!IsAcquired && stopWatch.Elapsed <= _waitTime.Value)
				{
					IsAcquired = await AcquireAsync().ConfigureAwait(false);
					if (!IsAcquired)
					{
						await Task.Delay(_retryTime.Value, _cancellationToken).ConfigureAwait(false);
					}
				}
			}
			else
			{
				IsAcquired = await AcquireAsync().ConfigureAwait(false);
			}

			if (IsAcquired)
			{
				StartAutoExtendTimer();
			}
		}

		private bool Acquire()
		{
			for (var i = 0; i < _quorumRetryCount; i++)
			{
				_cancellationToken.ThrowIfCancellationRequested();
				var startTick = Stopwatch.GetTimestamp();
				var locksAcquired = Lock();
				var validityTicks = GetRemainingValidityTicks(startTick);
				if (locksAcquired >= _quorum && validityTicks > 0)
				{
					return true;
				}
				// Failed to get enough locks for a quorum, unlock everything and try again
				Unlock();
				// only sleep if we have more retries left
				if (i < _quorumRetryCount - 1)
				{
					var sleepMs = ThreadSafeRandom.Next(_quorumRetryDelayMs);
					Task.Delay(sleepMs, _cancellationToken).Wait(_cancellationToken);
				}
			}
			return false;
		}

		private async Task<bool> AcquireAsync()
		{
			for (var i = 0; i < _quorumRetryCount; i++)
			{
				_cancellationToken.ThrowIfCancellationRequested();
				var startTick = Stopwatch.GetTimestamp();
				var locksAcquired = await LockAsync().ConfigureAwait(false);
				var validityTicks = GetRemainingValidityTicks(startTick);
				if (locksAcquired >= _quorum && validityTicks > 0)
				{
					return true;
				}
				// Failed to get enough locks for a quorum, unlock everything and try again
				await UnlockAsync().ConfigureAwait(false);
				// only sleep if we have more retries left
				if (i < _quorumRetryCount - 1)
				{
					var sleepMs = ThreadSafeRandom.Next(_quorumRetryDelayMs);
					await Task.Delay(sleepMs, _cancellationToken).ConfigureAwait(false);
				}
			}
			return false;
		}

		private void StartAutoExtendTimer()
		{
			_lockKeepaliveTimer = new Timer(
				state =>
				{
					try
					{
						var startTick = Stopwatch.GetTimestamp();
						var locksExtended = Extend();
						var validityTicks = GetRemainingValidityTicks(startTick);
						if (locksExtended >= _quorum && validityTicks > 0)
						{
							IsAcquired = true;
							ExtendCount++;
						}
						else
						{
							IsAcquired = false;
						}
					}
					catch (Exception exp)
					{
						//throw exp;
					}
				}, null, (long)(_expiryTime.TotalMilliseconds/2), (long)(_expiryTime.TotalMilliseconds/2)
				);
		}

		private long GetRemainingValidityTicks(long startTick)
		{
			// Add 2 milliseconds to the drift to account for Redis expires precision,
			// which is 1 milliescond, plus 1 millisecond min drift for small TTLs.
			var driftTicks = ((long)(_expiryTime.Ticks * _clockDriftFactor)) + TimeSpan.FromMilliseconds(2).Ticks;
			var validityTicks = _expiryTime.Ticks - (Stopwatch.GetTimestamp() - startTick) - driftTicks;
			return validityTicks;
		}

		private int Lock()
		{
			var locksAquired = 0;
			Parallel.ForEach(_redisConnections, conn =>
			{
				if (LockInstance(conn))
				{
					Interlocked.Increment(ref locksAquired);
				}
			});
			return locksAquired;
		}

		private async Task<int> LockAsync()
		{
			IEnumerable<Task<bool>> lockTasks = _redisConnections.Select(LockInstanceAsync);
			bool[] lockResults = await Task.WhenAll(lockTasks).ConfigureAwait(false);
			return lockResults.Count(x => x == true);
		}

		private int Extend()
		{
			var locksExtended = 0;
			Parallel.ForEach(_redisConnections, conn =>
			{
				if (ExtendInstance(conn))
				{
					Interlocked.Increment(ref locksExtended);
				}
			});
			return locksExtended;
		}

		private void Unlock()
		{
			Parallel.ForEach(_redisConnections, conn => UnlockInstance(conn));
			IsAcquired = false;
		}

		private async Task UnlockAsync()
		{
			IEnumerable<Task<bool>> unlockTasks = _redisConnections.Select(UnlockInstanceAsync);
			await Task.WhenAll(unlockTasks).ConfigureAwait(false);
		}

		private bool LockInstance(RedisConnection conn)
		{
			string redisKey = GetRedisKey(conn.RedisKeyFormat, KeyName);
			var result = false;
			try
			{
				result = conn.ConnectionMultiplexer.GetDatabase(conn.RedisDatabase)
					.StringSet(redisKey, LockId, _expiryTime, When.NotExists, CommandFlags.DemandMaster);
			}
			catch (Exception exp)
			{
				//throw exp;
			}
			return result;
		}

		private async Task<bool> LockInstanceAsync(RedisConnection conn)
		{
			var redisKey = GetRedisKey(conn.RedisKeyFormat, KeyName);
			var result = false;
			try
			{
				result =
					await
						conn.ConnectionMultiplexer.GetDatabase(conn.RedisDatabase)
							.StringSetAsync(redisKey, LockId, _expiryTime, When.NotExists, CommandFlags.DemandMaster)
							.ConfigureAwait(false);
			}
			catch (Exception exp)
			{
				//throw exp;
			}
			return result;
		}

		private bool ExtendInstance(RedisConnection conn)
		{
			var redisKey = GetRedisKey(conn.RedisKeyFormat, KeyName);
			var result = false;
			try
			{
				var extendResult = (long)
					conn.ConnectionMultiplexer.GetDatabase(conn.RedisDatabase)
						.ScriptEvaluate(RedisScripts.ExtendIfMatchingValueScript, new RedisKey[] {redisKey},
							new RedisValue[] {LockId, (long) _expiryTime.TotalMilliseconds}, CommandFlags.DemandMaster);
				result = (extendResult == 1);
			}
			catch (Exception exp)
			{
				//throw exp;
			}
			return result;
		}

		private bool UnlockInstance(RedisConnection conn)
		{
			var redisKey = GetRedisKey(conn.RedisKeyFormat, KeyName);
			var result = false;
			try
			{
				result =
					(bool)
						conn.ConnectionMultiplexer.GetDatabase(conn.RedisDatabase)
							.ScriptEvaluate(RedisScripts.UnlockScritpt, new RedisKey[] {redisKey}, new RedisValue[] {LockId},
								CommandFlags.DemandMaster);
			}
			catch (Exception exp)
			{
				//throw exp;
			}
			return result;
		}

		private async Task<bool> UnlockInstanceAsync(RedisConnection conn)
		{
			var redisKey = GetRedisKey(conn.RedisKeyFormat, KeyName);
			var result = false;
			try
			{
				result =
					(bool)
						await
							conn.ConnectionMultiplexer.GetDatabase(conn.RedisDatabase)
								.ScriptEvaluateAsync(RedisScripts.UnlockScritpt, new RedisKey[] {redisKey}, new RedisValue[] {LockId},
									CommandFlags.DemandMaster)
								.ConfigureAwait(false);
			}
			catch (Exception exp)
			{
				//throw exp;
			}
			return result;
		}

		private static string GetRedisKey(string keyFormat, string keyName)
		{
			return string.Format(keyFormat, keyName);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed) return;
			if (disposing)
			{
				lock (_lockObject)
				{
					if (null == _lockKeepaliveTimer) return;
					_lockKeepaliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
					_lockKeepaliveTimer.Dispose();
					_lockKeepaliveTimer = null;
				}
			}
			Unlock();
			_isDisposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// For unit tests only, do not use in normal operation
		/// </summary>
		internal void StopKeepAliveTimer()
		{
			if (_lockKeepaliveTimer == null)
			{
				return;
			}
			_lockKeepaliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
		}
	}
}
