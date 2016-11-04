namespace DistLock.Locker.Redis
{
	public static class RedisScripts
	{
		// Delete the key if key matches and value equals.
		// Returns 1 as success, otherwise 0
		public const string UnlockScritpt =
			@"if redis.call('get', KEYS[1]) == ARGV[1] then
				return redis.call('del', KEYS[1])
			else
				return 0
			end";

		// Set the expiry for the given key if its value matches the supplied value.
		// Returns 1 on success, 0 on failure setting expiry or key not existing, -1 if the key value didn't match
		public const string ExtendIfMatchingValueScript =
			@"local currentVal = redis.call('get', KEYS[1])
			if (currentVal == false) then
				return redis.call('set', KEYS[1], ARGV[1], 'PX', ARGV[2]) and 1 or 0
			elseif (currentVal == ARGV[1]) then
				return redis.call('pexpire', KEYS[1], ARGV[2])
			else
				return -1
			end";
	}
}
