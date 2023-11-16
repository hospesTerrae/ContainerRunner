namespace ContainerRunner.Configuration;

public class DockerApiServiceSettings
{
    public const string Key = "DockerApiService";

    public string SocketAddr { get; set; } = "unix:///var/run/docker.sock";
}