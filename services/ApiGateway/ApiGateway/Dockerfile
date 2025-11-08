# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/Shared/Shared.csproj", "shared/Shared/"]

# Copy ApiGateway project
COPY ["services/ApiGateway/ApiGateway/ApiGateway.csproj", "services/ApiGateway/ApiGateway/"]

# Restore dependencies
RUN dotnet restore "services/ApiGateway/ApiGateway/ApiGateway.csproj"

# Copy all source files
COPY ["shared/Shared/", "shared/Shared/"]
COPY ["services/ApiGateway/ApiGateway/", "services/ApiGateway/ApiGateway/"]

# Build
WORKDIR "/src/services/ApiGateway/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
