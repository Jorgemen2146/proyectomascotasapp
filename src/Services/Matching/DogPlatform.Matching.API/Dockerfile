FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Matching/DogPlatform.Matching.API/DogPlatform.Matching.API.csproj", "src/Services/Matching/DogPlatform.Matching.API/"]
COPY ["src/Services/Matching/DogPlatform.Matching.Application/DogPlatform.Matching.Application.csproj", "src/Services/Matching/DogPlatform.Matching.Application/"]
COPY ["src/Services/Matching/DogPlatform.Matching.Infrastructure/DogPlatform.Matching.Infrastructure.csproj", "src/Services/Matching/DogPlatform.Matching.Infrastructure/"]
COPY ["src/Services/Matching/DogPlatform.Matching.Domain/DogPlatform.Matching.Domain.csproj", "src/Services/Matching/DogPlatform.Matching.Domain/"]
COPY ["BuildingBlocks/DogPlatform.Authentication/DogPlatform.Authentication.csproj", "BuildingBlocks/DogPlatform.Authentication/"]
COPY ["BuildingBlocks/DogPlatform.Logging/DogPlatform.Logging.csproj", "BuildingBlocks/DogPlatform.Logging/"]
COPY ["BuildingBlocks/DogPlatform.Common/DogPlatform.Common.csproj", "BuildingBlocks/DogPlatform.Common/"]
COPY ["BuildingBlocks/DogPlatform.SharedKernel/DogPlatform.SharedKernel.csproj", "BuildingBlocks/DogPlatform.SharedKernel/"]
COPY ["BuildingBlocks/DogPlatform.Contracts/DogPlatform.Contracts.csproj", "BuildingBlocks/DogPlatform.Contracts/"]
COPY ["BuildingBlocks/DogPlatform.EventBus/DogPlatform.EventBus.csproj", "BuildingBlocks/DogPlatform.EventBus/"]
RUN dotnet restore "src/Services/Matching/DogPlatform.Matching.API/DogPlatform.Matching.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Matching/DogPlatform.Matching.API"
RUN dotnet build "DogPlatform.Matching.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DogPlatform.Matching.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogPlatform.Matching.API.dll"]
