using backend.Hubs;
using backend.Models.Realtime;
using backend.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services;

public class MockDataService : BackgroundService
{
    private readonly IRealtimeCacheService _cache;
    private readonly ILogger<MockDataService> _logger;
    private readonly IHubContext<RealtimeHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEquipmentTagBindingService _tagBindingService;
    private readonly List<EquipmentRuntimeSource> _sources = new();

    public MockDataService(
        IRealtimeCacheService cache,
        ILogger<MockDataService> logger,
        IHubContext<RealtimeHub> hubContext,
        IServiceScopeFactory scopeFactory,
        IEquipmentTagBindingService tagBindingService)
    {
        _cache = cache;
        _logger = logger;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _tagBindingService = tagBindingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Mock realtime data service started.");

        await EnsureSourcesAsync(stoppingToken);
        var rng = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var source in _sources)
            {
                foreach (var snapshot in BuildSnapshots(source, rng))
                {
                    _cache.UpdateTag(snapshot);
                    await _hubContext.Clients.All.SendAsync("ReceiveTagValue", snapshot, stoppingToken);
                }
            }

            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Mock realtime data service stopped.");
    }

    private async Task EnsureSourcesAsync(CancellationToken cancellationToken)
    {
        if (_sources.Count > 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
        var records = await repository.GetEquipmentMonitoringRecordsAsync(null, cancellationToken);

        foreach (var record in records)
        {
            _sources.Add(new EquipmentRuntimeSource(
                record.EquipmentCode,
                record.TypeCode,
                _tagBindingService.Resolve(record.EquipmentCode, record.TypeCode)));
        }
    }

    private static IEnumerable<TagSnapshot> BuildSnapshots(EquipmentRuntimeSource source, Random rng)
    {
        var now = DateTime.Now;

        foreach (var snapshot in BuildMetricSnapshots(source, rng, now))
        {
            yield return snapshot;
        }

        if (!string.IsNullOrWhiteSpace(source.Binding.StatusTagName))
        {
            yield return new TagSnapshot
            {
                TagName = source.Binding.StatusTagName,
                Value = rng.NextDouble() > 0.15 ? 1 : 0,
                Unit = string.Empty,
                Timestamp = now,
                Quality = 192
            };
        }
    }

    private static IEnumerable<TagSnapshot> BuildMetricSnapshots(EquipmentRuntimeSource source, Random rng, DateTime now)
    {
        foreach (var metric in EnumerateMetricDefinitions(source.Binding, source.TypeCode, rng))
        {
            if (string.IsNullOrWhiteSpace(metric.TagName))
            {
                continue;
            }

            yield return new TagSnapshot
            {
                TagName = metric.TagName,
                Value = metric.Value,
                Unit = metric.Unit,
                Timestamp = now,
                Quality = 192
            };
        }
    }

    private static IEnumerable<(string? TagName, double Value, string Unit)> EnumerateMetricDefinitions(
        EquipmentMetricTagBinding binding,
        string typeCode,
        Random rng)
    {
        var flowValue = typeCode switch
        {
            "LP_PUMP" => Math.Round(900 + rng.NextDouble() * 450, 2),
            "HP_PUMP" => Math.Round(250 + rng.NextDouble() * 160, 2),
            "SW_PUMP_BIG" => Math.Round(3200 + rng.NextDouble() * 600, 2),
            "SW_PUMP_SMALL" => Math.Round(1200 + rng.NextDouble() * 350, 2),
            "ORV" => Math.Round(300 + rng.NextDouble() * 120, 2),
            "BOG_COMPRESSOR" => Math.Round(180 + rng.NextDouble() * 80, 2),
            _ => Math.Round(100 + rng.NextDouble() * 100, 2)
        };

        var pressureValue = typeCode switch
        {
            "LP_PUMP" => Math.Round(0.45 + rng.NextDouble() * 0.4, 3),
            "HP_PUMP" => Math.Round(7.5 + rng.NextDouble() * 2.0, 3),
            "BOG_COMPRESSOR" => Math.Round(0.2 + rng.NextDouble() * 0.6, 3),
            _ => Math.Round(0.3 + rng.NextDouble() * 0.8, 3)
        };

        var powerValue = typeCode switch
        {
            "LP_PUMP" => Math.Round(180 + rng.NextDouble() * 90, 2),
            "HP_PUMP" => Math.Round(1100 + rng.NextDouble() * 280, 2),
            "SW_PUMP_BIG" => Math.Round(520 + rng.NextDouble() * 140, 2),
            "SW_PUMP_SMALL" => Math.Round(240 + rng.NextDouble() * 80, 2),
            "BOG_COMPRESSOR" => Math.Round(680 + rng.NextDouble() * 140, 2),
            _ => Math.Round(80 + rng.NextDouble() * 120, 2)
        };

        var temperatureValue = typeCode switch
        {
            "ORV" => Math.Round(8 + rng.NextDouble() * 14, 2),
            "BOG_COMPRESSOR" => Math.Round(-135 + rng.NextDouble() * 18, 2),
            "RECONDENSER" => Math.Round(-155 + rng.NextDouble() * 15, 2),
            _ => Math.Round(-165 + rng.NextDouble() * 20, 2)
        };

        var levelValue = typeCode switch
        {
            "LP_PUMP" => Math.Round(55 + rng.NextDouble() * 22, 2),
            "RECONDENSER" => Math.Round(30 + rng.NextDouble() * 20, 2),
            _ => Math.Round(10 + rng.NextDouble() * 15, 2)
        };

        yield return (binding.FlowRateTagName, flowValue, "m3/h");
        yield return (binding.PressureTagName, pressureValue, "MPa");
        yield return (binding.CurrentPowerTagName, powerValue, "kW");
        yield return (binding.TemperatureTagName, temperatureValue, "degC");
        yield return (binding.LiquidLevelTagName, levelValue, "%");
    }

    private sealed record EquipmentRuntimeSource(
        string EquipmentCode,
        string TypeCode,
        EquipmentMetricTagBinding Binding);
}
