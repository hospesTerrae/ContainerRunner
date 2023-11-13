namespace ContainerRunner.Models;

public record Image
{
    public string Name { get; set; }
    public string Tag { get; set; }

    public string Fullname => $"{Name.ToLower()}:{Tag.ToLower()}";
}