

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/NotificationService/NotificationService.csproj src/NotificationService/
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
RUN dotnet restore src/NotificationService/NotificationService.csproj

COPY src/NotificationService/ src/NotificationService/
COPY src/Shared.Contracts/ src/Shared.Contracts/
RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "NotificationService.dll"]
