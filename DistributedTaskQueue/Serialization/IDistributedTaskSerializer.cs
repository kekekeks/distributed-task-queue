namespace DistributedTaskQueue
{
	public interface IDistributedTaskSerializer
	{
		byte[] Serialize(object obj);
		T Deserialize<T>(byte[] data);
	}
}