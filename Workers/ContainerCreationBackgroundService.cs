using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Workers.Background;

namespace ContainerRunner.Workers;

public class ContainerCreationBackgroundService : BackgroundService
{
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerCreationBackgroundService> _logger;
    private readonly IContainerWorker<Image>[] _workers;
    private readonly int _parallelismDegree = 3;

    private readonly IBackgroundQueue<Image> _internalQueue;

    public ContainerCreationBackgroundService(IDockerApiService dockerApiService,
        ILogger<ContainerCreationBackgroundService> logger, IBackgroundQueue<Image> queue)
    {
        _dockerApiService = dockerApiService;
        _logger = logger;
        _workers = new IContainerWorker<Image>[_parallelismDegree];
        _internalQueue = queue;
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
                    i %= _parallelismDegree;

                    var worker = GetOrCreateWorker(i);
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
                UpWorker.CreateAndStartProcessing(index, processingCancellationToken, _dockerApiService, _logger);

        return _workers[index];
    }
}