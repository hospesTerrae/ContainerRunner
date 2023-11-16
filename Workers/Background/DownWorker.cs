using System.Threading.Channels;
using ContainerRunner.Enums;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.State;

namespace ContainerRunner.Workers.Background;

public class DownWorker : IContainerWorker<Container>
{
    private readonly IDockerApiService _dockerApiService;

    private readonly Channel<Container> _internalQueue = Channel.CreateUnbounded<Container>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private readonly ILogger _logger;
    private readonly string _processorId;
    private readonly IContainerStateService _stateService;
    private Task? _processingTask;

    private DownWorker(int processorId, IContainerStateService stateService, IDockerApiService dockerApiService,
        ILogger logger)
    {
        _processorId = GetWorkerName(processorId);
        _stateService = stateService;
        _dockerApiService = dockerApiService;
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
        CancellationToken processingCancellationToken, IContainerStateService stateService,
        IDockerApiService dockerApiService, ILogger logger)
    {
        logger.Log(LogLevel.Debug, $"Destroying processor [{id}] instance created");
        var instance = new DownWorker(id, stateService, dockerApiService, logger);
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
                    _stateService.UpdateStatus(container.Id, ContainerState.Stopping);
                    _logger.Log(LogLevel.Information, $"Stopping container [{container.Id} by [{_processorId}]");
                    await _dockerApiService.StopRunningContainerAsync(container, cancellationToken);
                }
            }, cancellationToken);
    }
}