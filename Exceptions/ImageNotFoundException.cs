namespace ContainerRunner.Exceptions;

public class ImageNotFoundException : Exception
{
    public ImageNotFoundException(string message) : base(message)
    {
        
    }
}