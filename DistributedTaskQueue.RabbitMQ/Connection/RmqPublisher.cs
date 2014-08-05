using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DistributedTaskQueue.RabbitMQ.Connection
{
	class RmqPublisher
	{
		private readonly ProcessingQueue _queue;

		public RmqPublisher(ProcessingQueue queue)
		{
			_queue = queue;
			_queue.Reconnected += delegate { _registeredQueues = new Dictionary<IModel, HashSet<string>>(); };
		}

		private Dictionary<IModel, HashSet<string>> _registeredQueues = new Dictionary<IModel, HashSet<string>>();

		public Task Send(string queue, byte[] data)
		{
			return _queue.EnqueueTask(model =>
			{
				lock (_registeredQueues)
				{
					var set = _registeredQueues.GetOrAdd(model, _ => new HashSet<string>());
					if (!set.Contains(queue))
						model.QueueDeclare(queue, true, false, false, new Dictionary<string, object>());
					set.Add(queue);
				}

				var props = model.CreateBasicProperties();
				props.SetPersistent(true);
				model.BasicPublish("", queue, props, data);
			});
		}

		public Task CreateDurableQueue(string queue)
		{
			return _queue.EnqueueTask(model => model.QueueDeclare(queue, true, false, false, new Dictionary<string, object>()));
		}
	}
}
