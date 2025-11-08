# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/Shared/Shared.csproj", "shared/Shared/"]

# Copy InventoryService project
COPY ["services/InventoryService/InventoryService.csproj", "services/InventoryService/"]

# Restore dependencies
RUN dotnet restore "services/InventoryService/InventoryService.csproj"

# Copy all source files
COPY ["shared/Shared/", "shared/Shared/"]
COPY ["services/InventoryService/", "services/InventoryService/"]

# Build
WORKDIR "/src/services/InventoryService"
RUN dotnet build "InventoryService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.dll"]
