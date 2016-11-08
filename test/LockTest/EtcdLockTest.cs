using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DistLock.Locker.Etcd;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LockTest
{
	[TestClass]
	public class EtcdLockTest
	{
		// Active Redis Server
		private static readonly Uri ActiveServer1 = new Uri("http://10.200.50.210:2379");
		private static readonly Uri ActiveServer2 = new Uri("http://10.200.50.211:2379");
		private static readonly Uri ActiveServer3 = new Uri("http://10.200.50.212:2379");

		private static readonly IEnumerable<Uri> AllActiveEndpoints = new[]
		{
			ActiveServer1,
			ActiveServer2,
			ActiveServer3
		};

		[TestMethod]
		public void TestSingleLock()
		{
			CheckSingleEtcdLock(AllActiveEndpoints, true);
		}

		private static void CheckSingleEtcdLock(IEnumerable<Uri> endPoints, bool expectedToAcquire)
		{
			using (var etcdLockFactory = new EtcdLockerFactory(endPoints))
			{
				var keyName = string.Format("test-1112");
				using (var etcdLock = etcdLockFactory.Create(keyName, 60000))
				{
					Assert.IsTrue(etcdLock.IsAcquired == expectedToAcquire);
				}
			}
		}

		[TestMethod]
		public void TestOverlappingLocks()
		{
			using (var redisLockFactory = new EtcdLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("test-{0}", Guid.NewGuid());
				using (var firstLock = redisLockFactory.Create(keyName, 120 * 1000))
				{
					Assert.IsTrue(firstLock.IsAcquired);
					using (var secondLock = redisLockFactory.Create(keyName, 120 * 100))
					{
						Assert.IsFalse(secondLock.IsAcquired);
					}
				}
			}
		}

		[TestMethod]
		public void TestRenewing()
		{
			using (var etcdLockFactory = new EtcdLockerFactory(AllActiveEndpoints))
			{
				var keyName = string.Format("testLock-{0}", 123);
				int extendCount;
				using (var etcdLock = etcdLockFactory.Create(keyName, 2000))
				{
					Assert.IsTrue(etcdLock.IsAcquired);
					Thread.Sleep(6000);
					extendCount = etcdLock.ExtendCount;
				}
				Assert.IsTrue(extendCount > 1);
			}
		}
	}
}
