using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;
using ContainerRunner.Workers.Background;

namespace ContainerRunner.Workers;

public class ContainerDestroyingBackgroundService : BackgroundService
{
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerDestroyingBackgroundService> _logger;
    private readonly IContainerStateService _stateService;
    private readonly int _parallelismDegree = 3;
    private readonly IContainerWorker<Container>[] _workers;

    private readonly IBackgroundQueue<Container> _queue;

    public ContainerDestroyingBackgroundService(
        IDockerApiService dockerApiService, ILogger<ContainerDestroyingBackgroundService> logger,
        IContainerStateService stateService, IBackgroundQueue<Container> queue)
    {
        _dockerApiService = dockerApiService;
        _logger = logger;
        _stateService = stateService;
        _workers = new IContainerWorker<Container>[_parallelismDegree];
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = nameof(ContainerDestroyingBackgroundService);
        _logger.Log(LogLevel.Debug, $"{serviceName} is running");

        await ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await foreach (var image in _queue.DequeueAsync(stoppingToken))
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

    private IContainerWorker<Container> GetOrCreateWorker(int index,
        CancellationToken processingCancellationToken = default)
    {
        if (_workers[index] == null)
            _workers[index] =
                DownWorker.CreateAndStartProcessing(index, processingCancellationToken, _stateService,
                    _dockerApiService, _logger);

        return _workers[index];
    }
}