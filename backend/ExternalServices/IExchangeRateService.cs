
using backend.DTOs.External;

namespace backend.ExternalServices;

public interface IExchangeRateService
{
    Task<ExchangeRateApiResponse> GetLatestRatesAsync(
        CancellationToken cancellationToken = default);
}
