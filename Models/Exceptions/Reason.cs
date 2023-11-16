namespace ContainerRunner.Models.Exceptions;

public class Reason
{
    public Reason(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}