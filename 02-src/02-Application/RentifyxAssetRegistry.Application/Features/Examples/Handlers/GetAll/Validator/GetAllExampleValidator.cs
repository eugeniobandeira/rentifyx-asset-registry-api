using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.MessageResource;
using FluentValidation;

namespace RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll.Validator;

public sealed class GetAllExampleValidator : AbstractValidator<GetAllExampleRequest>
{
    public GetAllExampleValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(ValidationConstants.Pagination.MinPage)
                .WithMessage(ValidationMessageResource.PAGE_INVALID);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(ValidationConstants.Pagination.MinPageSize, ValidationConstants.Pagination.MaxPageSize)
                .WithMessage(ValidationMessageResource.PAGE_SIZE_INVALID);
    }
}
