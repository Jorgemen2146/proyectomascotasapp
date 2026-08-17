FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Walks/DogPlatform.Walks.API/DogPlatform.Walks.API.csproj", "src/Services/Walks/DogPlatform.Walks.API/"]
COPY ["src/Services/Walks/DogPlatform.Walks.Application/DogPlatform.Walks.Application.csproj", "src/Services/Walks/DogPlatform.Walks.Application/"]
COPY ["src/Services/Walks/DogPlatform.Walks.Infrastructure/DogPlatform.Walks.Infrastructure.csproj", "src/Services/Walks/DogPlatform.Walks.Infrastructure/"]
COPY ["src/Services/Walks/DogPlatform.Walks.Domain/DogPlatform.Walks.Domain.csproj", "src/Services/Walks/DogPlatform.Walks.Domain/"]
COPY ["BuildingBlocks/DogPlatform.Authentication/DogPlatform.Authentication.csproj", "BuildingBlocks/DogPlatform.Authentication/"]
COPY ["BuildingBlocks/DogPlatform.Logging/DogPlatform.Logging.csproj", "BuildingBlocks/DogPlatform.Logging/"]
COPY ["BuildingBlocks/DogPlatform.Common/DogPlatform.Common.csproj", "BuildingBlocks/DogPlatform.Common/"]
COPY ["BuildingBlocks/DogPlatform.SharedKernel/DogPlatform.SharedKernel.csproj", "BuildingBlocks/DogPlatform.SharedKernel/"]
COPY ["BuildingBlocks/DogPlatform.Contracts/DogPlatform.Contracts.csproj", "BuildingBlocks/DogPlatform.Contracts/"]
COPY ["BuildingBlocks/DogPlatform.EventBus/DogPlatform.EventBus.csproj", "BuildingBlocks/DogPlatform.EventBus/"]
RUN dotnet restore "src/Services/Walks/DogPlatform.Walks.API/DogPlatform.Walks.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Walks/DogPlatform.Walks.API"
RUN dotnet build "DogPlatform.Walks.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DogPlatform.Walks.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogPlatform.Walks.API.dll"]
