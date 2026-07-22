using ErrorOr;

namespace rentifyx_asset_registry_api.Application.Common.Handler;

public interface IHandler<TRequest, TResponse>
{
    Task<ErrorOr<TResponse>> Handle(TRequest request, CancellationToken ct = default);
}
