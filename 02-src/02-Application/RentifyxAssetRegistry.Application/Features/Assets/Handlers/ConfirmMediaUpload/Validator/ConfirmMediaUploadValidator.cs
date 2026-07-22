using FluentValidation;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.MessageResource;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Validator;

public sealed class ConfirmMediaUploadValidator : AbstractValidator<ConfirmMediaUploadRequest>
{
    public ConfirmMediaUploadValidator()
    {
        RuleFor(x => x.AssetId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.ASSET_ID_REQUIRED);

        RuleFor(x => x.OwnerId)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.OWNER_ID_REQUIRED);

        RuleFor(x => x.S3Key)
            .NotEmpty()
                .WithMessage(ValidationMessageResource.S3_KEY_REQUIRED);

        RuleFor(x => x.MimeType)
            .Must(mimeType => !string.IsNullOrWhiteSpace(mimeType)
                && ValidationConstants.MediaRules.AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
                .WithMessage(ValidationMessageResource.MIME_TYPE_INVALID);

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
                .WithMessage(ValidationMessageResource.SIZE_BYTES_INVALID);
    }
}
