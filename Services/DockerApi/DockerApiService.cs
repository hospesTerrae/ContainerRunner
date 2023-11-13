using ContainerRunner.Exceptions;
using ContainerRunner.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace ContainerRunner.Services.DockerApi;

public class DockerApiService : IDockerApiService
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerApiService> _logger;

    public DockerApiService(ILogger<DockerApiService> logger)
    {
        _client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        _logger = logger;
    }

    public async Task<IList<ContainerListResponse>> GetContainers(CancellationToken cancellationToken)
    {
        var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
        {
            Limit = 10
        }, cancellationToken);

        return containers;
    }

    public async Task<bool> CreateContainer(Image image, CancellationToken cancellationToken)
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

        try
        {
            var started = await StartContainer(containerId, cancellationToken);
            return started;
        }
        catch (DockerApiException e)
        {
            throw new ContainerNotFoundException(e.Message);
        }
    }


    public async Task<bool> DeleteContainer(Container container, CancellationToken cancellationToken)
    {
        return await _client.Containers.StopContainerAsync(container.Id, new ContainerStopParameters
        {
            WaitBeforeKillSeconds = 30
        }, cancellationToken);
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
        _logger.Log(LogLevel.Debug, $"Fetching status - {e.Status}");
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