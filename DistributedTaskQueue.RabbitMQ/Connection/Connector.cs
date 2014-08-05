using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DistributedTaskQueue.RabbitMQ.Connection
{
	static class Connector
	{

		public static IModel Connect (Uri uri)
		{
			var pairs = uri.UserInfo.Split(':');
			var factory = new ConnectionFactory
			{
				HostName = uri.Host,
				UserName = string.IsNullOrEmpty(pairs[0]) ? "guest" : pairs[0],
				Password = (pairs.Length < 2 || string.IsNullOrEmpty(pairs[1])) ? "guest" : pairs[1],
				Port = uri.Port == 0 ? 5672 : uri.Port,
				VirtualHost = uri.AbsolutePath.Substring(1)
			};
			var conn = factory.CreateConnection ();
			try
			{
				var mdl = conn.CreateModel ();
				conn.AutoClose = true;
				mdl.BasicQos (0, 5, false);
				return mdl;
			}
			catch
			{
				conn.Dispose ();
				throw;
			}
		}

		public static async Task<IModel> Reconnect (IModel oldConnection, Uri uri, int cooldown = 10000)
		{
			for (;;)
			{
				try
				{
					oldConnection.Dispose ();
				}
				catch
				{
				}
				try
				{
					return Connect (uri);
				}
				catch (Exception e)
				{
					Console.WriteLine (e);
				}
				await Task.Delay (cooldown);
			}
		}
	}
}
