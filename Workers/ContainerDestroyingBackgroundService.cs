using ContainerRunner.Configuration;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;
using ContainerRunner.Workers.Background;
using Microsoft.Extensions.Options;

namespace ContainerRunner.Workers;

public class ContainerDestroyingBackgroundService : BackgroundService
{
    private readonly IDockerApiService _dockerApiService;
    private readonly ILogger<ContainerDestroyingBackgroundService> _logger;
    private readonly int _parallelismDegree = 3;

    private readonly IBackgroundQueue<Container> _queue;
    private readonly IContainerStateService _stateService;
    private readonly IContainerWorker<Container>[] _workers;
    private readonly IServiceProvider _services;

    public ContainerDestroyingBackgroundService(IOptions<DestroyingBackgroundServiceSettings> options, ILogger<ContainerDestroyingBackgroundService> logger, IBackgroundQueue<Container> queue,
        IServiceProvider services)
    {
        _parallelismDegree = options.Value.ParallelismDegree;
        _logger = logger;
        _workers = new IContainerWorker<Container>[_parallelismDegree];
        _queue = queue;
        _services = services;
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
                await foreach (var container in _queue.DequeueAsync(stoppingToken))
                {
                    _logger.Log(LogLevel.Information, $"Dequeued {container.Id} to stop");
                    i %= _parallelismDegree; // round robin

                    var worker = GetOrCreateWorker(i);
                    await worker.ScheduleWork(container);

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
                DownWorker.CreateAndStartProcessing(index, processingCancellationToken, _services, _logger);

        return _workers[index];
    }
}