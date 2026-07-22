using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.MessageResource;
using FluentValidation;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Validator;

public sealed class CreateAssetValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.OWNER_ID_REQUIRED);

        RuleFor(x => x.CategoryId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.CATEGORY_ID_REQUIRED);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.IDEMPOTENCY_KEY_REQUIRED);

        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.TITLE_REQUIRED)
            .MinimumLength(ValidationConstants.AssetRules.TitleMinLength)
                .WithMessage(ValidationMessageResource.TITLE_MIN_LENGTH)
            .MaximumLength(ValidationConstants.AssetRules.TitleMaxLength)
                .WithMessage(ValidationMessageResource.TITLE_MAX_LENGTH);

        RuleFor(x => x.Description)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.DESCRIPTION_REQUIRED)
            .MinimumLength(ValidationConstants.AssetRules.DescriptionMinLength)
                .WithMessage(ValidationMessageResource.DESCRIPTION_MIN_LENGTH)
            .MaximumLength(ValidationConstants.AssetRules.DescriptionMaxLength)
                .WithMessage(ValidationMessageResource.DESCRIPTION_MAX_LENGTH);
    }
}
