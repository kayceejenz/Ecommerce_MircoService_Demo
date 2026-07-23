
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/CatalogService/CatalogService.csproj src/CatalogService/
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
RUN dotnet restore src/CatalogService/CatalogService.csproj

COPY src/CatalogService/ src/CatalogService/
COPY src/Shared.Contracts/ src/Shared.Contracts/
RUN dotnet publish src/CatalogService/CatalogService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "CatalogService.dll"]
