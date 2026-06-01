using System.Collections.Concurrent;
using System.Linq;
using backend.Models.Realtime;
namespace backend.Services;

public class RealtimeCacheService : IRealtimeCacheService
{
    private readonly ConcurrentDictionary<string, TagSnapshot> _cache = new();

    public void UpdateTag(TagSnapshot snapshot) => _cache[snapshot.TagName] = snapshot;

    public TagSnapshot? GetTag(string tagName)
    {
        _cache.TryGetValue(tagName, out var snap);
        return snap;
    }

    public List<TagSnapshot> GetAllTags() => _cache.Values.OrderBy(t => t.TagName).ToList();
}