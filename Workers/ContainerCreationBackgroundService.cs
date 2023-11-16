using ContainerRunner.Configuration;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Workers.Background;
using Microsoft.Extensions.Options;

namespace ContainerRunner.Workers;

public class ContainerCreationBackgroundService : BackgroundService
{
    private readonly IBackgroundQueue<Image> _internalQueue;
    private readonly ILogger<ContainerCreationBackgroundService> _logger;
    private readonly int _parallelismDegree = 3;
    private readonly IContainerWorker<Image>[] _workers;
    private readonly IServiceProvider _services;

    public ContainerCreationBackgroundService(IOptions<CreationBackgroundServiceSettings> settings,
        ILogger<ContainerCreationBackgroundService> logger, IBackgroundQueue<Image> queue, IServiceProvider services)
    {
        _parallelismDegree = settings.Value.ParallelismDegree;
        _logger = logger;
        _workers = new IContainerWorker<Image>[_parallelismDegree];
        _internalQueue = queue;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = nameof(ContainerCreationBackgroundService);
        _logger.Log(LogLevel.Debug, $"{serviceName} is running");

        await ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await foreach (var image in _internalQueue.DequeueAsync(stoppingToken))
                {
                    _logger.Log(LogLevel.Information, $"Dequeued {image.Fullname} to start");
                    i %= _parallelismDegree; // round robin

                    var worker = GetOrCreateWorker(i, stoppingToken);
                    await worker.ScheduleWork(image);

                    i++;
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
            }
    }

    private IContainerWorker<Image> GetOrCreateWorker(int index,
        CancellationToken processingCancellationToken = default)
    {
        if (_workers[index] == null)
            _workers[index] =
                UpWorker.CreateAndStartProcessing(index, processingCancellationToken, _services, _logger);

        return _workers[index];
    }
}