using System.Threading.Channels;

namespace ContainerRunner.Services.Queue;

public abstract class AbstractBackgroundQueue<T> : IBackgroundQueue<T>
{
    private readonly Channel<T> _channel;
    private readonly ILogger _logger;

    public AbstractBackgroundQueue(UnboundedChannelOptions? options, ILogger logger)
    {
        _channel = Channel.CreateUnbounded<T>(options);
        _logger = logger;
    }

    public async ValueTask Enqueue(T item, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Queued {item}");
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
    {
        var workingItem = await _channel.Reader.ReadAsync(cancellationToken);

        _logger.Log(LogLevel.Debug, $"Dequeued {workingItem}");
        return workingItem;
    }
}