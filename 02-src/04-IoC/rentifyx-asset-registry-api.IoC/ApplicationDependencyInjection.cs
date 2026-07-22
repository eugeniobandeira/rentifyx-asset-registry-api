using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Validator;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Delete;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Validator;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetById;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Validator;
using rentifyx_asset_registry_api.Domain.Common;
using rentifyx_asset_registry_api.Domain.Entities;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace rentifyx_asset_registry_api.IoC;

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

        return services;
    }
}
