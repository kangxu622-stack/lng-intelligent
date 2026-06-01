using backend.Models.Realtime;

namespace backend.Services;

public interface IRealtimeCacheService
{
    void UpdateTag(TagSnapshot snapshot);
    TagSnapshot? GetTag(string tagName);
    List<TagSnapshot> GetAllTags();
}