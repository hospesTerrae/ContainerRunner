namespace ContainerRunner.Configuration;

public class CreationBackgroundServiceSettings
{
    public const string Key = "CreationBackgroundService";

    public int ParallelismDegree { get; set; } = 3;
}