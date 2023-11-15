namespace ContainerRunner.Workers.Background;

public interface IContainerWorker<T>
{
    Task ScheduleWork(T item);
}