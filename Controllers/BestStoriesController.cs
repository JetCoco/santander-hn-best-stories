using HnBestStories.Dtos;
using HnBestStories.Services;
using Microsoft.AspNetCore.Mvc;

namespace HnBestStories.Controllers;

[ApiController]
[Route("api/stories")]
public sealed class BestStoriesController(IHnService service) : ControllerBase
{
    [HttpGet("best")]
    [ProducesResponseType(typeof(IEnumerable<StoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBest([FromQuery] int count = 10, CancellationToken ct = default)
    {
        if (count is < 1 or > 100) return BadRequest("count must be between 1 and 100.");
        var stories = await service.GetBestStoriesAsync(count, ct);
        return Ok(stories);
    }
}