using ContainerRunner.Exceptions;
using ContainerRunner.Models;
using ContainerRunner.Services.State;
using Docker.DotNet;
using Docker.DotNet.Models;
using ContainerState = ContainerRunner.Enums.ContainerState;

namespace ContainerRunner.Services.DockerApi;

public class DockerApiService : IDockerApiService
{
    private readonly DockerClient _client;
    private readonly IContainerStateService _containerStateService;
    private readonly ILogger<DockerApiService> _logger;

    public DockerApiService(ILogger<DockerApiService> logger, IContainerStateService containerStateService)
    {
        _client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        _logger = logger;
        _containerStateService = containerStateService;
    }

    public async Task<bool> RunContainerFromImage(Image image, CancellationToken cancellationToken)
    {
        try
        {
            await FetchImage(image, cancellationToken);
        }
        catch (DockerApiException e)
        {
            throw new ImageNotFoundException(e.Message);
        }

        var containerId = await CreateContainerInternal(image, cancellationToken);
        _containerStateService.UpdateStatus(containerId, ContainerState.Starting);

        try
        {
            var started = await StartContainer(containerId, cancellationToken);
            _containerStateService.UpdateStatus(containerId, ContainerState.Running);
            return started;
        }
        catch (DockerApiException e)
        {
            throw new ContainerNotFoundException(e.Message);
        }
    }

    public async Task<bool> StopRunningContainer(Container container, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Stopping container {container.Id}");

        var stoppingResult = await _client.Containers.StopContainerAsync(container.Id, new ContainerStopParameters
        {
            WaitBeforeKillSeconds = 30
        }, cancellationToken);

        _logger.Log(LogLevel.Debug, $"Container [{container.Id}] was stopped [{stoppingResult}]");
        _containerStateService.UpdateStatus(container.Id, ContainerState.Stopped);

        return stoppingResult;
    }

    private async Task FetchImage(Image image, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Fetching [{image.Fullname}]");

        var progress = new Progress<JSONMessage>();
        progress.ProgressChanged += DownloadProgressChanged;
        await _client.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = image.Name,
            Tag = image.Tag
        }, null, progress, cancellationToken);

        _logger.Log(LogLevel.Debug, $"Fetch [{image.Fullname}] finished");
    }

    private void DownloadProgressChanged(object? sender, JSONMessage e)
    {
        _logger.Log(LogLevel.Trace, $"Fetching status - {e.Status}");
    }

    private async Task<string> CreateContainerInternal(Image image, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Creating container [{image.Fullname}]");

        var containerParameters = new CreateContainerParameters
        {
            Image = image.Name
        };

        var container = await _client.Containers.CreateContainerAsync(containerParameters, cancellationToken);

        _logger.Log(LogLevel.Debug, $"Container created {container.ID}");
        return container.ID;
    }

    private async Task<bool> StartContainer(string containerId, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Starting container {containerId}");

        var started = await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(),
            cancellationToken);

        _logger.Log(LogLevel.Debug, $"Container {containerId} started {started}");

        return started;
    }
}