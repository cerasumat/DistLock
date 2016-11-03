﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DistLock.Locker.Redis
{
	public class RedisLockEndPoint
	{
		public RedisLockEndPoint()
		{
			EndPoints = new List<EndPoint>();
		}

		/// <summary>
		/// The endpoints for the redis connection. Can be used for connecting to replicated master/slaves.
		/// These servers will all be considered a single entity as far as the RedLock algorithm is concerned.
		/// See http://redis.io/topics/distlock#why-failover-based-implementations-are-not-enough
		/// </summary>
		public IList<EndPoint> EndPoints { get; private set; }

		/// <summary>
		/// The endpoint for the redis connection.
		/// </summary>
		public EndPoint EndPoint
		{
			get
			{
				return EndPoints.FirstOrDefault();
			}
			set
			{
				EndPoints = new List<EndPoint> { value };
			}
		}

		/// <summary>
		/// Whether to use SSL for the redis connection.
		/// </summary>
		public bool Ssl { get; set; }

		/// <summary>
		/// The password for the redis connection.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// The connection timeout for the redis connection.
		/// Defaults to 100ms if not specified.
		/// </summary>
		public int? ConnectionTimeout { get; set; }

		/// <summary>
		/// The database to use with this redis connection.
		/// Defaults to 0 if not specified.
		/// </summary>
		public int? RedisDatabase { get; set; }

		/// <summary>
		/// The string format for keys created in redis, must include {0}.
		/// Defaults to "redlock-{0}" if not specified.
		/// </summary>
		public string RedisKeyFormat { get; set; }

		/// <summary>
		/// The number of seconds between config change checks
		/// Defaults to 10 seconds if not specified.
		/// </summary>
		public int? ConfigCheckSeconds { get; set; }
	}
}
