distributed-task-queue
======================

Wrapper around RabbitMQ to simplify distributing tasks. It manages connectivity (reconnects), threading and queues (it does NOT follow that pub-sub concept of most AMQP-related libraries, so everithing is stored in named durable queues with persistent messages) for you.


Usage
-----

```csharp

[QueuedTask("queue-name")]
public class MyTask
{
   public string Data { get; set; }
}


var bus = new RabbitMqMessageBus("rmq://guest:guest@localhost/vhost");
var mgr = new TaskQueueManager(bus);
await mgr.Subscribe<MyTask>(async m => Console.WriteLine(m.Data);
await mgr.Enqueue(new MyTask() {Data = "123"});


```
