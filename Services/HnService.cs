using System.Net.Http.Json;
using HnBestStories.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace HnBestStories.Services
{
    public sealed class HnService : IHnService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HnService> _logger;

        private static readonly string BestIdsKey = "hn:best:ids";
        private static readonly TimeSpan BestIdsTtl = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(5);
        private static readonly SemaphoreSlim Gate = new(initialCount: 12);

        private readonly AsyncRetryPolicy _retry;

        public HnService(HttpClient http, IMemoryCache cache, ILogger<HnService> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;

            _retry = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                    onRetryAsync: (ex, delay, attempt, ctx) =>
                    {
                        _logger.LogWarning(ex, "Retry {Attempt} after {Delay}", attempt, delay);
                        return Task.CompletedTask;
                    });
        }

        public async Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(int count, CancellationToken ct)
        {
            // Cache IDs de beststories (sin nulls)
            var ids = await _cache.GetOrCreateAsync(BestIdsKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = BestIdsTtl;

                const string url = "https://hacker-news.firebaseio.com/v0/beststories.json";
                _logger.LogDebug("Fetching beststories IDs");

                var result = await _retry.ExecuteAsync(token =>
                    _http.GetFromJsonAsync<List<long>>(url, token), ct);

                return result ?? new List<long>();
            }) ?? new List<long>();

            var tasks = ids.Take(count).Select(id => GetStoryCachedAsync(id, ct));
            var stories = await Task.WhenAll(tasks);

            return stories
                .Where(s => s is not null)
                .OrderByDescending(s => s!.Score)
                .Take(count)
                .ToList()!;
        }

        private async Task<StoryDto?> GetStoryCachedAsync(long id, CancellationToken ct)
        {
            var key = $"hn:item:{id}";

            // 🔧 Fix nulabilidad: out StoryDto? y chequeo de null
            if (_cache.TryGetValue(key, out StoryDto? cached) && cached is not null)
                return cached;

            await Gate.WaitAsync(ct);
            try
            {
                if (_cache.TryGetValue(key, out cached) && cached is not null)
                    return cached;

                var url = $"https://hacker-news.firebaseio.com/v0/item/{id}.json";

                HnItem? item = await _retry.ExecuteAsync(token =>
                    _http.GetFromJsonAsync<HnItem>(url, token), ct);

                if (item is null || !string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase))
                    return null;

                var dto = new StoryDto
                {
                    Title = item.Title ?? string.Empty,
                    Uri = item.Url ?? string.Empty,
                    PostedBy = item.By ?? string.Empty,
                    Time = DateTimeOffset.FromUnixTimeSeconds(item.Time),
                    Score = item.Score,
                    CommentCount = item.Descendants
                };

                _cache.Set(key, dto, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ItemTtl,
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

                return dto;
            }
            finally
            {
                Gate.Release();
            }
        }

        private sealed class HnItem
        {
            public string? By { get; set; }
            public int Descendants { get; set; }
            public long Id { get; set; }
            public int Score { get; set; }
            public long Time { get; set; }
            public string? Title { get; set; }
            public string? Type { get; set; }
            public string? Url { get; set; }
        }
    }
}
