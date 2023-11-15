namespace ContainerRunner.Services.Queue;

public interface IBackgroundQueue<T>
{
    ValueTask Enqueue(T item, CancellationToken cancellationToken);
    IAsyncEnumerable<T> DequeueAsync(CancellationToken cancellationToken);
}