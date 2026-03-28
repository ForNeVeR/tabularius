namespace Tabularius.Interop;

public class HledgerException(string message, string stackTrace) : Exception
{
    public override string Message => message;
    public override string StackTrace => stackTrace;
}
