FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["01-aspire/02-ServiceDefaults/RentifyxAssetRegistry.ServiceDefaults/RentifyxAssetRegistry.ServiceDefaults.csproj", "01-aspire/02-ServiceDefaults/RentifyxAssetRegistry.ServiceDefaults/"]
COPY ["02-src/03-Domain/RentifyxAssetRegistry.Domain/RentifyxAssetRegistry.Domain.csproj", "02-src/03-Domain/RentifyxAssetRegistry.Domain/"]
COPY ["02-src/02-Application/RentifyxAssetRegistry.Application/RentifyxAssetRegistry.Application.csproj", "02-src/02-Application/RentifyxAssetRegistry.Application/"]
COPY ["02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/RentifyxAssetRegistry.Infrastructure.csproj", "02-src/05-Infrastructure/RentifyxAssetRegistry.Infrastructure/"]
COPY ["02-src/04-IoC/RentifyxAssetRegistry.IoC/RentifyxAssetRegistry.IoC.csproj", "02-src/04-IoC/RentifyxAssetRegistry.IoC/"]
COPY ["02-src/01-Api/RentifyxAssetRegistry.Api/RentifyxAssetRegistry.Api.csproj", "02-src/01-Api/RentifyxAssetRegistry.Api/"]

RUN dotnet restore "02-src/01-Api/RentifyxAssetRegistry.Api/RentifyxAssetRegistry.Api.csproj"

COPY . .

RUN dotnet publish "02-src/01-Api/RentifyxAssetRegistry.Api/RentifyxAssetRegistry.Api.csproj" \
    --no-restore \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RentifyxAssetRegistry.Api.dll"]
