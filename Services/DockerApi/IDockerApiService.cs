using ContainerRunner.Models;

namespace ContainerRunner.Services.DockerApi;

public interface IDockerApiService
{
    Task<bool> RunContainerFromImageAsync(Image image, CancellationToken cancellationToken = default);
    Task<bool> StopRunningContainerAsync(Container container, CancellationToken cancellationToken = default);
}