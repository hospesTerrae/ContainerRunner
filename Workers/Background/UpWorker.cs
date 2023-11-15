using System.Threading.Channels;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;

namespace ContainerRunner.Workers.Background;

public class UpWorker : IContainerWorker<Image>
{
    private readonly int _processorId;
    private Task? _processingTask;
    private readonly IDockerApiService _dockerApiService;

    private readonly Channel<Image> _internalQueue = Channel.CreateUnbounded<Image>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    private readonly ILogger _logger;

    private UpWorker(int id, IDockerApiService dockerApiService, ILogger logger)
    {
        _processorId = id;
        _dockerApiService = dockerApiService;
        _logger = logger;
    }

    public async Task ScheduleWork(Image item)
    {
        _logger.Log(LogLevel.Debug, $"Creating processor [{_processorId} queued image [{item.Fullname}]");
        await _internalQueue.Writer.WriteAsync(item);
    }

    public static IContainerWorker<Image> CreateAndStartProcessing(int id,
        CancellationToken processingCancellationToken, IDockerApiService dockerApiService, ILogger logger)
    {
        logger.Log(LogLevel.Debug, $"Creating processor [{id}] instance created");
        var instance = new UpWorker(id, dockerApiService, logger);
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
                    _logger.Log(LogLevel.Debug,
                        $"Creating processor [{_processorId} executing image [{image.Fullname}]");
                    await _dockerApiService.RunContainerFromImage(image, cancellationToken);
                }
            }, cancellationToken);
    }
}