using System.Collections.Concurrent;
using System.Diagnostics;

namespace SaaS.Api.Infrastructure.Monitoring;

public sealed class AppMetrics
{
    private long _totalRequests;
    private long _failedRequests;
    private readonly ConcurrentDictionary<string, long> _requestsByPath = new();
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public void TrackRequest(string path, int statusCode)
    {
        Interlocked.Increment(ref _totalRequests);
        if (statusCode >= 500) Interlocked.Increment(ref _failedRequests);
        _requestsByPath.AddOrUpdate(path, 1, (_, count) => count + 1);
    }

    public object Snapshot() => new
    {
        uptimeSeconds = (long)_uptime.Elapsed.TotalSeconds,
        totalRequests = Interlocked.Read(ref _totalRequests),
        failedRequests = Interlocked.Read(ref _failedRequests),
        requestsByPath = _requestsByPath.OrderByDescending(pair => pair.Value).ToDictionary()
    };
}
