using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Request;
using rentifyx_asset_registry_api.Domain.Constants;
using rentifyx_asset_registry_api.Domain.MessageResource;
using FluentValidation;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Validator;

public sealed class CreateExampleValidator : AbstractValidator<CreateExampleRequest>
{
    public CreateExampleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.NAME_REQUIRED)
            .MaximumLength(ValidationConstants.ExampleRules.NameMaxLength)
                .WithMessage(ValidationMessageResource.NAME_MAX_LENGTH);

        RuleFor(x => x.Description)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.DESCRIPTION_REQUIRED)
            .MaximumLength(ValidationConstants.ExampleRules.DescriptionMaxLength)
                .WithMessage(ValidationMessageResource.DESCRIPTION_MAX_LENGTH);
    }
}
