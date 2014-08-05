using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedTaskQueue;
using DistributedTaskQueue.Messaging;
using Xunit;

namespace Tests
{
    public class ManagerTests
    {
		[QueuedTask("test")]
	    public class Message
	    {
			public string Data { get; set; }
	    }

		[Fact]
	    public void MessagingWorks()
		{
			var exps = new List<Exception>();
			var bus = new InProcMessageBus((_, e) => exps.Add(e));
			var mgr = new TaskQueueManager(bus);
			string res = null;
			mgr.Subscribe<Message>(async m => { res = m.Data; });
			mgr.Enqueue(new Message() {Data = "123"});

			bus.WaitForPending().Wait();
			Assert.Empty(exps);
			Assert.Equal("123", res);
		}


    }
}
