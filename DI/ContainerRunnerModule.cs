using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;
using ContainerRunner.Workers;
using ContainerRunner.Workers.Background;

namespace ContainerRunner.DI;

public static class ContainerRunnerModule
{
    public static IServiceCollection RegisterContainerRunnerModule(this IServiceCollection services)
    {
        services.AddSingleton<IDockerApiService, DockerApiService>();
        services.AddSingleton<IContainerStateService, ContainerStateService>();
        services.AddSingleton<IBackgroundQueue<Image>, UpQueue>();
        services.AddSingleton<IBackgroundQueue<Container>, ShutdownQueue>();
        
        services.AddHostedService<ContainerCreationBackgroundService>();
        services.AddHostedService<ContainerDestroyingBackgroundService>();

        return services;
    }
}