
using System.Net.Http.Json;
using backend.DTOs.External;

namespace backend.ExternalServices;

public sealed class ExchangeRateService(
    HttpClient httpClient,
    IConfiguration configuration) : IExchangeRateService
{
    public async Task<ExchangeRateApiResponse> GetLatestRatesAsync(
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["ExchangeRateApi:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "ExchangeRate-API key is not configured.");
        }

        using var response = await httpClient.GetAsync(
            $"{Uri.EscapeDataString(apiKey)}/latest/USD",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var rates = await response.Content
            .ReadFromJsonAsync<ExchangeRateApiResponse>(
                cancellationToken: cancellationToken);

        if (rates is null ||
            rates.Result != "success" ||
            rates.BaseCode != "USD" ||
            rates.ConversionRates.Count == 0)
        {
            throw new HttpRequestException(
                "ExchangeRate-API returned an invalid exchange-rate response.");
        }

        return rates;
    }
}
