using ContainerRunner.Enums;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;

namespace ContainerRunner.Workers;

public class ContainerDestroyingBackgroundService : BackgroundService
{
    private readonly IBackgroundQueue<Container> _queue;
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerDestroyingBackgroundService> _logger;
    private readonly IContainerStateService _stateService;

    public ContainerDestroyingBackgroundService(IBackgroundQueue<Container> queue,
        IDockerApiService dockerApiService, ILogger<ContainerDestroyingBackgroundService> logger,
        IContainerStateService stateService)
    {
        _queue = queue;
        _dockerApiService = dockerApiService;
        _logger = logger;
        _stateService = stateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = nameof(ContainerDestroyingBackgroundService);
        _logger.Log(LogLevel.Debug, $"{serviceName} is running");

        await ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var container = await _queue.DequeueAsync(stoppingToken);
                if (IsStillRunningContainer(container))
                {
                    _stateService.UpdateStatus(container.Id, ContainerState.Stopping);
                    _logger.Log(LogLevel.Information, $"Stopping container [{container.Id}]");
                    await _dockerApiService.StopRunningContainer(container, stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
    }

    private bool IsStillRunningContainer(Container container)
    {
        // preventing multiple stop requests processing via status
        return _stateService.GetStatus(container.Id) == ContainerState.EnqueuedToStop;
    }
}