FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Health/DogPlatform.Health.API/DogPlatform.Health.API.csproj", "src/Services/Health/DogPlatform.Health.API/"]
COPY ["src/Services/Health/DogPlatform.Health.Application/DogPlatform.Health.Application.csproj", "src/Services/Health/DogPlatform.Health.Application/"]
COPY ["src/Services/Health/DogPlatform.Health.Infrastructure/DogPlatform.Health.Infrastructure.csproj", "src/Services/Health/DogPlatform.Health.Infrastructure/"]
COPY ["src/Services/Health/DogPlatform.Health.Domain/DogPlatform.Health.Domain.csproj", "src/Services/Health/DogPlatform.Health.Domain/"]
COPY ["BuildingBlocks/DogPlatform.Authentication/DogPlatform.Authentication.csproj", "BuildingBlocks/DogPlatform.Authentication/"]
COPY ["BuildingBlocks/DogPlatform.Logging/DogPlatform.Logging.csproj", "BuildingBlocks/DogPlatform.Logging/"]
COPY ["BuildingBlocks/DogPlatform.Common/DogPlatform.Common.csproj", "BuildingBlocks/DogPlatform.Common/"]
COPY ["BuildingBlocks/DogPlatform.SharedKernel/DogPlatform.SharedKernel.csproj", "BuildingBlocks/DogPlatform.SharedKernel/"]
COPY ["BuildingBlocks/DogPlatform.Contracts/DogPlatform.Contracts.csproj", "BuildingBlocks/DogPlatform.Contracts/"]
COPY ["BuildingBlocks/DogPlatform.EventBus/DogPlatform.EventBus.csproj", "BuildingBlocks/DogPlatform.EventBus/"]
RUN dotnet restore "src/Services/Health/DogPlatform.Health.API/DogPlatform.Health.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Health/DogPlatform.Health.API"
RUN dotnet build "DogPlatform.Health.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DogPlatform.Health.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogPlatform.Health.API.dll"]
