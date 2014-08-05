using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTaskQueue.Messaging
{
	public class NamespaceMessageBus : IMessageBus
	{
		private readonly IMessageBus _bus;
		private readonly string _namespace;

		public NamespaceMessageBus(IMessageBus bus, string @namespace)
		{
			_bus = bus;
			_namespace = @namespace;
		}

		public Task Publish(string queue, byte[] data)
		{
			if (queue == null) throw new ArgumentNullException("queue");
			return _bus.Publish(_namespace + queue, data);
		}

		public Task Subscribe(string queue, Func<byte[], Task> handler)
		{
			if (queue == null) throw new ArgumentNullException("queue");
			return _bus.Subscribe(_namespace + queue, handler);
		}
	}
}
