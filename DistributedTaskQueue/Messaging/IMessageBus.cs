using System;
using System.Threading.Tasks;

namespace DistributedTaskQueue
{
    public interface IMessageBus
    {
	    Task Publish(string queue, byte[] data);
	    Task Subscribe(string queue, Func<byte[], Task> handler);
    }
}
