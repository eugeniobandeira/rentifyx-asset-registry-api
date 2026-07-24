using System.Text.Json.Nodes;
using ErrorOr;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Creates a new asset owned by the caller. The asset starts in <c>Draft</c> status: it is not
/// searchable or publicly visible until it is submitted for moderation
/// (<see cref="SubmitForModeration"/>) and approved.
/// </summary>
internal sealed class CreateAsset : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets", HandleAsync)
           .WithName("CreateAsset")
           .WithSummary("Create a new asset")
           .WithDescription(
               "Creates a new asset in Draft status. The caller becomes the owner. Title must be " +
               "3-100 characters and description 10-2000 characters. The asset is not visible in " +
               "search results until it is submitted for moderation and approved.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["ownerId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                       ["title"] = "Compact Excavator CAT 305E2",
                       ["description"] = "2022 CAT 305E2 mini excavator, 5.5t, low hours, well " +
                           "maintained, available for daily or weekly rental.",
                       ["price"] = 285.00m,
                       ["categoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                       ["idempotencyKey"] = "b3c1a2d4-5e6f-4a7b-8c9d-0e1f2a3b4c5d"
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the create-asset request and maps the result to a 201 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        CreateAssetRequest request,
        IHandler<CreateAssetRequest, CreateAssetResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(request, cancellationToken);

        return result.Match(
            r => Results.Created($"/api/v1/assets/{r.AssetId}", r),
            errors => errors.ToProblem(httpContext));
    }
}
