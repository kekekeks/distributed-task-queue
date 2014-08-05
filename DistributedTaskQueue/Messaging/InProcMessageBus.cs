using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedTaskQueue.Messaging
{
	/// <summary>
	/// Ineffective and loses messages sent before subscription. For automated testing environments without a broker.
	/// </summary>
	public class InProcMessageBus :IMessageBus
	{
		private readonly Action<string, Exception> _logger;
		private readonly Dictionary<string, Func<byte[], Task>> _handlers = new Dictionary<string, Func<byte[], Task>>();
		private readonly HashSet<Task> _pending = new HashSet<Task>();

		public InProcMessageBus(Action<string, Exception> logger)
		{
			_logger = logger;
		}

	

		public Task Publish(string queue, byte[] data)
		{
			var tcs = new TaskCompletionSource<int>();
			lock (_pending)
				_pending.Add(tcs.Task);

			Func<byte[], Task> handler;
			lock (_handlers)
			{
				handler = _handlers[queue];
			}
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				try
				{
					await handler(data);
				}
				catch (Exception e)
				{
					lock (_pending)
						_pending.Remove(tcs.Task);
					tcs.SetResult(0);
					_logger("Error executing handler", e);
					return;
				} 
				lock (_pending)
					_pending.Remove(tcs.Task);
				tcs.SetResult(0);

			});
			return Task.FromResult(0);
		}

		public Task Subscribe(string queue, Func<byte[], Task> handler)
		{
			lock (_handlers)
				_handlers.Add(queue, handler);
			return Task.FromResult(0);
		}

		public Task WaitForPending()
		{
			lock (_pending)
				return Task.WhenAll(_pending.ToList());
		}
	}
}
