using ContainerRunner.Exceptions;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;

namespace ContainerRunner.Workers;

public class ContainerDestroyingBackgroundService : BackgroundService
{
    private readonly IBackgroundQueue<Container> _queue;
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerDestroyingBackgroundService> _logger;

    public ContainerDestroyingBackgroundService(IBackgroundQueue<Container> queue,
        IDockerApiService dockerApiService, ILogger<ContainerDestroyingBackgroundService> logger)
    {
        _queue = queue;
        _dockerApiService = dockerApiService;
        _logger = logger;
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
        {
            try
            {
                var container = await _queue.DequeueAsync(stoppingToken);
                _logger.Log(LogLevel.Information, $"Stopping container [{container.Id}]");
                await _dockerApiService.DeleteContainer(container, stoppingToken);
            }
            catch (ContainerNotFoundException e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
        }
    }
}