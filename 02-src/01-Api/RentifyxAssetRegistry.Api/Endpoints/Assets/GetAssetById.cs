using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>Returns a single asset by its id, regardless of moderation status.</summary>
internal sealed class GetAssetById : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/assets/{id:guid}", HandleAsync)
           .WithName("GetAssetById")
           .WithSummary("Get an asset by id")
           .WithDescription("Returns an asset by id. Responds 404 if no asset exists with the given id.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .Produces<GetAssetByIdResponse>(StatusCodes.Status200OK)
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.Responses is not null &&
                   operation.Responses.TryGetValue("200", out var response) &&
                   response is OpenApiResponse { Content: not null } concreteResponse &&
                   concreteResponse.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["id"] = "6f1a2b3c-4d5e-4f60-8a1b-2c3d4e5f6071",
                       ["title"] = "Compact Excavator CAT 305E2",
                       ["description"] = "2022 CAT 305E2 mini excavator, 5.5t, low hours, well " +
                           "maintained, available for daily or weekly rental.",
                       ["price"] = 285.00m,
                       ["categoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                       ["ownerId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                       ["status"] = 2,
                       ["createdAt"] = "2026-07-20T14:32:00Z"
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the get-asset-by-id request and maps the result to 200 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        Guid id,
        IHandler<GetAssetByIdRequest, GetAssetByIdResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new GetAssetByIdRequest(id), cancellationToken);

        return result.ToResult(httpContext);
    }
}
