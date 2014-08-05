using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTaskQueue.RabbitMQ
{
	static class Extensions
	{
		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TKey, TValue> getter)
		{
			TValue v;
			if (d.TryGetValue(key, out v))
				return v;
			return d[key] = getter(key);
		}
	}
}
