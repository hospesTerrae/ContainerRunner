namespace ContainerRunner.Workers.Background;

public interface IContainerWorker<T>
{
    Task ScheduleWork(T item);

    string GetWorkerName(int id);
}