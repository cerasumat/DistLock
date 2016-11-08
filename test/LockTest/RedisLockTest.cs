using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DistLock.Locker.Redis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LockTest
{
	[TestClass]
	public class RedisLockTest
	{
		// Active Redis Server
		private static readonly DnsEndPoint ActiveServer1 = new DnsEndPoint("10.200.50.211", 30301);
		private static readonly DnsEndPoint ActiveServer2 = new DnsEndPoint("10.200.50.198", 6379);

		// Inactive Redis Server
		private static readonly DnsEndPoint InactiveServer1 = new DnsEndPoint("10.200.50.212", 30304);
		private static readonly DnsEndPoint InactiveServer2 = new DnsEndPoint("10.200.50.212", 30305);

		private static readonly IEnumerable<EndPoint> AllActiveEndpoints = new[]
		{
			//ActiveServer1,
			ActiveServer2
		};

		private static readonly IEnumerable<EndPoint> AllInactiveEndpoints = new[]
		{
			InactiveServer1,
			InactiveServer2
		};

		private static readonly IEnumerable<EndPoint> EndpointsWithQuorum = new[]
		{
			ActiveServer1,
			ActiveServer2,
			InactiveServer1
		};

		private static readonly IEnumerable<EndPoint> EndpointsWithNoQuorum = new[]
		{
			ActiveServer1,
			InactiveServer1,
			InactiveServer2
		};
		
		[TestMethod]
		public void TestSingleLock()
		{
			CheckSingleRedisLock(AllActiveEndpoints, true);
		}

		private static void CheckSingleRedisLock(IEnumerable<EndPoint> endPoints, bool expectedToAcquire)
		{
			CheckSingleRedisLock(endPoints.Select(x => new RedisLockEndPoint {EndPoint = x}), expectedToAcquire);
		}

		private static void CheckSingleRedisLock(IEnumerable<RedisLockEndPoint> endPoints, bool expectedToAcquire)
		{
			using (var redisLockFactory = new RedisLockerFactory(endPoints))
			{
				var keyName = string.Format("test");
				using (var redisLock = redisLockFactory.Create(keyName, 120000))
				{
					Assert.IsTrue(redisLock.IsAcquired==expectedToAcquire);
				}
			}
		}

		[TestMethod]
		public void TestOverlappingLocks()
		{
			using (var redisLockFactory = new RedisLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("test-{0}", Guid.NewGuid());
				using (var firstLock = redisLockFactory.Create(keyName, 120*1000))
				{
					Assert.IsTrue(firstLock.IsAcquired);
					using (var secondLock = redisLockFactory.Create(keyName, 120*100))
					{
						Assert.IsFalse(secondLock.IsAcquired);
					}
				}
			}
		}

		[TestMethod]
		public void TestBlockingConcurrentLocks()
		{
			var locksAcquired = 0;
			using (var redisLockFactory = new RedisLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("concurrentTestLock-{0}", Guid.NewGuid());
				var threads = new List<Thread>();
				for (var i = 0; i < 3; i++)
				{
					var thread = new Thread(() =>
					{
						using (var redisLock = redisLockFactory.Create(keyName, 3*1000, 15*1000, 500))
						{
							if (redisLock.IsAcquired)
							{
								Interlocked.Increment(ref locksAcquired);
							}
							Thread.Sleep(4000);
						}
					});
					thread.Start();
					threads.Add(thread);
				}
				foreach (var thread in threads)
				{
					thread.Join();
				}
			}
			Assert.IsTrue(locksAcquired==3);
		}

		[TestMethod]
		public void TestSequentialLocks()
		{
			using (var redisLockFactory = new RedisLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("testLock-{0}", Guid.NewGuid());
				using (var firstLock = redisLockFactory.Create(keyName, 30*1000))
				{
					Assert.IsTrue(firstLock.IsAcquired);
				}
				using (var secondLock = redisLockFactory.Create(keyName, 30*1000))
				{
					Assert.IsTrue(secondLock.IsAcquired);
				}
			}
		}

		[TestMethod]
		public void TestRenewing()
		{
			using (var redisLockFactory = new RedisLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("testLock-{0}", Guid.NewGuid());
				int extendCount;
				using (var redisLock = redisLockFactory.Create(keyName, 2*1000))
				{
					Assert.IsTrue(redisLock.IsAcquired);
					Thread.Sleep(5000);
					extendCount = redisLock.ExtendCount;
				}
				Assert.IsTrue(extendCount>2);
			}
		}

	}
}
