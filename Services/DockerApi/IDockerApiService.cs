using ContainerRunner.Models;

namespace ContainerRunner.Services.DockerApi;

public interface IDockerApiService
{
    Task<bool> RunContainerFromImage(Image image, CancellationToken cancellationToken = default);
    Task<bool> StopRunningContainer(Container container, CancellationToken cancellationToken = default);
}