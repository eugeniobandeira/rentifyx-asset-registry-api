using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Validator;
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
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
