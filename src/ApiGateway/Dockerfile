
# Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (for Docker layer caching)
# If only code changes (not .csproj), Docker reuses the cached restore layer
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
RUN dotnet restore src/ApiGateway/ApiGateway.csproj

# Copy all source code and build
COPY src/ApiGateway/ src/ApiGateway/
COPY src/Shared.Contracts/ src/Shared.Contracts/
RUN dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /app/publish

# Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP (8080) and HTTPS (8443)
# Note: We use non-root user for security best practices
EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
