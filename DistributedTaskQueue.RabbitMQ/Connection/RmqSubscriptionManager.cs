using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DistributedTaskQueue.RabbitMQ.Connection
{
	class RmqSubscriptionManager
	{
		private readonly ProcessingQueue _queue;
		private readonly RmqPublisher _publisher;
		private readonly Action<string, Exception> _logger;

		class Subscription
		{	
			internal Func<byte[], Task> Handler { get; private set; }
			internal string Queue { get; private set; }

			public Subscription(string queue, Func<byte[], Task> handler)
			{
				Queue = queue;
				Handler = handler;
			}
		}


		readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription> ();

		public RmqSubscriptionManager (ProcessingQueue queue, RmqPublisher publisher, Action<string, Exception> logger)
		{
			_queue = queue;
			_publisher = publisher;
			_logger = logger ?? ((_, __) => { });
			_queue.Reconnected += RestoreSubscriptions;
		}



		async Task Subscribe (Subscription subscription)
		{
			await _publisher.CreateDurableQueue(subscription.Queue);
			await _queue.EnqueueTask(model =>
			{
				var c = new EventingBasicConsumer(model);
				c.Received += (s, e) => ThreadPool.QueueUserWorkItem(_ => HandleMessage(subscription, e.Body, e.DeliveryTag, model));

				model.BasicConsume(subscription.Queue, false, c);

				lock (_subscriptions)
					_subscriptions.Add(subscription);
			});

		}

		async void SafeExec(Task t)
		{
			try
			{
				await t;
			}
			catch (Exception e)
			{
				_logger("Internal error", e);
			}
		}

		async void HandleMessage(Subscription s, byte[] data, ulong tag, IModel model)
		{
			var status = false;
			try
			{
				await s.Handler(data);
				status = true;
			}
			catch (Exception e)
			{
				_logger("Exception while executing message handler", e);
			}
			SafeExec(_queue.EnqueueTask(m =>
			{
				if (m != model) //Reconnect occured, its not the same connection anymore
					return;
				if (status)
					model.BasicAck(tag, false);
				else
					model.BasicNack(tag, false, true);
			}));


		}
		
		public Task Subscribe(string queue, Func<byte[], Task> handler)
		{
			return Subscribe (new Subscription (queue, handler));
		}

		private void RestoreSubscriptions(object sender, EventArgs e)
		{
			List<Subscription> subs;
			lock (_subscriptions)
				subs = _subscriptions.ToList();
			Task.WhenAll(subs.Select(Subscribe).ToList()).ContinueWith(t =>
			{
				//We can safely ignore that
			});
		}
	}
}
