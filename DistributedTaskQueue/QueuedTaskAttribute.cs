using System;

namespace DistributedTaskQueue
{
	[AttributeUsage(AttributeTargets.Class)]
	public class QueuedTaskAttribute : Attribute
	{
		public string Queue { get; set; }

		public QueuedTaskAttribute(string queue = null)
		{
			Queue = queue;
		}
	}
}
