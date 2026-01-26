using Microsoft.Extensions.Configuration;

namespace HrLite.Infrastructure.AI;

public class LlmRateLimiter
{
    private readonly int _rateLimitPerMinute;
    private readonly Queue<DateTime> _timestamps = new();
    private readonly object _lock = new();

    public LlmRateLimiter(IConfiguration configuration)
    {
        _rateLimitPerMinute = configuration.GetValue<int>("Ai:RateLimitPerMinute", 0);
    }

    public async Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (_rateLimitPerMinute <= 0)
        {
            return;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int delayMs;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                while (_timestamps.Count > 0 && (now - _timestamps.Peek()).TotalSeconds >= 60)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count < _rateLimitPerMinute)
                {
                    _timestamps.Enqueue(now);
                    return;
                }

                var oldest = _timestamps.Peek();
                delayMs = (int)Math.Ceiling(60000 - (now - oldest).TotalMilliseconds);
                if (delayMs < 0)
                {
                    delayMs = 0;
                }
            }

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }
}
