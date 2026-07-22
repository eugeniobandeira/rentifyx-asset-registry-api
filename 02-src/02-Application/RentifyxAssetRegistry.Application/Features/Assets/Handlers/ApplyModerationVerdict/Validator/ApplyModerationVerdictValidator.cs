using FluentValidation;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Domain.MessageResource;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Validator;

public sealed class ApplyModerationVerdictValidator : AbstractValidator<ApplyModerationVerdictRequest>
{
    public ApplyModerationVerdictValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.ASSET_ID_REQUIRED);
    }
}
