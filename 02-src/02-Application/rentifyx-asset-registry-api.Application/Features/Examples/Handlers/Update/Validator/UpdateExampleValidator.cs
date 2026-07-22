using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Request;
using rentifyx_asset_registry_api.Domain.Constants;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Interfaces.Common;
using rentifyx_asset_registry_api.Domain.MessageResource;
using FluentValidation;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Validator;

public sealed class UpdateExampleValidator : AbstractValidator<UpdateExampleRequest>
{
    public UpdateExampleValidator(IGetByIdRepository<ExampleEntity> repository)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await repository.GetByIdAsync(id, ct) is not null)
            .WithErrorCode(ExampleErrorCodes.NotFound)
            .WithMessage(ValidationMessageResource.EXAMPLE_NOT_FOUND);

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
