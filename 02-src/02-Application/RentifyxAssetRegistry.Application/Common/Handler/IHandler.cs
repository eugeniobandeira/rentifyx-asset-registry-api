using ErrorOr;

namespace RentifyxAssetRegistry.Application.Common.Handler;

public interface IHandler<TRequest, TResponse>
{
    Task<ErrorOr<TResponse>> Handle(TRequest request, CancellationToken ct = default);
}
