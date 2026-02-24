using ReScene.Core;
using ReScene.Core.IO;

namespace ReScene.NET.Services;

public interface IBruteForceService
{
    Task<bool> RunAsync(BruteForceOptions options, CancellationToken ct);
    void Stop();
    event EventHandler<BruteForceProgressEventArgs>? Progress;
    event EventHandler<BruteForceStatusChangedEventArgs>? StatusChanged;
    event EventHandler<LogEventArgs>? LogMessage;
}
