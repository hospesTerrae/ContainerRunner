using ContainerRunner.Models;
using Docker.DotNet.Models;

namespace ContainerRunner.Services.DockerApi;

public interface IDockerApiService
{
    Task<IList<ContainerListResponse>> GetContainers(CancellationToken cancellationToken = default);
    Task<bool> CreateContainer(Image image, CancellationToken cancellationToken = default);
    Task<bool> DeleteContainer(Container container, CancellationToken cancellationToken = default);
}