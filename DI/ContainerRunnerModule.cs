using ContainerRunner.Configuration;
using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;
using ContainerRunner.Workers;

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

    public static IServiceCollection RegisterConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CreationBackgroundServiceSettings>(
            configuration.GetSection(CreationBackgroundServiceSettings.Key));

        services.Configure<DestroyingBackgroundServiceSettings>(
            configuration.GetSection(DestroyingBackgroundServiceSettings.Key));

        services.Configure<DockerApiServiceSettings>(
            configuration.GetSection(DockerApiServiceSettings.Key));

        return services;
    }
}