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

    public IAsyncEnumerable<T> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public abstract void UpdateStatusAfterDequeued(T item);
}