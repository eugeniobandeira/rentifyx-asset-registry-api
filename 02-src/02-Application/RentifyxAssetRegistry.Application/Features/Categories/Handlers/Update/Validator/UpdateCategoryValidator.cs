using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;
using RentifyxAssetRegistry.Domain.MessageResource;
using FluentValidation;

namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Validator;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.CATEGORY_ID_REQUIRED);

        RuleFor(x => x.NewName)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.CATEGORY_NAME_REQUIRED)
            .When(x => x.NewName is not null);
    }
}
