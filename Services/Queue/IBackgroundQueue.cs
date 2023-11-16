namespace ContainerRunner.Services.Queue;

public interface IBackgroundQueue<T>
{
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken);
    IAsyncEnumerable<T> DequeueAsync(CancellationToken cancellationToken);
}