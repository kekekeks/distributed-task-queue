using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedTaskQueue.RabbitMQ.Connection;

namespace DistributedTaskQueue.RabbitMq
{
	public class RabbitMqMessageBus : IMessageBus
	{
		private readonly string _connectionString;
		private readonly ProcessingQueue _queue;
		private readonly RmqPublisher _publisher;
		private readonly RmqSubscriptionManager _subscriber;

		public RabbitMqMessageBus(string connectionString, Action<string, Exception> logger = null)
		{
			_connectionString = connectionString;
			logger = logger ?? ((_, __) => { });
			_queue = new ProcessingQueue(new Uri(connectionString), logger);
			_publisher = new RmqPublisher(_queue);
			_subscriber = new RmqSubscriptionManager(_queue, _publisher, logger);
		}




		public Task Publish(string queue, byte[] data)
		{
			return _publisher.Send(queue, data);
		}

		public Task Subscribe(string queue, Func<byte[], Task> handler)
		{
			return _subscriber.Subscribe(queue, handler);
		}
	}
}
