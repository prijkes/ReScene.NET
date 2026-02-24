using ReScene.Core;
using ReScene.Core.IO;

namespace ReScene.NET.Services;

/// <summary>
/// Wraps ReScene.Core.Manager to provide brute-force RAR reconstruction as a service.
/// </summary>
public class BruteForceService : IBruteForceService
{
    public event EventHandler<BruteForceProgressEventArgs>? Progress;
    public event EventHandler<BruteForceStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<LogEventArgs>? LogMessage;

    private Manager? _manager;

    public async Task<bool> RunAsync(BruteForceOptions options, CancellationToken ct)
    {
        var logger = new ReSceneLogger();
        logger.Logged += (s, e) => LogMessage?.Invoke(s, e);

        _manager = new Manager(logger);
        _manager.BruteForceProgress += (s, e) => Progress?.Invoke(s, e);
        _manager.BruteForceStatusChanged += (s, e) => StatusChanged?.Invoke(s, e);

        return await _manager.BruteForceRARVersionAsync(options);
    }

    public void Stop()
    {
        _manager?.Stop();
    }
}
