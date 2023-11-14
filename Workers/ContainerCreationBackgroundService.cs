using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;

namespace ContainerRunner.Workers;

public class ContainerCreationBackgroundService : BackgroundService
{
    private readonly IBackgroundQueue<Image> _queue;
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerCreationBackgroundService> _logger;

    public ContainerCreationBackgroundService(IBackgroundQueue<Image> queue,
        IDockerApiService dockerApiService, ILogger<ContainerCreationBackgroundService> logger)
    {
        _queue = queue;
        _dockerApiService = dockerApiService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = nameof(ContainerCreationBackgroundService);
        _logger.Log(LogLevel.Debug, $"{serviceName} is running");

        await ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var image = await _queue.DequeueAsync(stoppingToken);
                _logger.Log(LogLevel.Information, $"Starting container from image [{image.Fullname}]");
                await _dockerApiService.RunContainerFromImage(image, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
    }
}