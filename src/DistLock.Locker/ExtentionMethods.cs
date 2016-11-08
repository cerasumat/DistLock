using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistLock.Locker
{
	public static class ExtentionMethods
	{
		public static Uri AppendPath(this Uri uri, string path)
		{
			var newPath = uri.AbsolutePath.TrimEnd(new[] { '/', ' ' }) + "/" + path;
			return new UriBuilder(uri.Scheme, uri.Host, uri.Port, newPath, uri.Query).Uri;
		}

		public static int ToInt32(this string str)
        {
            var result = 0;
            if (int.TryParse(str, out result))
                return result;
            throw new NotFiniteNumberException(string.Format("{0}不能转换为32位整数", str));
        }
	}
}
