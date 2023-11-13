namespace ContainerRunner.Models;

public record ContainerStatus
{
    public string ContainerId { get; set; }
    public string Image { get; set; }
    public string Status { get; set; }
    public string State { get; set; }
    public string ContainerName { get; set; }
}