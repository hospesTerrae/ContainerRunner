namespace ContainerRunner.Services.Queue;

public interface IBackgroundQueue<T>
{
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> DequeueAsync(CancellationToken cancellationToken);
}