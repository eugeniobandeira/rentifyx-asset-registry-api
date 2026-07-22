using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Validator;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Validator;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Validator;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Validator;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Create.Validator;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Delete;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll.Validator;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetById;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update.Validator;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.IoC;

internal static class ApplicationDependencyInjection
{
    internal static IServiceCollection Register(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateExampleRequest>, CreateExampleValidator>();
        services.AddScoped<IValidator<UpdateExampleRequest>, UpdateExampleValidator>();
        services.AddScoped<IValidator<GetAllExampleRequest>, GetAllExampleValidator>();

        services.AddScoped<IHandler<CreateExampleRequest, ExampleEntity>, CreateExampleHandler>();
        services.AddScoped<IHandler<Guid, Deleted>, DeleteExampleHandler>();
        services.AddScoped<IHandler<GetAllExampleRequest, PagedResult<ExampleEntity>>, GetAllExampleHandler>();
        services.AddScoped<IHandler<Guid, ExampleEntity>, GetByIdExampleHandler>();
        services.AddScoped<IHandler<UpdateExampleRequest, ExampleEntity>, UpdateExampleHandler>();

        services.AddScoped<IValidator<CreateAssetRequest>, CreateAssetValidator>();
        services.AddScoped<IHandler<CreateAssetRequest, CreateAssetResponse>, CreateAssetHandler>();

        services.AddScoped<IValidator<RequestMediaUploadRequest>, RequestMediaUploadValidator>();
        services.AddScoped<IHandler<RequestMediaUploadRequest, RequestMediaUploadResponse>, RequestMediaUploadHandler>();

        services.AddScoped<IValidator<ConfirmMediaUploadRequest>, ConfirmMediaUploadValidator>();
        services.AddScoped<IHandler<ConfirmMediaUploadRequest, ConfirmMediaUploadResponse>, ConfirmMediaUploadHandler>();

        services.AddScoped<IValidator<SearchAssetsRequest>, SearchAssetsValidator>();
        services.AddScoped<IHandler<SearchAssetsRequest, SearchAssetsResponse>, SearchAssetsHandler>();

        services.AddScoped<IValidator<SubmitForModerationRequest>, SubmitForModerationValidator>();
        services.AddScoped<IHandler<SubmitForModerationRequest, AssetModerationResponse>, SubmitForModerationHandler>();

        services.AddScoped<IValidator<ApplyModerationVerdictRequest>, ApplyModerationVerdictValidator>();
        services.AddScoped<IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>, ApplyModerationVerdictHandler>();

        services.AddScoped<IValidator<AdminReviewAssetRequest>, AdminReviewAssetValidator>();
        services.AddScoped<IHandler<AdminReviewAssetRequest, AssetModerationResponse>, AdminReviewAssetHandler>();

        services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryValidator>();
        services.AddScoped<IHandler<CreateCategoryRequest, CategoryResponse>, CreateCategoryHandler>();

        services.AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryValidator>();
        services.AddScoped<IHandler<UpdateCategoryRequest, CategoryResponse>, UpdateCategoryHandler>();

        services.AddScoped<IHandler<ListCategoriesRequest, IReadOnlyList<CategoryResponse>>, ListCategoriesHandler>();

        return services;
    }
}
