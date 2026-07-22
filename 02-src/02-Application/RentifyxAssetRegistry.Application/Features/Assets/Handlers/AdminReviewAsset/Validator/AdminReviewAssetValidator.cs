using FluentValidation;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;
using RentifyxAssetRegistry.Domain.MessageResource;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Validator;

public sealed class AdminReviewAssetValidator : AbstractValidator<AdminReviewAssetRequest>
{
    public AdminReviewAssetValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.ASSET_ID_REQUIRED);
    }
}
