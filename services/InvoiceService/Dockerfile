# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/Shared/Shared.csproj", "shared/Shared/"]

# Copy InvoiceService project
COPY ["services/InvoiceService/InvoiceService.csproj", "services/InvoiceService/"]

# Restore dependencies
RUN dotnet restore "services/InvoiceService/InvoiceService.csproj"

# Copy all source files
COPY ["shared/Shared/", "shared/Shared/"]
COPY ["services/InvoiceService/", "services/InvoiceService/"]

# Build
WORKDIR "/src/services/InvoiceService"
RUN dotnet build "InvoiceService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "InvoiceService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InvoiceService.dll"]
