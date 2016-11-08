using RestSharp;

namespace DistLock.Locker.Etcd
{
	public class EtcdConnection
	{
		public IRestClient EtcdClient { get; set; }
		public string EtcdKeyFormat { get; set; }
	}
}
