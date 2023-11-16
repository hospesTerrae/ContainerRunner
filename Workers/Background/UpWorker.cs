using System.Threading.Channels;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;

namespace ContainerRunner.Workers.Background;

public class UpWorker : IContainerWorker<Image>
{
    private readonly Channel<Image> _internalQueue = Channel.CreateUnbounded<Image>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private readonly ILogger _logger;
    private readonly string _processorId;
    private Task? _processingTask;
    private readonly IServiceProvider _services;

    private UpWorker(int id, IServiceProvider services, ILogger logger)
    {
        _processorId = GetWorkerName(id);
        _logger = logger;
        _services = services;
    }

    public async Task ScheduleWork(Image item)
    {
        _logger.Log(LogLevel.Debug, $"Schedule: [{item.Fullname}] is queued to [{_processorId}]");
        await _internalQueue.Writer.WriteAsync(item);
    }

    public string GetWorkerName(int id)
    {
        return $"up-{id}";
    }

    public static IContainerWorker<Image> CreateAndStartProcessing(int id,
        CancellationToken processingCancellationToken, IServiceProvider services, ILogger logger)
    {
        logger.Log(LogLevel.Debug, $"Creating processor [{id}] instance created");
        var instance = new UpWorker(id, services, logger);
        instance.StartProcessing(processingCancellationToken);

        return instance;
    }

    private void StartProcessing(CancellationToken cancellationToken)
    {
        _processingTask = Task.Factory.StartNew(
            async () =>
            {
                await foreach (var image in _internalQueue.Reader.ReadAllAsync(cancellationToken))
                {
                    _logger.Log(LogLevel.Information,
                        $"Creating container from image [{image.Fullname} by [{_processorId}]");

                    using var scope = _services.CreateScope();
                    var dockerApiScoped = scope.ServiceProvider.GetRequiredService<IDockerApiService>();
                    await dockerApiScoped.RunContainerFromImageAsync(image, cancellationToken);
                }
            }, cancellationToken);
    }
}