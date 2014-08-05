using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DistributedTaskQueue.Logging
{
	public class NullFaultedTaskLogger : IFaultedTaskLogger
	{
		public void LogException(Exception e, string queue, object task)
		{
			
		}
	}
}
