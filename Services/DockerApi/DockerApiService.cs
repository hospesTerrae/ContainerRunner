using ContainerRunner.Configuration;
using ContainerRunner.Exceptions;
using ContainerRunner.Models;
using ContainerRunner.Services.State;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using ContainerState = ContainerRunner.Enums.ContainerState;

namespace ContainerRunner.Services.DockerApi;

public class DockerApiService : IDockerApiService
{
    private readonly DockerClient _client;
    private readonly IContainerStateService _containerStateService;
    private readonly ILogger<DockerApiService> _logger;

    public DockerApiService(IOptions<DockerApiServiceSettings> options, ILogger<DockerApiService> logger,
        IContainerStateService containerStateService)
    {
        var uri = options.Value.SocketAddr;
        _client = new DockerClientConfiguration(new Uri(uri)).CreateClient();
        _logger = logger;
        _containerStateService = containerStateService;
    }

    public async Task<bool> RunContainerFromImageAsync(Image image, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, $"Creating container from image [{image.Fullname}]");
        try
        {
            await FetchImageAsync(image, cancellationToken);
        }
        catch (DockerApiException e)
        {
            throw new ImageNotFoundException(e.Message);
        }

        var containerId = await CreateContainerInternalAsync(image, cancellationToken);
        _containerStateService.UpdateStatus(containerId, ContainerState.Starting);

        try
        {
            var started = await StartContainerAsync(containerId, cancellationToken);
            _containerStateService.UpdateStatus(containerId, ContainerState.Running);
            _logger.Log(LogLevel.Information, $"Container created [{containerId}]");
            return started;
        }
        catch (DockerApiException e)
        {
            throw new ContainerNotFoundException(e.Message);
        }
    }

    public async Task<bool> StopRunningContainerAsync(Container container, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, $"Stopping container {container.Id}");

        var stoppingResult = await _client.Containers.StopContainerAsync(container.Id, new ContainerStopParameters
        {
            WaitBeforeKillSeconds = 30
        }, cancellationToken);

        _logger.Log(LogLevel.Information, $"Container [{container.Id}] was stopped [{stoppingResult}]");
        _containerStateService.UpdateStatus(container.Id, ContainerState.Stopped);

        return stoppingResult;
    }

    private async Task FetchImageAsync(Image image, CancellationToken cancellationToken)
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

    private async Task<string> CreateContainerInternalAsync(Image image, CancellationToken cancellationToken)
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

    private async Task<bool> StartContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Debug, $"Starting container {containerId}");

        var started = await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(),
            cancellationToken);

        _logger.Log(LogLevel.Debug, $"Container {containerId} started {started}");

        return started;
    }
}