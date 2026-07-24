using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Moves a <c>Draft</c> asset into <c>PendingModeration</c>, queuing it for review by
/// <c>rentifyx-ai-services</c> (automated verdict) or an admin (<see cref="AdminReviewAsset"/>).
/// </summary>
internal sealed class SubmitForModeration : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/submit-for-moderation", HandleAsync)
           .WithName("SubmitForModeration")
           .WithSummary("Submit a Draft asset for moderation")
           .WithDescription(
               "Submits a Draft asset for moderation review. Only the asset's owner may submit " +
               "it, and it must currently be in Draft status; the asset transitions to " +
               "PendingModeration on success.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["ownerId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the submit-for-moderation request and maps the result to 200 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        Guid id,
        SubmitForModerationBody body,
        IHandler<SubmitForModerationRequest, AssetModerationResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        SubmitForModerationRequest request = new(id, body.OwnerId);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    /// <summary>Request body for <see cref="SubmitForModeration"/>: the caller's asserted owner id.</summary>
    private sealed record SubmitForModerationBody(Guid OwnerId);
}
