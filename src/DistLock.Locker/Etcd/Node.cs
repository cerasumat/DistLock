using System;
using System.Collections.Generic;

namespace DistLock.Locker.Etcd
{
	public class Node
	{
		public int CreatedIndex { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }
		public int ModifiedIndex { get; set; }
		public int? Ttl { get; set; }
		public DateTime? Expiration { get; set; }
		public bool Dir { get; set; }
		public List<Node> Nodes { get; set; } 
	}
}
