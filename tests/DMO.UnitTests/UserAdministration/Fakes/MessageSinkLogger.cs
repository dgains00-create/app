using Microsoft.Extensions.Logging;

namespace DMO.UnitTests.UserAdministration.Fakes;

/// <summary>
/// Collects every formatted log message for assertions (e.g. proving that provider secrets
/// never reach the log surface).
/// </summary>
public sealed class MessageSinkLogger<T> : ILogger<T>
{
    private readonly List<string> _messages = [];

    /// <summary>Formatted messages, in order.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>All messages joined, for substring assertions.</summary>
    public string Joined => string.Join(Environment.NewLine, _messages);

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (exception is not null)
        {
            message += $" [exception: {exception.GetType().Name}]";
        }

        _messages.Add(message);
    }
}