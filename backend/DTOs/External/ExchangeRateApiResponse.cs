
using System.Text.Json.Serialization;

namespace backend.DTOs.External;

public sealed class ExchangeRateApiResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("base_code")]
    public string BaseCode { get; set; } = string.Empty;

    [JsonPropertyName("time_last_update_utc")]
    public string TimeLastUpdateUtc { get; set; } = string.Empty;

    [JsonPropertyName("time_next_update_utc")]
    public string TimeNextUpdateUtc { get; set; } = string.Empty;

    [JsonPropertyName("conversion_rates")]
    public Dictionary<string, decimal> ConversionRates { get; set; } = new();
}
