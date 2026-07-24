using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Admin override for a <c>PendingModeration</c> asset: approve straight to <c>Active</c>, or
/// reject back to <c>Draft</c>, bypassing the automated moderation verdict from
/// <c>rentifyx-ai-services</c>.
/// </summary>
internal sealed class AdminReviewAsset : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/admin-review", HandleAsync)
           .WithName("AdminReviewAsset")
           .WithSummary("Admin approve/reject a pending asset")
           .WithDescription(
               "Admin override for a PendingModeration asset: approve or reject. The asset must " +
               "currently be in PendingModeration status - calling this on a Draft, Active, " +
               "Suspended, or Archived asset fails. isAdmin must be true or the request is " +
               "rejected as forbidden. approve=true transitions the asset to Active; " +
               "approve=false transitions it back to Draft and reason should explain why.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["approve"] = true,
                       ["isAdmin"] = true,
                       ["reason"] = "Meets listing guidelines; photos are clear and description accurate."
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the admin-review request and maps the result to 200 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        Guid id,
        AdminReviewAssetBody body,
        IHandler<AdminReviewAssetRequest, AssetModerationResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        AdminReviewAssetRequest request = new(id, body.Approve, body.IsAdmin, body.Reason);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    /// <summary>
    /// Request body for <see cref="AdminReviewAsset"/>: the approve/reject verdict, an
    /// admin-privilege assertion, and an optional human-readable reason.
    /// </summary>
    private sealed record AdminReviewAssetBody(
        bool Approve,
        bool IsAdmin,
        string? Reason = null
    );
}
