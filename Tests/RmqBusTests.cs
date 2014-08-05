using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DistributedTaskQueue.RabbitMq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Tests
{
	public class RmqBusTests
	{
		void SendApi(string url, HttpMethod method = null, string data = null)
		{
			var client = new HttpClient();
			var msg = new HttpRequestMessage(method ?? HttpMethod.Get, "http://localhost:15672/api/" + url);
			msg.Headers.Authorization =
				new AuthenticationHeaderValue(
					"Basic",
					Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest")));
			msg.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(data ?? ""));
			msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			var res = Encoding.UTF8.GetString(client.SendAsync(msg).Result.Content.ReadAsByteArrayAsync().Result);
			if (res != "")
				throw new Exception(res);
		}

		private const string vhost = "dtaskqueuetest";

		public RmqBusTests()
		{
			try
			{
				SendApi("vhosts/" + vhost, HttpMethod.Delete);
			}
			catch
			{
			}
			SendApi("vhosts/" + vhost, HttpMethod.Put);
			SendApi("permissions/" + vhost + "/guest", HttpMethod.Put, JObject.FromObject(new
			{
				configure = ".*",
				write = ".*",
				read = ".*"
			}).ToString());

		}

		[Fact]
		public void CheckMessaging()
		{
			var bus = new RabbitMqMessageBus("rmq://guest:guest@localhost/" + vhost);
			var rnd = new byte[100];
			new Random().NextBytes(rnd);
			bus.Publish("test", rnd).Wait();
			byte[] res = null;
			bus.Subscribe("test", async x => { res = x; }).Wait();
			Thread.Sleep(1000);
			Assert.Equal(rnd, res);
		}
	}
}
