using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Aliyun;

/// <summary>
/// Instances of this type are single-threaded, generally only updated on a background
/// timer thread. An exception is <see cref="IsIncluded(LogEvent)"/>, which may be called
/// concurrently but performs no synchronization.
/// </summary>
public sealed class ControlledLevelSwitch
{
    // If non-null, then background level checks will be performed; set either through the constructor
    // or in response to a level specification from the server. Never set to null after being made non-null.
    private LoggingLevelSwitch? _controlledSwitch;
    private LogEventLevel? _originalLevel;

    public ControlledLevelSwitch(LoggingLevelSwitch? controlledSwitch = null)
    {
        _controlledSwitch = controlledSwitch;
    }

    public bool IsActive => _controlledSwitch != null;

    public bool IsIncluded(LogEvent evt)
    {
        // Concurrent, but not synchronized.
        var controlledSwitch = _controlledSwitch;
        return controlledSwitch == null ||
               (int)controlledSwitch.MinimumLevel <= (int)evt.Level;
    }

    public void Update(LogEventLevel? minimumAcceptedLevel)
    {
        if (minimumAcceptedLevel == null)
        {
            if (_controlledSwitch != null && _originalLevel.HasValue)
                _controlledSwitch.MinimumLevel = _originalLevel.Value;

            return;
        }

        if (_controlledSwitch == null)
        {
            // The server is controlling the logging level, but not the overall logger. Hence, if the server
            // stops controlling the level, the switch should become transparent.
            _originalLevel = LevelAlias.Minimum;
            _controlledSwitch = new LoggingLevelSwitch(minimumAcceptedLevel.Value);
            return;
        }

        _originalLevel ??= _controlledSwitch.MinimumLevel;

        _controlledSwitch.MinimumLevel = minimumAcceptedLevel.Value;
    }
}