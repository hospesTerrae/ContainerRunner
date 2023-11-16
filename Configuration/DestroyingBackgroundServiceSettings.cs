namespace ContainerRunner.Configuration;

public class DestroyingBackgroundServiceSettings
{
    public const string Key = "DestroyingBackgroundService";

    public int ParallelismDegree { get; set; } = 3;
}