using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealtimeController : ControllerBase
{
    private readonly IRealtimeCacheService _cache;

    public RealtimeController(IRealtimeCacheService cache)
    {
        _cache = cache;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var allTags = _cache.GetAllTags();
        return Ok(allTags);
    }
}