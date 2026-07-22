FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["01-aspire/02-ServiceDefaults/rentifyx_asset_registry_api.ServiceDefaults/rentifyx_asset_registry_api.ServiceDefaults.csproj", "01-aspire/02-ServiceDefaults/rentifyx_asset_registry_api.ServiceDefaults/"]
COPY ["02-src/03-Domain/rentifyx_asset_registry_api.Domain/rentifyx_asset_registry_api.Domain.csproj", "02-src/03-Domain/rentifyx_asset_registry_api.Domain/"]
COPY ["02-src/02-Application/rentifyx_asset_registry_api.Application/rentifyx_asset_registry_api.Application.csproj", "02-src/02-Application/rentifyx_asset_registry_api.Application/"]
COPY ["02-src/05-Infrastructure/rentifyx_asset_registry_api.Infrastructure/rentifyx_asset_registry_api.Infrastructure.csproj", "02-src/05-Infrastructure/rentifyx_asset_registry_api.Infrastructure/"]
COPY ["02-src/04-IoC/rentifyx_asset_registry_api.IoC/rentifyx_asset_registry_api.IoC.csproj", "02-src/04-IoC/rentifyx_asset_registry_api.IoC/"]
COPY ["02-src/01-Api/rentifyx_asset_registry_api.Api/rentifyx_asset_registry_api.Api.csproj", "02-src/01-Api/rentifyx_asset_registry_api.Api/"]

RUN dotnet restore "02-src/01-Api/rentifyx_asset_registry_api.Api/rentifyx_asset_registry_api.Api.csproj"

COPY . .

RUN dotnet publish "02-src/01-Api/rentifyx_asset_registry_api.Api/rentifyx_asset_registry_api.Api.csproj" \
    --no-restore \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "rentifyx_asset_registry_api.Api.dll"]
