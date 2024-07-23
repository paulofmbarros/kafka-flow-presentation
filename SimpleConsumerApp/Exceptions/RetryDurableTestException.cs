namespace SimpleConsumerApp.Exceptions;

public class RetryDurableTestException : Exception
{
    public RetryDurableTestException(string message) : base(message)
    {
    }
}