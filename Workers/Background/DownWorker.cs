using System.Threading.Channels;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;

namespace ContainerRunner.Workers.Background;

public class DownWorker : IContainerWorker<Container>
{
    private readonly Channel<Container> _internalQueue = Channel.CreateUnbounded<Container>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private readonly ILogger _logger;
    private readonly string _processorId;
    private Task? _processingTask;
    private readonly IServiceProvider _services;

    private DownWorker(int processorId, IServiceProvider services,
        ILogger logger)
    {
        _processorId = GetWorkerName(processorId);
        _services = services;
        _logger = logger;
    }

    public async Task ScheduleWork(Container item)
    {
        _logger.Log(LogLevel.Debug, $"Schedule: [{item.Id}] is queued to [{_processorId}]");
        await _internalQueue.Writer.WriteAsync(item);
    }

    public string GetWorkerName(int id)
    {
        return $"down-{id}";
    }

    public static IContainerWorker<Container> CreateAndStartProcessing(int id,
        CancellationToken processingCancellationToken, IServiceProvider serviceProvider, ILogger logger)
    {
        logger.Log(LogLevel.Debug, $"Destroying processor [{id}] instance created");
        var instance = new DownWorker(id, serviceProvider, logger);
        instance.StartProcessing(processingCancellationToken);

        return instance;
    }

    private void StartProcessing(CancellationToken cancellationToken)
    {
        _processingTask = Task.Factory.StartNew(
            async () =>
            {
                await foreach (var container in _internalQueue.Reader.ReadAllAsync(cancellationToken))
                {
                    using var scope = _services.CreateScope();
                    var dockerApiService = scope.ServiceProvider.GetRequiredService<IDockerApiService>();

                    _logger.Log(LogLevel.Information, $"Stopping container [{container.Id} by [{_processorId}]");
                    await dockerApiService.StopRunningContainerAsync(container, cancellationToken);
                }
            }, cancellationToken);
    }
}