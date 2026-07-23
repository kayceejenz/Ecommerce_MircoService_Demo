
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/OrderService/OrderService.csproj src/OrderService/
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
RUN dotnet restore src/OrderService/OrderService.csproj

COPY src/OrderService/ src/OrderService/
COPY src/Shared.Contracts/ src/Shared.Contracts/
RUN dotnet publish src/OrderService/OrderService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "OrderService.dll"]
