FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/ApiGateway/DogPlatform.ApiGateway/DogPlatform.ApiGateway.csproj", "src/ApiGateway/DogPlatform.ApiGateway/"]
RUN dotnet restore "src/ApiGateway/DogPlatform.ApiGateway/DogPlatform.ApiGateway.csproj"

COPY . .
WORKDIR "/src/src/ApiGateway/DogPlatform.ApiGateway"
RUN dotnet build "DogPlatform.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DogPlatform.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogPlatform.ApiGateway.dll"]
