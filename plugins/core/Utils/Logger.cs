// utils/Log.cs
using Microsoft.Extensions.Logging;

namespace RetakeExecutesPlugin;

public static class Log
{
    private static ILogger? _logger;

    public static void Initialize(ILogger logger) => _logger = logger;

    public static void Info(string msg, params object?[] args) =>
        _logger?.LogInformation($"[RetakeExecutes] |      {msg}", args);

    public static void Warn(string msg, params object?[] args) =>
        _logger?.LogWarning($"[RetakeExecutes] |      {msg}", args);

    public static void Error(string msg, params object?[] args) =>
        _logger?.LogError($"[RetakeExecutes] |      {msg}", args);
}
