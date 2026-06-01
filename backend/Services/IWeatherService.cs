using backend.Models;

namespace backend.Services;

public interface IWeatherService
{
    Task<WeatherData> GetWeatherSeriesAsync(string? scheduleDate, int horizon, CancellationToken cancellationToken);
}
