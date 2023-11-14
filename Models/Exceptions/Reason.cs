namespace ContainerRunner.Models.Exceptions;

public class Reason
{
    public string Message { get; set; }

    public Reason(string message)
    {
        Message = message;
    }
}