using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using RentifyxAssetRegistry.Domain.MessageResource;
using FluentValidation;

namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Validator;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.CATEGORY_NAME_REQUIRED);
    }
}
