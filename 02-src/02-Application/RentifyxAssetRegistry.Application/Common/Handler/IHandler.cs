using ErrorOr;

namespace RentifyxAssetRegistry.Application.Common.Handler;

public interface IHandler<TRequest, TResponse>
{
    Task<ErrorOr<TResponse>> HandleAsync(TRequest request, CancellationToken ct = default);
}
