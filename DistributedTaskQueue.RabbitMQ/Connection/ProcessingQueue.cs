using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DistributedTaskQueue.RabbitMQ.Connection
{
	class ProcessingQueue
	{
		readonly Queue<Action<IModel>> _queue = new Queue<Action<IModel>> ();
		readonly AutoResetEvent _event = new AutoResetEvent (false);
		private readonly Uri _uri;
		private readonly Action<string, Exception> _loger;
		public event EventHandler Reconnected = delegate { };
		public ProcessingQueue (Uri uri, Action<string, Exception> loger)
		{
			_uri = uri;
			_loger = loger;
			Connector.Connect(_uri);
			new Thread(m => ThreadProc((IModel) m)).Start(Connector.Connect(_uri));
		}

		IModel Reconnect (IModel model)
		{
			var t = Connector.Reconnect (model, _uri);
			t.Wait ();
			Reconnected(this, new EventArgs());
			return t.Result;
		}

		void ThreadProc (IModel model)
		{
			for (;;)
			{
				Action<IModel> current = null;
				lock (_queue)
				{
					if (_queue.Count != 0)
						current = _queue.Dequeue ();
				}

				if (current == null)
				{
					//Wait for incoming events or timeout
					_event.WaitOne (1000);
					continue;
				}

				var reconnect = true;
				try
				{
					current (model);
					reconnect = false;
				}
				catch (Exception e)
				{
					_loger("Internal error", e);
				}
				if (reconnect)
					model = Reconnect (model);
			}
		}

		void Enqueue (Action<IModel> act)
		{
			lock (_queue)
			{
				_queue.Enqueue (act);
			}
			_event.Set ();
		}

		public Task EnqueueTask (Action<IModel> act)
		{
			var tcs = new TaskCompletionSource<int> ();
			Enqueue (model =>
			{
				try
				{
					act (model);
				}
				catch (Exception e)
				{
					tcs.SetException (e);
					throw;
				}
				tcs.SetResult (0);
			});
			return tcs.Task;
		}
	}
}
