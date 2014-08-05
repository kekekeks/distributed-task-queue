using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DistributedTaskQueue.Logging;
using DistributedTaskQueue.Serialization;

namespace DistributedTaskQueue
{
	public class TaskQueueManager
	{
		private readonly IMessageBus _messageBus;
		private readonly IDistributedTaskSerializer _serializer;
		private readonly IFaultedTaskLogger _logger;
		private volatile Dictionary<Type, string> _queueNameCache = new Dictionary<Type, string>();
		private readonly object _queueNameCacheSyncRoot = new object();

		string GetQueue(object obj)
		{
			string rv;
			var type = (obj as Type) ?? obj.GetType();
			if (_queueNameCache.TryGetValue(type, out rv))
				return rv;
			lock (_queueNameCacheSyncRoot)
			{
				if (_queueNameCache.TryGetValue(type, out rv))
					return rv;
				var newCache = _queueNameCache.ToDictionary(x => x.Key, x => x.Value);
				var description = type.GetCustomAttributes(typeof (QueuedTaskAttribute), false)
					.OfType<QueuedTaskAttribute>().FirstOrDefault();
				newCache[type] = rv = (description ?? new QueuedTaskAttribute()).Queue ?? type.Name;
				_queueNameCache = newCache;
				return rv;
			}
		}


		public TaskQueueManager(IMessageBus messageBus, IDistributedTaskSerializer serializer, IFaultedTaskLogger logger)
		{
			_messageBus = messageBus;
			_serializer = serializer;
			_logger = logger;
		}

		public TaskQueueManager(IMessageBus messageBus) : this(messageBus, new JsonNetDistributedTaskSerializer(), new NullFaultedTaskLogger())
		{
			
		}

		public Task Subscribe<T>(Func<T, Task> handler)
		{
			return _messageBus.Subscribe(GetQueue(typeof (T)), data =>
			{
				var task = _serializer.Deserialize<T>(data);
				return handler(task).ContinueWith(t =>
				{
					if (t.IsFaulted)
						_logger.LogException(t.Exception, GetQueue(typeof (T)), task);
				}, TaskContinuationOptions.ExecuteSynchronously);
			});
		}

		public Task Enqueue(object task)
		{
			return _messageBus.Publish(GetQueue(task), _serializer.Serialize(task));
		}
	}
}
