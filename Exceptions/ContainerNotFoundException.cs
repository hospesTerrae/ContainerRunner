namespace ContainerRunner.Exceptions;

public class ContainerNotFoundException : Exception
{
    public ContainerNotFoundException(string message) : base(message)
    {
        
    }
}