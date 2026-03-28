using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.Services;

public sealed class NullLogger<T> : ILogger<T>
{
    public static readonly NullLogger<T> Instance = new();

    private NullLogger()
    {
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}
