using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistLock.Locker.Etcd
{
	public class EtcdResponse
	{
		public EtcdResponse()
		{
			Headers = new EtcdHeaders();
		}

		public string Action { get; set; }
		public Node Node { get; set; }
		public int? ErrorCode { get; set; }
		public string Cause { get; set; }
		public int? Index { get; set; }
		public string Message { get; set; }
		public Node PrevNode { get; set; }
		public EtcdHeaders Headers { get; set; }
	}

	public class EtcdHeaders
	{
		public int EtcdIndex { get; set; }
		public int? RaftIndex { get; set; }
		public int? RaftTerm { get; set; }
	}
}
