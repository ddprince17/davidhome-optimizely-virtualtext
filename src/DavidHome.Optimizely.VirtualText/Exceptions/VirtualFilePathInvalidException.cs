namespace DavidHome.Optimizely.VirtualText.Exceptions;

public class VirtualFilePathInvalidException : Exception
{
    public VirtualFilePathInvalidException()
    {
    }

    public VirtualFilePathInvalidException(string? message) : base(message)
    {
    }

    public VirtualFilePathInvalidException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}