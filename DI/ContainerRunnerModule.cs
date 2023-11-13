using ContainerRunner.Services.DockerApi;

namespace ContainerRunner.DI;

public static class ContainerRunnerModule
{
    public static IServiceCollection RegisterContainerRunnerModule(this IServiceCollection services)
    {
        services.AddScoped<IDockerApiService, DockerApiService>();

        return services;
    }
}