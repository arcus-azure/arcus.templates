FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.100-bullseye-slim AS build
WORKDIR /src
COPY ["Arcus.Templates.AzureFunctions.EventHubs.csproj", ""]

COPY . .
WORKDIR "/src/."
RUN dotnet build "Arcus.Templates.AzureFunctions.EventHubs.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Arcus.Templates.AzureFunctions.EventHubs.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV AzureWebJobsScriptRoot=/app \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
