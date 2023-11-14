namespace ContainerRunner.Services.Queue;

public interface IBackgroundQueue<T>
{
    ValueTask Enqueue(T item, CancellationToken cancellationToken);
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
}