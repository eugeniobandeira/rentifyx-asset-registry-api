using FluentValidation;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;
using RentifyxAssetRegistry.Domain.MessageResource;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Validator;

public sealed class SubmitForModerationValidator : AbstractValidator<SubmitForModerationRequest>
{
    public SubmitForModerationValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.ASSET_ID_REQUIRED);

        RuleFor(x => x.OwnerId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.OWNER_ID_REQUIRED);
    }
}
