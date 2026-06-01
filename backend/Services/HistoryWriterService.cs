using backend.Entities;
using backend.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services;

public class HistoryWriterService : BackgroundService
{
    private readonly IRealtimeCacheService _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HistoryWriterService> _logger;

    public HistoryWriterService(
        IRealtimeCacheService cache,
        IServiceScopeFactory scopeFactory,
        ILogger<HistoryWriterService> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("历史数据写入服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var allTags = _cache.GetAllTags();
                if (allTags.Count > 0)
                {
                    var records = allTags.Select(tag => new TagTrendRecord
                    {
                        TagName = tag.TagName,
                        Value = tag.Value,
                        Quality = tag.Quality,
                        Unit = tag.Unit,
                        Timestamp = tag.Timestamp
                    }).ToList();

                    // 手动创建 scope，获取 Scoped 的 ITagTrendRepository
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var trendRepo = scope.ServiceProvider.GetRequiredService<ITagTrendRepository>();
                        await trendRepo.InsertBatchAsync(records, stoppingToken);
                    }

                    _logger.LogDebug($"写入 {records.Count} 条历史记录");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "历史数据写入失败");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}