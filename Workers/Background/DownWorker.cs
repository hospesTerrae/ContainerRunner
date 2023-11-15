using System.Threading.Channels;
using ContainerRunner.Enums;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.State;

namespace ContainerRunner.Workers.Background;

public class DownWorker : IContainerWorker<Container>
{
    private readonly IContainerStateService _stateService;
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger _logger;
    private Task? _processingTask;
    private readonly int _processorId;

    private readonly Channel<Container> _internalQueue = Channel.CreateUnbounded<Container>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private DownWorker(int processorId, IContainerStateService stateService, IDockerApiService dockerApiService,
        ILogger logger)
    {
        _processorId = processorId;
        _stateService = stateService;
        _dockerApiService = dockerApiService;
        _logger = logger;
    }

    public async Task ScheduleWork(Container item)
    {
        _logger.Log(LogLevel.Debug, $"Destroying processor [{_processorId} queued container [{item.Id}]");
        await _internalQueue.Writer.WriteAsync(item);
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
                    _logger.Log(LogLevel.Debug,
                        $"Destroying processor [{_processorId} executing container [{container}]");

                    _stateService.UpdateStatus(container.Id, ContainerState.Stopping);
                    _logger.Log(LogLevel.Information, $"Stopping container [{container.Id}]");
                    await _dockerApiService.StopRunningContainer(container, cancellationToken);
                }
            }, cancellationToken);
    }
}