namespace ContainerRunner.Models;

public class Reason
{
    public string Message { get; set; }

    public Reason(string message)
    {
        Message = message;
    }
}