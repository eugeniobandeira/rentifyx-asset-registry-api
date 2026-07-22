using FluentValidation;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.MessageResource;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Validator;

public sealed class SearchAssetsValidator : AbstractValidator<SearchAssetsRequest>
{
    public SearchAssetsValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(ValidationConstants.SearchRules.MinPageSize, ValidationConstants.SearchRules.MaxPageSize)
                .WithMessage(ValidationMessageResource.PAGE_SIZE_INVALID);

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessageResource.PRICE_INVALID)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
                .WithMessage(ValidationMessageResource.PRICE_INVALID)
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage(ValidationMessageResource.PRICE_RANGE_INVALID)
                .WithName(nameof(SearchAssetsRequest.MinPrice));
    }
}
