using System.Collections.Concurrent;

namespace ZEGU.WebApp.Services
{
    public class RateLimitService
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _attempts = new();

        public bool IsAllowed(string key, int maxAttempts, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var attempts = _attempts.GetOrAdd(key, _ => new List<DateTime>());

            lock (attempts)
            {
                attempts.RemoveAll(t => now - t > window);

                if (attempts.Count >= maxAttempts)
                    return false;

                attempts.Add(now);
                return true;
            }
        }

        public void Reset(string key)
        {
            _attempts.TryRemove(key, out _);
        }
    }
}
