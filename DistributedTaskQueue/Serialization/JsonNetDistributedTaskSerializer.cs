using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace DistributedTaskQueue.Serialization
{
	public class JsonNetDistributedTaskSerializer : IDistributedTaskSerializer
	{
		private readonly JsonSerializer _serializer;
		private static readonly Encoding Encoding = new UTF8Encoding(false);

		public JsonNetDistributedTaskSerializer(JsonSerializer serializer)
		{
			_serializer = serializer;
		}

		public JsonNetDistributedTaskSerializer():this(new JsonSerializer())
		{
			
		}

		public byte[] Serialize(object obj)
		{
			var ms = new MemoryStream();
			using (var wr = new StreamWriter(ms, Encoding))
				_serializer.Serialize(wr, obj);
			return ms.ToArray();
		}

		public T Deserialize<T>(byte[] data)
		{
			return _serializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(data), Encoding)));
		}
	}
}