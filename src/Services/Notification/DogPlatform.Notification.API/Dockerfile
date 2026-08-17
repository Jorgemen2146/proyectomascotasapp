FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Services/Notification/DogPlatform.Notification.API/DogPlatform.Notification.API.csproj", "src/Services/Notification/DogPlatform.Notification.API/"]
COPY ["src/Services/Notification/DogPlatform.Notification.Application/DogPlatform.Notification.Application.csproj", "src/Services/Notification/DogPlatform.Notification.Application/"]
COPY ["src/Services/Notification/DogPlatform.Notification.Infrastructure/DogPlatform.Notification.Infrastructure.csproj", "src/Services/Notification/DogPlatform.Notification.Infrastructure/"]
COPY ["src/Services/Notification/DogPlatform.Notification.Domain/DogPlatform.Notification.Domain.csproj", "src/Services/Notification/DogPlatform.Notification.Domain/"]
COPY ["BuildingBlocks/DogPlatform.Authentication/DogPlatform.Authentication.csproj", "BuildingBlocks/DogPlatform.Authentication/"]
COPY ["BuildingBlocks/DogPlatform.Logging/DogPlatform.Logging.csproj", "BuildingBlocks/DogPlatform.Logging/"]
COPY ["BuildingBlocks/DogPlatform.Common/DogPlatform.Common.csproj", "BuildingBlocks/DogPlatform.Common/"]
COPY ["BuildingBlocks/DogPlatform.SharedKernel/DogPlatform.SharedKernel.csproj", "BuildingBlocks/DogPlatform.SharedKernel/"]
COPY ["BuildingBlocks/DogPlatform.Contracts/DogPlatform.Contracts.csproj", "BuildingBlocks/DogPlatform.Contracts/"]
COPY ["BuildingBlocks/DogPlatform.EventBus/DogPlatform.EventBus.csproj", "BuildingBlocks/DogPlatform.EventBus/"]
RUN dotnet restore "src/Services/Notification/DogPlatform.Notification.API/DogPlatform.Notification.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Notification/DogPlatform.Notification.API"
RUN dotnet build "DogPlatform.Notification.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DogPlatform.Notification.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogPlatform.Notification.API.dll"]
