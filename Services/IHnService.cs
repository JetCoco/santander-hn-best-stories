using HnBestStories.Dtos;

namespace HnBestStories.Services;

public interface IHnService
{
    Task<IReadOnlyList<StoryDto>> GetBestStoriesAsync(int count, CancellationToken ct);
}
