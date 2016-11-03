using StackExchange.Redis;

namespace DistLock.Locker.Redis
{
	public class RedisConnection
	{
		public ConnectionMultiplexer ConnectionMultiplexer { get; set; }
		public int RedisDatabase { get; set; }
		public string RedisKeyFormat { get; set; }
	}
}
