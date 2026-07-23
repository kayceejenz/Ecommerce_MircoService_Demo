
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PaymentService/PaymentService.csproj src/PaymentService/
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
RUN dotnet restore src/PaymentService/PaymentService.csproj

COPY src/PaymentService/ src/PaymentService/
COPY src/Shared.Contracts/ src/Shared.Contracts/
RUN dotnet publish src/PaymentService/PaymentService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PaymentService.dll"]
