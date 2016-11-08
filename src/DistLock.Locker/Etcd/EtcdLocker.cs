using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace DistLock.Locker.Etcd
{
	public class EtcdLocker : ILocker
	{
		public const string DefaultEtcdKeyFormat = "etcdlock-{0}";
		private readonly double _clockDriftFactor;
		private readonly object _lockObject = new object();
		private readonly IEnumerable<EtcdConnection> _etcdConnections;
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

		private EtcdLocker(IEnumerable<EtcdConnection> etcdEndpoints, string keyName, TimeSpan expiryTime,
			TimeSpan? waitTime = null, TimeSpan? retryTime = null, CancellationToken? cancellationToken = null)
		{
			if (expiryTime < _minExpiryTime)
				expiryTime = _minExpiryTime;
			if (retryTime.HasValue && retryTime.Value < _minRetryTime)
				retryTime = _retryTime;
			_etcdConnections = etcdEndpoints;
			_clockDriftFactor = 0.01;
			KeyName = keyName;
			LockId = Guid.NewGuid().ToString("N");
			_expiryTime = expiryTime;
			_waitTime = waitTime;
			_retryTime = retryTime;
			_cancellationToken = cancellationToken ?? CancellationToken.None;
		}

		internal static EtcdLocker Create(IEnumerable<EtcdConnection> etcdEndpoints, string keyName, TimeSpan expiryTime,
			TimeSpan? waitTime = null, TimeSpan? retryTime = null, CancellationToken? cancellationToken = null)
		{
			var etcdLock = new EtcdLocker(etcdEndpoints, keyName, expiryTime, waitTime, retryTime, cancellationToken);
			etcdLock.Start();
			return etcdLock;
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

		private bool Acquire()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			var startTick = Stopwatch.GetTimestamp();
			var locksAcquired = Lock();
			var validityTicks = GetRemainingValidityTicks(startTick);
			return locksAcquired && validityTicks > 0;
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
						if (locksExtended && validityTicks > 0)
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
				}, null, (long) (_expiryTime.TotalMilliseconds/2), (long) (_expiryTime.TotalMilliseconds/2));
		}

		private long GetRemainingValidityTicks(long startTick)
		{
			// Add 2 milliseconds to the drift to account for etcd expires precision,
			// which is 1 milliescond, plus 1 millisecond min drift for small TTLs.
			var driftTicks = ((long)(_expiryTime.Ticks * _clockDriftFactor)) + TimeSpan.FromMilliseconds(2).Ticks;
			var validityTicks = _expiryTime.Ticks - (Stopwatch.GetTimestamp() - startTick) - driftTicks;
			return validityTicks;
		}

		private bool Lock()
		{
			Action<IRestRequest> action = req =>
			{
				req.AddParameter("value", LockId);
				if (_expiryTime.TotalSeconds > 0)
					req.AddParameter("ttl", (int) _expiryTime.TotalSeconds);
				req.AddParameter("prevExist", "false");
			};
			EtcdResponse response = Request(KeyName, Method.PUT, action);
			return !response.ErrorCode.HasValue;
		}

		private bool Extend()
		{
			Action<IRestRequest> action = req =>
			{
				req.AddParameter("value", LockId);
				if (_expiryTime.TotalSeconds > 0)
					req.AddParameter("ttl", (int) _expiryTime.TotalSeconds);
				req.AddParameter("prevExist", "true");
				req.AddParameter("prevValue", LockId);
			};
			EtcdResponse response = Request(KeyName, Method.PUT, action);
			return !response.ErrorCode.HasValue;
		}

		private void Unlock()
		{
			Action<IRestRequest> action = req =>
			{
				req.AddParameter("value", LockId);
				req.AddParameter("prevExist", "true");
				req.AddParameter("prevValue", LockId);
			};
			EtcdResponse response = Request(KeyName, Method.DELETE, action);
			IsAcquired = false;
		}

		public void Dispose()
		{
			Dispose(true);
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

		private EtcdResponse Request(string key, Method method, Action<IRestRequest> action = null)
        {
			foreach (var etcdConnection in _etcdConnections)
			{
				var requestUrl =
					etcdConnection.EtcdClient.BaseUrl.AppendPath("v2").AppendPath("keys").AppendPath("lock").AppendPath(key);
				var request = new RestRequest(requestUrl, method);
				if (action != null)
				{
					action(request);
				}
				request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
				var response = etcdConnection.EtcdClient.Execute<EtcdResponse>(request);
				if (CheckErrors(response)) continue;
				var etcdResponse = ProcessRestResponse(response);
				return etcdResponse;
			}
            return NotFoundResponse();
        }

		private static bool CheckErrors(IRestResponse response)
		{
			return response.StatusCode == 0;
		}

		private static EtcdResponse ProcessRestResponse(IRestResponse<EtcdResponse> response)
		{
			var etcdResponse = response != null ? response.Data : null;
            if (etcdResponse == null) return NotFoundResponse();
            etcdResponse.Headers.EtcdIndex =
                response.Headers.First(h => h.Name.Equals("X-Etcd-Index")).Value.ToString().ToInt32();
            var raftIndexHeader = response.Headers.FirstOrDefault(h => h.Name.Equals("X-Raft-Index"));
            if (null != raftIndexHeader)
                etcdResponse.Headers.RaftIndex = raftIndexHeader.Value.ToString().ToInt32();
            var raftTermeHeader = response.Headers.FirstOrDefault(h => h.Name.Equals("X-Raft-Term"));
            if (null != raftTermeHeader)
                etcdResponse.Headers.RaftTerm = raftTermeHeader.Value.ToString().ToInt32();
            return etcdResponse;
        }

		private static EtcdResponse NotFoundResponse()
		{
			return new EtcdResponse
			{
				ErrorCode = 404,
				Message = "No Content Found",
				Cause = "No Content Found"
			};
		}
	}
}
